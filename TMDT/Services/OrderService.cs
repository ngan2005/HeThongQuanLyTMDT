using Microsoft.EntityFrameworkCore;
using TMDT.DTOs;
using TMDT.Models;
using TMDT.Services.Interfaces;
using TMDT.Utilities;

namespace TMDT.Services;

public class OrderService : IOrderService
{
    private static OrderService? _instance;
    public static OrderService Instance => _instance ??= new OrderService();

    private OrderService() { }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        using var context = new TmdtContext();
        return await context.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Shop)
            .Include(o => o.Address)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Include(o => o.OrderStatusHistories)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<List<Order>> GetBuyerOrdersAsync(int buyerId, string? statusFilter = null)
    {
        using var context = new TmdtContext();
        var query = context.Orders.AsNoTracking()
            .Include(o => o.Shop)
            .Include(o => o.Address)
            .Include(o => o.OrderDetails)
            .Where(o => o.BuyerId == buyerId);

        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Tất cả")
            query = query.Where(o => o.OrderStatus == statusFilter);

        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    public async Task<List<Order>> GetShopOrdersAsync(int shopId, string? statusFilter = null)
    {
        using var context = new TmdtContext();
        var query = context.Orders.AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.Address)
            .Include(o => o.OrderDetails)
            .Where(o => o.ShopId == shopId);

        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All" && statusFilter != "Tất cả")
            query = query.Where(o => o.OrderStatus == statusFilter);

        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    public async Task<List<Order>> GetAllOrdersAsync(string? statusFilter = null, string? keyword = null)
    {
        using var context = new TmdtContext();
        var query = context.Orders.AsNoTracking()
            .Include(o => o.Shop)
            .Include(o => o.Buyer)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Tất cả")
            query = query.Where(o => o.OrderStatus == statusFilter);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string kw = keyword.Trim();
            query = query.Where(o =>
                (o.OrderCode != null && EF.Functions.Like(o.OrderCode, $"%{kw}%")) ||
                (o.Shop != null && o.Shop.ShopName != null && EF.Functions.Like(o.Shop.ShopName, $"%{kw}%")) ||
                (o.Buyer != null && o.Buyer.FullName != null && EF.Functions.Like(o.Buyer.FullName, $"%{kw}%"))
            );
        }

        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    /// <summary>
    /// 🟢 Lấy tỉ lệ hoa hồng áp dụng cho 1 shop: ưu tiên Shop.CommissionRate (admin set riêng), fallback về SystemSettings.PlatformCommissionRate (global).
    /// Trả về (rate phần trăm, nguồn "Shop"/"Global") — dùng để snapshot vào Order.AppliedCommissionRate + Order.CommissionRateSource.
    /// </summary>
    private static async Task<(decimal Rate, string Source)> GetEffectiveCommissionRateAsync(TmdtContext context, int? shopId)
    {
        if (shopId.HasValue)
        {
            var shop = await context.Shops.FindAsync(shopId.Value);
            if (shop?.CommissionRate.HasValue == true && shop.CommissionRate.Value >= 0)
                return (shop.CommissionRate.Value, "Shop");
        }
        return (SystemSettingsHelper.Current.PlatformCommissionRate, "Global");
    }

    /// <summary>
    /// 🟢 Phiên bản sync (chỉ dùng khi đã có shop trong context hoặc không có shopId).
    /// Trả về % hoa hồng áp dụng — dùng SystemSettings global khi không xác định được shop.
    /// </summary>
    private static decimal GetEffectiveCommissionRate(decimal? shopRate)
    {
        if (shopRate.HasValue && shopRate.Value >= 0) return shopRate.Value;
        return SystemSettingsHelper.Current.PlatformCommissionRate;
    }

    /// <summary>
    /// 🟢 Ghi log InventoryTransaction cho mỗi biến động tồn kho trong OrderService.
    /// Gọi sau khi trừ/hoàn kho thành công, trước SaveChangesAsync.
    /// Phải truyền đầy đủ orderCode, reason, referenceType.
    /// </summary>
    private static void LogInventoryChange(
        TmdtContext context,
        int shopId,
        int? productId,
        int? variantId,
        string type,
        int before,
        int after,
        int qtyChange,
        string orderCode,
        string reason,
        string referenceType)
    {
        if (shopId <= 0) return; // không có shop → không log
        context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = productId,
            VariantId = variantId,
            ShopId = shopId,
            Type = type,
            QuantityBefore = before,
            QuantityChange = qtyChange,
            QuantityAfter = after,
            Reason = reason,
            ReferenceOrderCode = orderCode,
            ReferenceType = referenceType,
            PerformedBy = null,
            CreatedAt = DateTime.Now
        });
    }

    public async Task<Order?> CreateOrderFromCartAsync(
        int buyerId, int shopId, int? addressId, int? voucherId,
        string paymentMethod, decimal shippingFee,
        List<CartOrderItem> items, int pointsUsed = 0)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Danh sách sản phẩm trống.");

        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var shop = await context.Shops.FindAsync(shopId);
            if (shop != null && shop.IsActive == false)
                throw new InvalidOperationException($"Cửa hàng '{shop.ShopName}' hiện đang bị tạm khóa.");

            var subTotal = items.Sum(i => i.TotalPrice);
            
            // Xử lý dùng điểm tích lũy
            decimal discountFromPoints = 0;
            var buyer = await context.Users.FindAsync(buyerId);
            if (buyer == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản người dùng.");
                
            if (pointsUsed > 0)
            {
                if ((buyer.LoyaltyPoints ?? 0) < pointsUsed)
                    throw new InvalidOperationException("Số điểm tích lũy không đủ.");
                    
                discountFromPoints = pointsUsed * 100m; // 1 điểm = 100 VNĐ
                buyer.LoyaltyPoints = (buyer.LoyaltyPoints ?? 0) - pointsUsed;
                
                context.PointHistories.Add(new PointHistory
                {
                    UserId = buyer.UserId,
                    Points = -pointsUsed,
                    TransactionType = "Spend",
                    Description = $"Dùng {pointsUsed} điểm thanh toán đơn hàng",
                    CreatedAt = DateTime.Now
                });
            }

            // Xử lý Voucher
            decimal discountFromVoucher = 0;
            if (voucherId.HasValue)
            {
                var voucher = await context.Vouchers.FindAsync(voucherId.Value);
                if (voucher != null && voucher.IsActive == true && (voucher.UsedCount ?? 0) < (voucher.TotalQuantity ?? 0))
                {
                    if (voucher.DiscountType == "Percentage")
                    {
                        var discount = subTotal * (voucher.DiscountValue ?? 0) / 100m;
                        if (voucher.MaxDiscount.HasValue && discount > voucher.MaxDiscount.Value)
                            discount = voucher.MaxDiscount.Value;
                        discountFromVoucher = discount;
                    }
                    else
                    {
                        discountFromVoucher = voucher.DiscountValue ?? 0;
                        if (discountFromVoucher > subTotal) discountFromVoucher = subTotal;
                    }
                    
                    voucher.UsedCount = (voucher.UsedCount ?? 0) + 1;
                }
                else
                {
                    // Nếu voucher không hợp lệ lúc thanh toán, gỡ bỏ
                    voucherId = null;
                }
            }

            var totalAmount = subTotal + shippingFee - discountFromPoints - discountFromVoucher;
            if (totalAmount < 0) totalAmount = 0;

            // 🟢 Tính phí sàn theo Shop.CommissionRate (admin set riêng), fallback global rate
            var (commissionRate, rateSource) = await GetEffectiveCommissionRateAsync(context, shopId);
            var platformFee = totalAmount * (commissionRate / 100m);

            var order = new Order
            {
                OrderCode = GenerateOrderCode(),
                BuyerId = buyerId,
                ShopId = shopId,
                AddressId = addressId,
                VoucherId = voucherId,
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                Discount = discountFromPoints + discountFromVoucher, // Lưu tổng số tiền được giảm
                TotalAmount = totalAmount,
                PlatformFee = platformFee,
                // 🟢 Snapshot rate đã áp dụng để audit khi admin thay đổi sau
                AppliedCommissionRate = commissionRate,
                CommissionRateSource = rateSource,
                PaymentMethod = paymentMethod,
                OrderStatus = "Pending",
                OrderDate = DateTime.Now
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            foreach (var item in items)
            {
                var product = await context.Products.FindAsync(item.ProductId);
                if (product == null)
                    throw new InvalidOperationException($"Không tìm thấy sản phẩm '{item.ProductName}'.");

                if (item.VariantId.HasValue)
                {
                    var variant = await context.ProductVariants.FindAsync(item.VariantId.Value);
                    if (variant == null)
                        throw new InvalidOperationException($"Không tìm thấy biến thể của sản phẩm '{item.ProductName}'.");
                    if ((variant.Quantity ?? 0) < item.Quantity)
                        throw new InvalidOperationException($"Sản phẩm '{item.ProductName} ({item.VariantName})' không đủ tồn kho.");

                    int variantBefore = variant.Quantity ?? 0;
                    variant.Quantity = (variant.Quantity ?? 0) - item.Quantity;
                    LogInventoryChange(context, shopId, product.ProductId, variant.VariantId,
                        "Order", variantBefore, variant.Quantity.Value, -item.Quantity,
                        order.OrderCode, "Bán online (buyer đặt)", "Order");
                }
                else
                {
                    if ((product.StockQuantity ?? 0) < item.Quantity)
                        throw new InvalidOperationException(
                            $"Sản phẩm '{product.ProductName}' không đủ tồn kho. (Còn: {product.StockQuantity ?? 0}, yêu cầu: {item.Quantity}).");

                    int productBefore = product.StockQuantity ?? 0;
                    product.StockQuantity = (product.StockQuantity ?? 0) - item.Quantity;
                    LogInventoryChange(context, shopId, product.ProductId, null,
                        "Order", productBefore, product.StockQuantity.Value, -item.Quantity,
                        order.OrderCode, "Bán online (buyer đặt)", "Order");
                }

                context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    ProductNameSnapshot = item.VariantName != null ? $"{item.ProductName} ({item.VariantName})" : item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                });
            }

            await context.SaveChangesAsync();

            // Tạo Payment record
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Method = paymentMethod,
                Amount = totalAmount,
                Status = "Pending"
            };
            context.Payments.Add(payment);

            // Xử lý thanh toán
            switch (paymentMethod)
            {
                case "COD":
                    // COD: giữ Pending, khách trả khi nhận
                    payment.Status = "Pending";
                    break;

                case "MoMo":
                    // Mock online payment — trong thực tế sẽ redirect sang trang thanh toán
                    payment.TransactionCode = GenerateTransactionCode(paymentMethod);
                    payment.Status = "Paid";
                    payment.PaidAt = DateTime.Now;
                    break;

                case "Wallet":
                    // Reload buyer in case it wasn't tracked properly or wallet changed
                    var buyerWallet = await context.Users.FindAsync(buyerId);
                    if (buyerWallet == null)
                        throw new InvalidOperationException("Không tìm thấy tài khoản người dùng.");
                    if ((buyerWallet.WalletBalance ?? 0) < totalAmount)
                        throw new InvalidOperationException("Số dư ví không đủ để thanh toán.");
                    buyerWallet.WalletBalance = (buyerWallet.WalletBalance ?? 0) - totalAmount;
                    payment.TransactionCode = GenerateTransactionCode("Wallet");
                    payment.Status = "Paid";
                    payment.PaidAt = DateTime.Now;
                    break;

                default:
                    payment.Status = "Pending";
                    break;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return order;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Order?> CreatePosOrderAsync(int? buyerId, int shopId, int? voucherId, string paymentMethod, List<CartOrderItem> items, int pointsUsed = 0, decimal manualDiscount = 0, string orderStatus = "Completed")
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var subTotal = items.Sum(i => i.TotalPrice);
            
            // Xử lý giảm giá từ điểm
            decimal discountFromPoints = 0;
            User? actualBuyer = null;

            if (buyerId.HasValue)
            {
                actualBuyer = await context.Users.FindAsync(buyerId.Value);
                if (actualBuyer != null && pointsUsed > 0)
                {
                    if ((actualBuyer.LoyaltyPoints ?? 0) < pointsUsed)
                        throw new InvalidOperationException("Không đủ điểm tích lũy.");

                    discountFromPoints = pointsUsed * 1000m; // 1 điểm = 1000 VNĐ
                    actualBuyer.LoyaltyPoints -= pointsUsed;

                    context.PointHistories.Add(new PointHistory
                    {
                        UserId = actualBuyer.UserId,
                        Points = -pointsUsed,
                        TransactionType = "Redeem",
                        Description = $"Dùng điểm mua hàng tại quầy",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Tìm hoặc tạo tài khoản "Khách vãng lai" nếu không có buyer
            if (actualBuyer == null)
            {
                actualBuyer = await context.Users.FirstOrDefaultAsync(u => u.Email == "guest@pos.local");
                if (actualBuyer == null)
                {
                    actualBuyer = new User
                    {
                        FullName = "Khách vãng lai",
                        Email = "guest@pos.local",
                        Password = "POS_GUEST",
                        RoleId = 2, // Buyer
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };
                    context.Users.Add(actualBuyer);
                    await context.SaveChangesAsync();
                }
            }

            // Xử lý Voucher
            decimal discountFromVoucher = 0;
            if (voucherId.HasValue)
            {
                var voucher = await context.Vouchers.FindAsync(voucherId.Value);
                if (voucher != null && voucher.IsActive == true && voucher.ShopId == shopId &&
                    voucher.StartDate <= DateTime.Now && voucher.EndDate >= DateTime.Now &&
                    subTotal >= (voucher.MinOrderValue ?? 0) &&
                    (voucher.TotalQuantity == null || voucher.UsedCount < voucher.TotalQuantity))
                {
                    if (voucher.DiscountType == "Percentage")
                        discountFromVoucher = subTotal * ((voucher.DiscountValue ?? 0) / 100m);
                    else
                        discountFromVoucher = voucher.DiscountValue ?? 0;

                    if (voucher.MaxDiscount.HasValue && discountFromVoucher > voucher.MaxDiscount.Value)
                        discountFromVoucher = voucher.MaxDiscount.Value;

                    voucher.UsedCount = (voucher.UsedCount ?? 0) + 1;
                }
                else
                {
                    voucherId = null;
                }
            }

            var totalAmount = subTotal - discountFromPoints - discountFromVoucher - manualDiscount;
            if (totalAmount < 0) totalAmount = 0;

            // 🟢 POS offline sales at counter are FREE of platform commission (0%)
            var commissionRate = 0m;
            var rateSource = "POS";
            var platformFee = 0m;

            var order = new Order
            {
                OrderCode = GenerateOrderCode(),
                BuyerId = actualBuyer.UserId,
                ShopId = shopId,
                VoucherId = voucherId,
                SubTotal = subTotal,
                ShippingFee = 0,
                Discount = discountFromPoints + discountFromVoucher,
                ManualDiscount = manualDiscount > 0 ? manualDiscount : null,
                TotalAmount = totalAmount,
                PlatformFee = platformFee,
                AppliedCommissionRate = commissionRate,
                CommissionRateSource = rateSource,
                PaymentMethod = paymentMethod,
                OrderStatus = orderStatus,
                OrderDate = DateTime.Now
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Chỉ trừ tồn kho khi thanh toán Cash (Completed) — MoMo/VNPay (AwaitingPayment) sẽ trừ khi ConfirmPosOrderAsync
            if (orderStatus == "Completed")
            {
                foreach (var item in items)
                {
                    if (item.VariantId.HasValue)
                    {
                        // 🟢 Dùng raw SQL UPDATE có WHERE Quantity >= @qty để chống race condition
                        // Nếu bị 0 rows affected → tồn đã bị người khác trừ mất → rollback
                        // 🟢 Audit: đọc tồn trước để log change (sau khi UPDATE thành công)
                        var beforeVariant = await context.ProductVariants.FindAsync(item.VariantId.Value);
                        int beforeQty = beforeVariant?.Quantity ?? 0;

                        var rows = await context.Database.ExecuteSqlInterpolatedAsync(
                            $"UPDATE ProductVariant SET Quantity = Quantity - {item.Quantity} WHERE VariantId = {item.VariantId.Value} AND Quantity >= {item.Quantity}");
                        if (rows == 0)
                            throw new InvalidOperationException($"Sản phẩm '{item.ProductName} ({item.VariantName})' không đủ tồn kho (đã có người khác mua trước).");

                        LogInventoryChange(context, shopId, item.ProductId, item.VariantId,
                            "Order", beforeQty, beforeQty - item.Quantity, -item.Quantity,
                            order.OrderCode, "Bán tại quầy (POS)", "Order");
                    }
                    else
                    {
                        var beforeProduct = await context.Products.FindAsync(item.ProductId);
                        int beforeQty = beforeProduct?.StockQuantity ?? 0;

                        var rows = await context.Database.ExecuteSqlInterpolatedAsync(
                            $"UPDATE Product SET StockQuantity = StockQuantity - {item.Quantity} WHERE ProductId = {item.ProductId} AND StockQuantity >= {item.Quantity}");
                        if (rows == 0)
                            throw new InvalidOperationException($"Sản phẩm '{item.ProductName}' không đủ tồn kho (đã có người khác mua trước).");

                        LogInventoryChange(context, shopId, item.ProductId, null,
                            "Order", beforeQty, beforeQty - item.Quantity, -item.Quantity,
                            order.OrderCode, "Bán tại quầy (POS)", "Order");
                    }
                }
            }

            foreach (var item in items)
            {
                var product = await context.Products.FindAsync(item.ProductId);
                if (product == null)
                    throw new InvalidOperationException($"Không tìm thấy sản phẩm '{item.ProductName}'.");

                context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    ProductNameSnapshot = item.VariantName != null ? $"{item.ProductName} ({item.VariantName})" : item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                });
            }

            await context.SaveChangesAsync();

            // Tạo Payment record
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Method = paymentMethod,
                Amount = totalAmount,
                Status = orderStatus == "Completed" ? "Paid" : "Pending",
                PaidAt = orderStatus == "Completed" ? DateTime.Now : null
            };
            
            if (paymentMethod == "MoMo" || paymentMethod == "VNPay" || paymentMethod == "POS_Transfer")
            {
                payment.TransactionCode = GenerateTransactionCode(paymentMethod);
            }
            context.Payments.Add(payment);

            // Cập nhật doanh thu cho Shop (chỉ khi thanh toán Cash — MoMo/VNPay sẽ cộng ở ConfirmPosOrderAsync)
            if (paymentMethod == "Cash")
            {
                var shop = await context.Shops.FindAsync(shopId);
                if (shop != null)
                {
                    var shopRevenue = totalAmount - platformFee;
                    shop.WalletBalance = (shop.WalletBalance ?? 0) + shopRevenue;
                }

                // 🟢 FIX: cộng phí sàn vào ví hệ thống (trước đây thiếu — tiền commission của POS cash bị "mất")
                SystemSettingsHelper.AddSystemWalletBalance(platformFee);

                // Tích điểm cho khách hàng khi thanh toán Cash (1 điểm = 10,000đ)
                if (actualBuyer.Email != "guest@pos.local")
                {
                    int earnedPoints = (int)(totalAmount / 10000);
                    if (earnedPoints > 0)
                    {
                        actualBuyer.LoyaltyPoints = (actualBuyer.LoyaltyPoints ?? 0) + earnedPoints;
                        context.PointHistories.Add(new PointHistory
                        {
                            UserId = actualBuyer.UserId,
                            Points = earnedPoints,
                            TransactionType = "Earn",
                            OrderId = order.OrderId,
                            Description = $"Tích điểm từ đơn hàng POS {order.OrderCode}",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Reload order kèm OrderDetails để trả về đầy đủ cho hóa đơn
            using var readCtx = new TmdtContext();
            return await readCtx.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CancelOrderAsync(int orderId)
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return false;
            if (order.OrderStatus != "Pending" && order.OrderStatus != "Chờ duyệt")
                throw new InvalidOperationException("Chỉ có thể hủy đơn ở trạng thái chờ duyệt.");

            foreach (var detail in order.OrderDetails)
            {
                if (detail.VariantId.HasValue && detail.Quantity.HasValue)
                {
                    var variant = await context.ProductVariants.FindAsync(detail.VariantId.Value);
                    if (variant != null)
                    {
                        int before = variant.Quantity ?? 0;
                        variant.Quantity = (variant.Quantity ?? 0) + detail.Quantity.Value;
                        LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, detail.VariantId,
                            "Cancel", before, variant.Quantity.Value, detail.Quantity.Value,
                            order.OrderCode ?? "", "Hoàn kho do hủy đơn online", "Order");
                    }
                }
                else if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                {
                    var product = await context.Products.FindAsync(detail.ProductId.Value);
                    if (product != null)
                    {
                        int before = product.StockQuantity ?? 0;
                        product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity.Value;
                        LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, null,
                            "Cancel", before, product.StockQuantity.Value, detail.Quantity.Value,
                            order.OrderCode ?? "", "Hoàn kho do hủy đơn online", "Order");
                    }
                }
            }

            order.OrderStatus = "Cancelled";
            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                NewStatus = "Cancelled",
                ChangedAt = DateTime.Now
            });

            // Hoàn tiền cho Buyer nếu đã thanh toán trước (Ví, VNPay, MoMo)
            var payment = await context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment != null && payment.Status == "Paid" && order.PaymentMethod != "COD")
            {
                if (order.BuyerId.HasValue)
                {
                    var buyer = await context.Users.FindAsync(order.BuyerId.Value);
                    if (buyer != null)
                    {
                        buyer.WalletBalance = (buyer.WalletBalance ?? 0) + (order.TotalAmount ?? 0);
                    }
                }
                payment.Status = "Refunded";
                context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = orderId,
                    NewStatus = "Hoàn tiền",
                    Note = "Tự động hoàn tiền vào Ví Volox do Hủy đơn",
                    ChangedAt = DateTime.Now
                });
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ShipOrderAsync(int orderId, string shippingProvider)
    {
        using var context = new TmdtContext();

        var order = await context.Orders.FindAsync(orderId);
        if (order == null || order.OrderStatus != "Pending")
            return false;

        string prefix = shippingProvider switch
        {
            "Giao Hàng Tiết Kiệm" => "GHTK",
            "Viettel Post" => "VTP",
            "Giao Hàng Nhanh" => "GHN",
            "J&T Express" => "JNT",
            _ => "SPX"
        };

        order.OrderStatus = "Shipping";
        order.TrackingCode = prefix + "-" + Random.Shared.Next(10000000, 99999999);
        order.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            NewStatus = "Shipping",
            Note = $"Giao qua {shippingProvider}",
            ChangedAt = DateTime.Now
        });

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReceiveOrderAsync(int orderId)
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var order = await context.Orders.FindAsync(orderId);
            if (order == null || order.OrderStatus != "Shipping")
                return false;

            order.OrderStatus = "Completed";
            order.CompletedAt = DateTime.Now;

            if (order.ShopId.HasValue)
            {
                var shop = await context.Shops.FindAsync(order.ShopId.Value);
                if (shop != null)
                {
                    var revenue = (order.TotalAmount ?? 0) - (order.PlatformFee ?? 0);
                    shop.WalletBalance = (shop.WalletBalance ?? 0) + revenue;
                }
            }

            SystemSettingsHelper.AddSystemWalletBalance(order.PlatformFee ?? 0);

            // TÍCH ĐIỂM CHO NGƯỜI MUA (1 điểm = 10,000 VNĐ)
            if (order.BuyerId.HasValue)
            {
                var buyer = await context.Users.FindAsync(order.BuyerId.Value);
                if (buyer != null)
                {
                    int earnedPoints = (int)((order.TotalAmount ?? 0) / 10000);
                    if (earnedPoints > 0)
                    {
                        buyer.LoyaltyPoints = (buyer.LoyaltyPoints ?? 0) + earnedPoints;
                        context.PointHistories.Add(new PointHistory
                        {
                            UserId = buyer.UserId,
                            Points = earnedPoints,
                            TransactionType = "Earn",
                            OrderId = order.OrderId,
                            Description = $"Tích điểm từ đơn hàng {order.OrderCode}",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                NewStatus = "Completed",
                ChangedAt = DateTime.Now
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> RefundOrderAsync(int orderId)
    {
        // 🟢 Refund cho Seller POS: chỉ đơn POS (AddressId == null) + đang Completed/CompletedOffline.
        return await RefundOrderInternalAsync(orderId, requirePosOnly: true);
    }

    public async Task<bool> AdminRefundOrderAsync(int orderId)
    {
        // 🟢 Refund cho Admin: cho phép mọi đơn (kể cả ship) — admin đã qua xét duyệt.
        return await RefundOrderInternalAsync(orderId, requirePosOnly: false);
    }

    private async Task<bool> RefundOrderInternalAsync(int orderId, bool requirePosOnly)
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null || order.BuyerId == null)
                return false;

            // 🟢 Phân quyền: Seller (POS) chỉ refund đơn POS tại quầy (AddressId == null).
            if (requirePosOnly && order.AddressId.HasValue)
                throw new InvalidOperationException("Đơn hàng online có vận chuyển không thể hoàn trả tại POS. Vui lòng liên hệ Admin để được xử lý.");

            // 🟢 Chỉ refund khi đơn đang Completed hoặc CompletedOffline (sau sync).
            if (order.OrderStatus != "Completed" && order.OrderStatus != "CompletedOffline")
                throw new InvalidOperationException($"Đơn ở trạng thái '{order.OrderStatus}' không thể hoàn trả. Chỉ chấp nhận đơn đã hoàn thành.");

            if (order.OrderStatus == "Completed" && order.ShopId.HasValue)
            {
                var shop = await context.Shops.FindAsync(order.ShopId.Value);
                if (shop != null)
                {
                    var revenue = (order.TotalAmount ?? 0) - (order.PlatformFee ?? 0);
                    shop.WalletBalance = (shop.WalletBalance ?? 0) - revenue;
                }
                SystemSettingsHelper.AddSystemWalletBalance(-(order.PlatformFee ?? 0));
            }

            foreach (var detail in order.OrderDetails)
            {
                if (detail.VariantId.HasValue && detail.Quantity.HasValue)
                {
                    var variant = await context.ProductVariants.FindAsync(detail.VariantId.Value);
                    if (variant != null)
                    {
                        int before = variant.Quantity ?? 0;
                        variant.Quantity = (variant.Quantity ?? 0) + detail.Quantity.Value;
                        LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, detail.VariantId,
                            "Refund", before, variant.Quantity.Value, detail.Quantity.Value,
                            order.OrderCode ?? "", "Hoàn kho do refund", "Order");
                    }
                }
                else if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                {
                    var product = await context.Products.FindAsync(detail.ProductId.Value);
                    if (product != null)
                    {
                        int before = product.StockQuantity ?? 0;
                        product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity.Value;
                        LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, null,
                            "Refund", before, product.StockQuantity.Value, detail.Quantity.Value,
                            order.OrderCode ?? "", "Hoàn kho do refund", "Order");
                    }
                }
            }

            var buyer = await context.Users.FindAsync(order.BuyerId.Value);
            if (buyer != null)
                buyer.WalletBalance = (buyer.WalletBalance ?? 0) + (order.TotalAmount ?? 0);

            order.OrderStatus = "Hoàn tiền";

            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                NewStatus = "Hoàn tiền",
                Note = "Hoàn tiền bởi Admin",
                ChangedAt = DateTime.Now
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Order?> UpdatePosOrderAsync(int orderId, List<CartOrderItem> items, decimal manualDiscount, int? voucherId, int pointsUsed)
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new InvalidOperationException("Không tìm thấy đơn hàng.");

            // 🔴 Chỉ cho phép sửa khi đơn chờ thanh toán — tránh sửa đơn đã Completed/Cancelled
            if (order.OrderStatus != "AwaitingPayment")
                throw new InvalidOperationException($"Không thể sửa đơn đang ở trạng thái '{order.OrderStatus}'.");

            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Đơn hàng phải có ít nhất 1 sản phẩm.");

            // Xoá OrderDetails cũ (AwaitingPayment chưa trừ kho, không cần hoàn kho)
            context.OrderDetails.RemoveRange(order.OrderDetails);

            // Tính lại SubTotal + Discount từ danh sách items mới
            var subTotal = items.Sum(i => i.TotalPrice);
            decimal voucherDiscount = 0;
            decimal platformFee = 0m;

            if (voucherId.HasValue)
            {
                var voucher = await context.Vouchers.FindAsync(voucherId.Value);
                if (voucher != null && voucher.IsActive == true)
                {
                    voucherDiscount = voucher.DiscountType == "Percentage"
                        ? subTotal * ((voucher.DiscountValue ?? 0) / 100m)
                        : (voucher.DiscountValue ?? 0);

                    if (voucher.MaxDiscount.HasValue)
                        voucherDiscount = Math.Min(voucherDiscount, voucher.MaxDiscount.Value);
                }
            }

            decimal pointsDiscount = pointsUsed * 1000m;
            decimal totalDiscount = voucherDiscount + manualDiscount + pointsDiscount;
            decimal totalAmount = Math.Max(0, subTotal - totalDiscount);

            // 🟢 Tính phí sàn theo Shop.CommissionRate (admin set riêng), fallback global rate
            var (commissionRate, rateSource) = await GetEffectiveCommissionRateAsync(context, order.ShopId);
            platformFee = totalAmount * (commissionRate / 100m);
            // 🟢 Snapshot rate đã áp dụng để audit khi admin thay đổi sau
            order.AppliedCommissionRate = commissionRate;
            order.CommissionRateSource = rateSource;

            // Tạo OrderDetails mới
            foreach (var item in items)
            {
                var detail = new OrderDetail
                {
                    OrderId = orderId,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    ProductNameSnapshot = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                };
                context.OrderDetails.Add(detail);
            }

            // Cập nhật các trường trên Order
            order.SubTotal = subTotal;
            order.Discount = voucherDiscount + pointsDiscount;
            order.ManualDiscount = manualDiscount;
            order.TotalAmount = totalAmount;
            order.PlatformFee = platformFee;
            order.VoucherId = voucherId;
            order.OrderDate = DateTime.Now; // cập nhật thời điểm sửa

            // Thêm lịch sử trạng thái
            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                NewStatus = "AwaitingPayment",
                ChangedAt = DateTime.Now,
                Note = $"Đơn được chỉnh sửa lúc {DateTime.Now:HH:mm dd/MM/yyyy} — tổng mới: {totalAmount:N0}đ"
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Trả về order với OrderDetails mới
            return await GetOrderByIdAsync(orderId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ConfirmPosOrderAsync(int orderId, string transactionCode)
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var order = await context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return false;
            if (order.OrderStatus != "AwaitingPayment")
                throw new InvalidOperationException("Đơn hàng không ở trạng thái chờ thanh toán.");

            // Cập nhật Payment
            var payment = await context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment != null)
            {
                payment.Status = "Paid";
                payment.TransactionCode = transactionCode;
                payment.PaidAt = DateTime.Now;
            }

            // Trừ tồn kho khi xác nhận thanh toán MoMo/VNPay thành công — dùng raw SQL chống race
            foreach (var detail in order.OrderDetails)
            {
                if (detail.VariantId.HasValue && detail.Quantity.HasValue)
                {
                    var beforeVariant = await context.ProductVariants.FindAsync(detail.VariantId.Value);
                    int beforeQty = beforeVariant?.Quantity ?? 0;

                    var rows = await context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE ProductVariant SET Quantity = Quantity - {detail.Quantity.Value} WHERE VariantId = {detail.VariantId.Value} AND Quantity >= {detail.Quantity.Value}");
                    if (rows == 0)
                        throw new InvalidOperationException($"Sản phẩm '{detail.ProductNameSnapshot}' không đủ tồn kho khi xác nhận thanh toán (đã có người mua trước).");

                    LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, detail.VariantId,
                        "Order", beforeQty, beforeQty - detail.Quantity.Value, -detail.Quantity.Value,
                        order.OrderCode ?? "", "Xác nhận thanh toán MoMo/VNPay (POS)", "Order");
                }
                else if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                {
                    var beforeProduct = await context.Products.FindAsync(detail.ProductId.Value);
                    int beforeQty = beforeProduct?.StockQuantity ?? 0;

                    var rows = await context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE Product SET StockQuantity = StockQuantity - {detail.Quantity.Value} WHERE ProductId = {detail.ProductId.Value} AND StockQuantity >= {detail.Quantity.Value}");
                    if (rows == 0)
                        throw new InvalidOperationException($"Sản phẩm '{detail.ProductNameSnapshot}' không đủ tồn kho khi xác nhận thanh toán (đã có người mua trước).");

                    LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, null,
                        "Order", beforeQty, beforeQty - detail.Quantity.Value, -detail.Quantity.Value,
                        order.OrderCode ?? "", "Xác nhận thanh toán MoMo/VNPay (POS)", "Order");
                }
            }

            // Cộng tiền vào ví Shop
            var shop = await context.Shops.FindAsync(order.ShopId);
            if (shop != null)
            {
                var shopRevenue = (order.TotalAmount ?? 0) - (order.PlatformFee ?? 0);
                shop.WalletBalance = (shop.WalletBalance ?? 0) + shopRevenue;
            }

            // 🟢 FIX: cộng phí sàn vào ví hệ thống (trước đây thiếu — tiền commission của POS MoMo/VNPay bị "mất")
            SystemSettingsHelper.AddSystemWalletBalance(order.PlatformFee ?? 0);

            order.OrderStatus = "Completed";
            order.CompletedAt = DateTime.Now;
            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                NewStatus = "Completed",
                Note = "Thanh toán POS thành công",
                ChangedAt = DateTime.Now
            });

            // Tích điểm cho khách hàng
            if (order.Buyer != null && order.Buyer.Email != "guest@pos.local")
            {
                int earnedPoints = (int)((order.TotalAmount ?? 0) / 10000);
                if (earnedPoints > 0)
                {
                    order.Buyer.LoyaltyPoints = (order.Buyer.LoyaltyPoints ?? 0) + earnedPoints;
                    context.PointHistories.Add(new PointHistory
                    {
                        UserId = order.Buyer.UserId,
                        Points = earnedPoints,
                        TransactionType = "Earn",
                        OrderId = order.OrderId,
                        Description = $"Tích điểm từ đơn hàng POS {order.OrderCode}",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 🟢 Xác nhận thanh toán QR khi offline (mất mạng / server down).
    /// - Set OrderStatus = Completed, Payment.Status = "PaidOffline", ghi TransactionCode dạng OFFLINE_{ticks}.
    /// - KHÔNG gọi wallet cộng tiền ở đây — sẽ đồng bộ khi có mạng.
    /// - Trừ tồn kho ngay để cashier in được bill chính xác.
    /// </summary>
    public async Task<bool> ConfirmPosOrderOfflineAsync(int orderId, string transactionCode)
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var order = await context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return false;
            if (order.OrderStatus != "AwaitingPayment")
                throw new InvalidOperationException("Đơn hàng không ở trạng thái chờ thanh toán.");

            var payment = await context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment != null)
            {
                payment.Status = "PaidOffline";
                payment.TransactionCode = transactionCode;
                payment.PaidAt = DateTime.Now;
            }

            // Trừ tồn kho ngay — cashier in bill cần số liệu khớp
            foreach (var detail in order.OrderDetails)
            {
                if (detail.VariantId.HasValue && detail.Quantity.HasValue)
                {
                    var beforeVariant = await context.ProductVariants.FindAsync(detail.VariantId.Value);
                    int beforeQty = beforeVariant?.Quantity ?? 0;

                    var rows = await context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE ProductVariant SET Quantity = Quantity - {detail.Quantity.Value} WHERE VariantId = {detail.VariantId.Value} AND Quantity >= {detail.Quantity.Value}");
                    if (rows == 0)
                        throw new InvalidOperationException($"Sản phẩm '{detail.ProductNameSnapshot}' không đủ tồn kho.");

                    LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, detail.VariantId,
                        "Order", beforeQty, beforeQty - detail.Quantity.Value, -detail.Quantity.Value,
                        order.OrderCode ?? "", "Thanh toán POS offline (mạng lỗi)", "Order");
                }
                else if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                {
                    var beforeProduct = await context.Products.FindAsync(detail.ProductId.Value);
                    int beforeQty = beforeProduct?.StockQuantity ?? 0;

                    var rows = await context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE Product SET StockQuantity = StockQuantity - {detail.Quantity.Value} WHERE ProductId = {detail.ProductId.Value} AND StockQuantity >= {detail.Quantity.Value}");
                    if (rows == 0)
                        throw new InvalidOperationException($"Sản phẩm '{detail.ProductNameSnapshot}' không đủ tồn kho.");

                    LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, null,
                        "Order", beforeQty, beforeQty - detail.Quantity.Value, -detail.Quantity.Value,
                        order.OrderCode ?? "", "Thanh toán POS offline (mạng lỗi)", "Order");
                }
            }

            order.OrderStatus = "CompletedOffline";
            order.CompletedAt = DateTime.Now;
            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                NewStatus = "CompletedOffline",
                Note = $"Thanh toán offline (mạng lỗi) — TX: {transactionCode}. Chờ sync.",
                ChangedAt = DateTime.Now
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 🟢 Sync 1 đơn offline đã online: cộng tiền vào ví shop, tích điểm, set OrderStatus = Completed.
    /// </summary>
    public async Task<bool> SyncOfflinePosOrderAsync(int orderId)
    {
        using var context = new TmdtContext();
        try
        {
            var order = await context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return false;
            if (order.OrderStatus != "CompletedOffline") return true; // đã sync rồi

            var shop = await context.Shops.FindAsync(order.ShopId);
            if (shop != null)
            {
                var shopRevenue = (order.TotalAmount ?? 0) - (order.PlatformFee ?? 0);
                shop.WalletBalance = (shop.WalletBalance ?? 0) + shopRevenue;
            }

            // 🟢 FIX: cộng phí sàn vào ví hệ thống (trước đây thiếu — đơn POS offline sync không tính commission sàn)
            SystemSettingsHelper.AddSystemWalletBalance(order.PlatformFee ?? 0);

            order.OrderStatus = "Completed";
            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                NewStatus = "Completed",
                Note = "Sync offline thành công — đã cộng tiền vào ví shop.",
                ChangedAt = DateTime.Now
            });

            if (order.Buyer != null && order.Buyer.Email != "guest@pos.local")
            {
                int earnedPoints = (int)((order.TotalAmount ?? 0) / 10000);
                if (earnedPoints > 0)
                {
                    order.Buyer.LoyaltyPoints = (order.Buyer.LoyaltyPoints ?? 0) + earnedPoints;
                    context.PointHistories.Add(new PointHistory
                    {
                        UserId = order.Buyer.UserId,
                        Points = earnedPoints,
                        TransactionType = "Earn",
                        OrderId = order.OrderId,
                        Description = $"Tích điểm (sync offline) từ đơn POS {order.OrderCode}",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CancelPosOrderAsync(int orderId)
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Buyer)
                .Include(o => o.Voucher)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return false;
            if (order.OrderStatus != "AwaitingPayment")
                throw new InvalidOperationException("Chỉ có thể hủy đơn ở trạng thái chờ thanh toán.");

            // Hoàn voucher
            if (order.VoucherId.HasValue && order.Voucher != null)
                order.Voucher.UsedCount = Math.Max(0, (order.Voucher.UsedCount ?? 1) - 1);

            // Hoàn điểm đã dùng
            if (order.Buyer != null)
            {
                var pointsUsed = (int)((order.Discount ?? 0) / 1000);
                if (pointsUsed > 0)
                {
                    order.Buyer.LoyaltyPoints = (order.Buyer.LoyaltyPoints ?? 0) + pointsUsed;
                    context.PointHistories.Add(new PointHistory
                    {
                        UserId = order.Buyer.UserId,
                        Points = pointsUsed,
                        TransactionType = "Refund",
                        Description = $"Hoàn điểm từ đơn hủy {order.OrderCode}",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Trừ điểm đã cộng cho khách (loyalty points earned) — kiểm tra theo PointHistory để tránh trừ nhầm
            if (order.Buyer != null && order.OrderId > 0)
            {
                var earnedHistory = await context.PointHistories.FirstOrDefaultAsync(ph =>
                    ph.UserId == order.Buyer.UserId &&
                    ph.TransactionType == "Earn" &&
                    ph.Description != null && ph.Description.Contains(order.OrderCode));
                if (earnedHistory != null && earnedHistory.Points > 0)
                {
                    order.Buyer.LoyaltyPoints = Math.Max(0, (order.Buyer.LoyaltyPoints ?? 0) - earnedHistory.Points.Value);
                    context.PointHistories.Add(new PointHistory
                    {
                        UserId = order.Buyer.UserId,
                        Points = -earnedHistory.Points.Value,
                        TransactionType = "Refund",
                        Description = $"Thu hồi điểm thưởng từ đơn hủy {order.OrderCode}",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Hoàn lại tồn kho
            foreach (var detail in order.OrderDetails)
            {
                if (detail.VariantId.HasValue && detail.Quantity.HasValue)
                {
                    var variant = await context.ProductVariants.FindAsync(detail.VariantId.Value);
                    if (variant != null)
                    {
                        int before = variant.Quantity ?? 0;
                        variant.Quantity = (variant.Quantity ?? 0) + detail.Quantity.Value;
                        LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, detail.VariantId,
                            "Cancel", before, variant.Quantity.Value, detail.Quantity.Value,
                            order.OrderCode ?? "", "Hoàn kho do hủy POS chờ thanh toán", "Order");
                    }
                }
                else if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                {
                    var product = await context.Products.FindAsync(detail.ProductId.Value);
                    if (product != null)
                    {
                        int before = product.StockQuantity ?? 0;
                        product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity.Value;
                        LogInventoryChange(context, order.ShopId ?? 0, detail.ProductId, null,
                            "Cancel", before, product.StockQuantity.Value, detail.Quantity.Value,
                            order.OrderCode ?? "", "Hoàn kho do hủy POS chờ thanh toán", "Order");
                    }
                }
            }

            order.OrderStatus = "Cancelled";
            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                NewStatus = "Cancelled",
                Note = "Hủy đơn chờ thanh toán MoMo/VNPay",
                ChangedAt = DateTime.Now
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdatePaymentSuccessAsync(int orderId, string transactionCode)
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var payment = await context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment == null) return false;

            if (payment.Status != "Paid")
            {
                payment.Status = "Paid";
                payment.TransactionCode = transactionCode;
                payment.PaidAt = DateTime.Now;

            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<OrderStats> GetBuyerStatsAsync(int buyerId)
    {
        using var context = new TmdtContext();
        var orders = await context.Orders.AsNoTracking()
            .Where(o => o.BuyerId == buyerId)
            .ToListAsync();

        return new OrderStats
        {
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.OrderStatus == "Pending"),
            ShippingOrders = orders.Count(o => o.OrderStatus == "Shipping"),
            CompletedOrders = orders.Count(o => o.OrderStatus == "Completed"),
            CancelledOrders = orders.Count(o => o.OrderStatus == "Cancelled"),
            TotalSpending = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount ?? 0)
        };
    }

    public async Task<OrderStats> GetShopStatsAsync(int shopId)
    {
        using var context = new TmdtContext();
        var orders = await context.Orders.AsNoTracking()
            .Where(o => o.ShopId == shopId)
            .ToListAsync();

        return new OrderStats
        {
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.OrderStatus == "Pending"),
            ShippingOrders = orders.Count(o => o.OrderStatus == "Shipping"),
            CompletedOrders = orders.Count(o => o.OrderStatus == "Completed"),
            CancelledOrders = orders.Count(o => o.OrderStatus == "Cancelled"),
            TotalRevenue = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount ?? 0) -
                           orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.PlatformFee ?? 0)
        };
    }

    public async Task<OrderStats> GetAdminStatsAsync()
    {
        using var context = new TmdtContext();
        var orders = await context.Orders.AsNoTracking().ToListAsync();

        return new OrderStats
        {
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ xác nhận"),
            ShippingOrders = orders.Count(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao hàng"),
            CompletedOrders = orders.Count(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn thành"),
            CancelledOrders = orders.Count(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy"),
            TotalRevenue = orders.Where(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn thành")
                                 .Sum(o => o.PlatformFee ?? 0)
        };
    }

    private static string GenerateOrderCode()
    {
        return "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-" +
               Guid.NewGuid().ToString("N")[..6].ToUpper();
    }

    private static string GenerateTransactionCode(string prefix)
    {
        return $"{prefix}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}

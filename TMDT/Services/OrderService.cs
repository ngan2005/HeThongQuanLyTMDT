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

    public async Task<Order?> CreateOrderFromCartAsync(
        int buyerId, int shopId, int? addressId, int? voucherId,
        string paymentMethod, decimal shippingFee,
        List<CartOrderItem> items)
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
            var totalAmount = subTotal + shippingFee;
            var platformFee = totalAmount * (SystemSettingsHelper.Current.PlatformCommissionRate / 100m);

            var order = new Order
            {
                OrderCode = GenerateOrderCode(),
                BuyerId = buyerId,
                ShopId = shopId,
                AddressId = addressId,
                VoucherId = voucherId,
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                Discount = 0,
                TotalAmount = totalAmount,
                PlatformFee = platformFee,
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

                if ((product.StockQuantity ?? 0) < item.Quantity)
                    throw new InvalidOperationException(
                        $"Sản phẩm '{product.ProductName}' không đủ tồn kho. (Còn: {product.StockQuantity ?? 0}, yêu cầu: {item.Quantity}).");

                product.StockQuantity = (product.StockQuantity ?? 0) - item.Quantity;

                context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    ProductNameSnapshot = item.ProductName,
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
                    var buyer = await context.Users.FindAsync(buyerId);
                    if (buyer == null)
                        throw new InvalidOperationException("Không tìm thấy tài khoản người dùng.");
                    if ((buyer.WalletBalance ?? 0) < totalAmount)
                        throw new InvalidOperationException("Số dư ví không đủ để thanh toán.");
                    buyer.WalletBalance = (buyer.WalletBalance ?? 0) - totalAmount;
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
                if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                {
                    var product = await context.Products.FindAsync(detail.ProductId.Value);
                    if (product != null)
                        product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity.Value;
                }
            }

            order.OrderStatus = "Cancelled";
            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                NewStatus = "Cancelled",
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
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null || order.BuyerId == null)
                return false;

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
                if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                {
                    var product = await context.Products.FindAsync(detail.ProductId.Value);
                    if (product != null)
                        product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity.Value;
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

    public async Task<bool> UpdatePaymentSuccessAsync(int orderId, string transactionCode)
    {
        using var context = new TmdtContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var payment = await context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment == null) return false;

            payment.Status = "Paid";
            payment.TransactionCode = transactionCode;
            payment.PaidAt = DateTime.Now;

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

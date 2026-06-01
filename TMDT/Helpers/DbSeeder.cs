using System;
using System.Linq;
using TMDT.Models;

namespace TMDT.Helpers
{
    public static class DbSeeder
    {
        public static void Seed()
        {
            using (var context = new TmdtContext())
            {
                // Seed Roles
                if (!context.Roles.Any(r => r.RoleName == "Admin"))
                {
                    context.Roles.Add(new Role { RoleName = "Admin", Description = "System Administrator", IsActive = true });
                }
                if (!context.Roles.Any(r => r.RoleName == "Seller"))
                {
                    context.Roles.Add(new Role { RoleName = "Seller", Description = "Shop Owner", IsActive = true });
                }
                if (!context.Roles.Any(r => r.RoleName == "Buyer"))
                {
                    context.Roles.Add(new Role { RoleName = "Buyer", Description = "Regular Customer", IsActive = true });
                }
                context.SaveChanges();

                // Seed Admin User
                var adminEmail = "admin@myshop.com";
                if (!context.Users.Any(u => u.Email == adminEmail))
                {
                    var adminRole = context.Roles.First(r => r.RoleName == "Admin");
                    context.Users.Add(new User
                    {
                        UserCode = "USR-ADMIN",
                        Email = adminEmail,
                        FullName = "Administrator",
                        Password = PasswordHelper.HashPassword("admin123"),
                        Phone = "0123456789",
                        RoleId = adminRole.RoleId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    });
                }

                // Seed Seller User
                var sellerEmail = "seller@myshop.com";
                if (!context.Users.Any(u => u.Email == sellerEmail))
                {
                    var sellerRole = context.Roles.First(r => r.RoleName == "Seller");
                    context.Users.Add(new User
                    {
                        UserCode = "USR-SELLER",
                        Email = sellerEmail,
                        FullName = "Demo Seller",
                        Password = PasswordHelper.HashPassword("seller123"),
                        Phone = "0987654321",
                        RoleId = sellerRole.RoleId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    });
                }

                // Seed Buyer User
                var buyerEmail = "buyer@myshop.com";
                if (!context.Users.Any(u => u.Email == buyerEmail))
                {
                    var buyerRole = context.Roles.First(r => r.RoleName == "Buyer");
                    context.Users.Add(new User
                    {
                        UserCode = "USR-BUYER",
                        Email = buyerEmail,
                        FullName = "Nguyễn Văn Khách",
                        Password = PasswordHelper.HashPassword("buyer123"),
                        Phone = "0909123456",
                        RoleId = buyerRole.RoleId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    });
                }
                context.SaveChanges();

                // Seed Categories
                if (!context.Categories.Any())
                {
                    context.Categories.AddRange(
                        new Category { CategoryName = "Điện tử & Công nghệ", Icon = "E179", SortOrder = 1, IsActive = true },
                        new Category { CategoryName = "Thời trang nam", Icon = "E1A6", SortOrder = 2, IsActive = true },
                        new Category { CategoryName = "Thời trang nữ", Icon = "E1A5", SortOrder = 3, IsActive = true },
                        new Category { CategoryName = "Gia dụng & Nội thất", Icon = "E13A", SortOrder = 4, IsActive = true },
                        new Category { CategoryName = "Sức khỏe & Làm đẹp", Icon = "E1CD", SortOrder = 5, IsActive = true }
                    );
                    context.SaveChanges();
                }

                // Seed Shop (pre-approved for seller)
                var seller = context.Users.FirstOrDefault(u => u.Email == sellerEmail);
                if (seller != null && !context.Shops.Any(s => s.UserId == seller.UserId))
                {
                    context.Shops.Add(new Shop
                    {
                        UserId = seller.UserId,
                        ShopName = "TechZone Vietnam",
                        WarehouseAddress = "123 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh",
                        CommissionRate = 3.0m,
                        WalletBalance = 15000000,
                        Rating = 4.7m,
                        IsActive = true,
                        OpenedAt = DateTime.Now.AddMonths(-3)
                    });
                    context.SaveChanges();
                }

                // Seed Products for the shop
                    var shop = context.Shops.FirstOrDefault(s => s.UserId == seller.UserId);
                    var catElec = context.Categories.FirstOrDefault(c => c.CategoryName == "Điện tử & Công nghệ");
                    var catFashion = context.Categories.FirstOrDefault(c => c.CategoryName == "Thời trang nam");
                    var catHome = context.Categories.FirstOrDefault(c => c.CategoryName == "Gia dụng & Nội thất");

                if (shop != null && !context.Products.Any(p => p.ShopId == shop.ShopId))
                {
                    context.Products.AddRange(
                        new Product
                        {
                            ShopId = shop.ShopId,
                            CategoryId = catElec?.CategoryId,
                            ProductCode = "TZ-001",
                            ProductName = "Tai nghe Bluetooth Sony WH-CH520",
                            Description = "Tai nghe Bluetooth chụp tai, pin 50 giờ, microphone tích hợp.",
                            Price = 1490000,
                            OriginalPrice = 1990000,
                            StockQuantity = 50,
                            SoldCount = 23,
                            Status = "Approved",
                            ApprovedAt = DateTime.Now.AddDays(-30)
                        },
                        new Product
                        {
                            ShopId = shop.ShopId,
                            CategoryId = catElec?.CategoryId,
                            ProductCode = "TZ-002",
                            ProductName = "Bàn phím cơ Gaming RK61",
                            Description = "Bàn phím cơ 61 phím, switch blue, RGB backlight.",
                            Price = 890000,
                            OriginalPrice = 1200000,
                            StockQuantity = 30,
                            SoldCount = 15,
                            Status = "Approved",
                            ApprovedAt = DateTime.Now.AddDays(-20)
                        },
                        new Product
                        {
                            ShopId = shop.ShopId,
                            CategoryId = catFashion?.CategoryId,
                            ProductCode = "TZ-003",
                            ProductName = "Áo thun nam cotton form rộng",
                            Description = "Áo thun 100% cotton, form rộng thoáng mát, nhiều màu.",
                            Price = 199000,
                            OriginalPrice = 299000,
                            StockQuantity = 200,
                            SoldCount = 87,
                            Status = "Approved",
                            ApprovedAt = DateTime.Now.AddDays(-45)
                        },
                        new Product
                        {
                            ShopId = shop.ShopId,
                            CategoryId = catFashion?.CategoryId,
                            ProductCode = "TZ-004",
                            ProductName = "Quần jeans nam slim fit",
                            Description = "Quần jeans ống slim, chất vải co giãn thoải mái.",
                            Price = 459000,
                            OriginalPrice = 599000,
                            StockQuantity = 80,
                            SoldCount = 34,
                            Status = "Approved",
                            ApprovedAt = DateTime.Now.AddDays(-15)
                        },
                        new Product
                        {
                            ShopId = shop.ShopId,
                            CategoryId = catHome?.CategoryId,
                            ProductCode = "TZ-005",
                            ProductName = "Bình giữ nhiệt Inox 500ml",
                            Description = "Bình giữ nhiệt 12h, chất liệu inox 304 an toàn.",
                            Price = 299000,
                            OriginalPrice = 399000,
                            StockQuantity = 120,
                            SoldCount = 56,
                            Status = "Approved",
                            ApprovedAt = DateTime.Now.AddDays(-10)
                        },
                        new Product
                        {
                            ShopId = shop.ShopId,
                            CategoryId = catElec?.CategoryId,
                            ProductCode = "TZ-006",
                            ProductName = "Sạc dự phòng 10000mAh",
                            Description = "Sạc dự phòng 2 cổng USB-A, 1 cổng USB-C PD 20W.",
                            Price = 350000,
                            OriginalPrice = 450000,
                            StockQuantity = 60,
                            SoldCount = 0,
                            Status = "Pending",
                            ApprovedAt = null
                        }
                    );
                    context.SaveChanges();
                }

                // Seed Orders for the shop
                var buyer = context.Users.FirstOrDefault(u => u.Email == buyerEmail);
                if (shop != null && buyer != null && !context.Orders.Any(o => o.ShopId == shop.ShopId))
                {
                    var address = new Address
                    {
                        RecipientName = buyer.FullName,
                        Phone = buyer.Phone,
                        FullAddress = "456 Lê Lợi, Quận 1, TP. Hồ Chí Minh"
                    };
                    context.Addresses.Add(address);
                    context.SaveChanges();

                    var product1 = context.Products.First(p => p.ProductCode == "TZ-001");
                    var product3 = context.Products.First(p => p.ProductCode == "TZ-003");

                    context.Orders.AddRange(
                        new Order
                        {
                            ShopId = shop.ShopId,
                            BuyerId = buyer.UserId,
                            AddressId = address.AddressId,
                            OrderCode = "ORD-10001",
                            OrderStatus = "Completed",
                            PaymentMethod = "Thanh toán Online",
                            SubTotal = 1490000,
                            ShippingFee = 30000,
                            TotalAmount = 1520000,
                            PlatformFee = 44700,
                            OrderDate = DateTime.Now.AddDays(-5),
                            CompletedAt = DateTime.Now.AddDays(-3)
                        },
                        new Order
                        {
                            ShopId = shop.ShopId,
                            BuyerId = buyer.UserId,
                            AddressId = address.AddressId,
                            OrderCode = "ORD-10002",
                            OrderStatus = "Shipping",
                            PaymentMethod = "COD",
                            SubTotal = 598000,
                            ShippingFee = 25000,
                            TotalAmount = 623000,
                            PlatformFee = 18690,
                            OrderDate = DateTime.Now.AddDays(-2),
                            TrackingCode = "GHTK-123456789"
                        },
                        new Order
                        {
                            ShopId = shop.ShopId,
                            BuyerId = buyer.UserId,
                            AddressId = address.AddressId,
                            OrderCode = "ORD-10003",
                            OrderStatus = "Pending",
                            PaymentMethod = "Thanh toán Online",
                            SubTotal = 299000,
                            ShippingFee = 30000,
                            TotalAmount = 329000,
                            PlatformFee = 9870,
                            OrderDate = DateTime.Now.AddHours(-2)
                        },
                        new Order
                        {
                            ShopId = shop.ShopId,
                            BuyerId = buyer.UserId,
                            AddressId = address.AddressId,
                            OrderCode = "ORD-10004",
                            OrderStatus = "Cancelled",
                            PaymentMethod = "COD",
                            SubTotal = 890000,
                            ShippingFee = 35000,
                            TotalAmount = 925000,
                            PlatformFee = 0,
                            OrderDate = DateTime.Now.AddDays(-7)
                        }
                    );
                    context.SaveChanges();

                    // Add order details
                    var completedOrder = context.Orders.First(o => o.OrderCode == "ORD-10001");
                    var shippingOrder = context.Orders.First(o => o.OrderCode == "ORD-10002");
                    var pendingOrder = context.Orders.First(o => o.OrderCode == "ORD-10003");

                    context.OrderDetails.AddRange(
                        new OrderDetail { OrderId = completedOrder.OrderId, ProductNameSnapshot = product1.ProductName, Quantity = 1, UnitPrice = product1.Price, TotalPrice = product1.Price },
                        new OrderDetail { OrderId = shippingOrder.OrderId, ProductNameSnapshot = product3.ProductName, Quantity = 3, UnitPrice = product3.Price, TotalPrice = 597000 },
                        new OrderDetail { OrderId = pendingOrder.OrderId, ProductNameSnapshot = "Bình giữ nhiệt Inox 500ml", Quantity = 1, UnitPrice = 299000, TotalPrice = 299000 }
                    );
                    context.SaveChanges();
                }
            }
        }
    }
}

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
                if (!context.Roles.Any(r => r.RoleName == "Staff"))
                {
                    context.Roles.Add(new Role { RoleName = "Staff", Description = "Shop Staff/Cashier", IsActive = true });
                }
                context.SaveChanges();

                // Seed SystemConfig — cấu hình mặc định lưu vào DB
                void UpsertConfig(string key, string value, string desc)
                {
                    if (!context.SystemConfigs.Any(c => c.ConfigKey == key))
                        context.SystemConfigs.Add(new SystemConfig
                        {
                            ConfigKey   = key,
                            ConfigValue = value,
                            Description = desc,
                            UpdatedAt   = DateTime.Now
                        });
                }
                UpsertConfig("PlatformCommissionRate", "5",                  "Tỷ lệ hoa hồng nền tảng (%)");
                UpsertConfig("MinWithdrawAmount",      "100000",              "Số tiền rút tối thiểu (VNĐ)");
                UpsertConfig("MaintenanceMode",        "False",               "Chế độ bảo trì hệ thống");
                UpsertConfig("RequireProductApproval", "True",                "Bắt buộc duyệt sản phẩm trước khi hiển thị");
                UpsertConfig("SupportEmail",           "support@myshop.vn",   "Email hỗ trợ khách hàng");
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

                // Seed Staff User
                var staffEmail = "staff@myshop.com";
                if (!context.Users.Any(u => u.Email == staffEmail))
                {
                    var staffRole = context.Roles.First(r => r.RoleName == "Staff");
                    context.Users.Add(new User
                    {
                        UserCode = "USR-STAFF",
                        Email = staffEmail,
                        FullName = "Trần Thị Thu Ngân",
                        Password = PasswordHelper.HashPassword("staff123"),
                        Phone = "0988112233",
                        RoleId = staffRole.RoleId,
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
                if (seller != null)
                {
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
                }

                // Seed Orders for the shop
                // Demo order data removed to keep order management empty for real data only.
            }
        }
    }
}

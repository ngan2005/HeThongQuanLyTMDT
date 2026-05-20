using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;

namespace TMDT.ViewModels.Admin
{
    public class AdminDashboardViewModel : ViewModelBase
    {
        private TmdtContext _context;

        // Statistics Properties
        private int _totalUsers;
        public int TotalUsers { get => _totalUsers; set { _totalUsers = value; OnPropertyChanged(); } }

        private int _totalShops;
        public int TotalShops { get => _totalShops; set { _totalShops = value; OnPropertyChanged(); } }

        private int _pendingShops;
        public int PendingShops { get => _pendingShops; set { _pendingShops = value; OnPropertyChanged(); } }

        private int _totalProducts;
        public int TotalProducts { get => _totalProducts; set { _totalProducts = value; OnPropertyChanged(); } }

        private int _pendingProducts;
        public int PendingProducts { get => _pendingProducts; set { _pendingProducts = value; OnPropertyChanged(); } }

        private decimal _monthlyRevenue;
        public decimal MonthlyRevenue { get => _monthlyRevenue; set { _monthlyRevenue = value; OnPropertyChanged(); } }

        private decimal _commissionsEarned;
        public decimal CommissionsEarned { get => _commissionsEarned; set { _commissionsEarned = value; OnPropertyChanged(); } }

        private int _withdrawPendingCount;
        public int WithdrawPendingCount { get => _withdrawPendingCount; set { _withdrawPendingCount = value; OnPropertyChanged(); } }

        // Collections
        public ObservableCollection<OrderSummary> RecentOrders { get; set; }
        public ObservableCollection<ShopSummary> TopShops { get; set; }
        public ObservableCollection<RevenueTrendPoint> RevenueTrend { get; set; }
        public ObservableCollection<CategorySharePoint> CategoryShares { get; set; }

        public AdminDashboardViewModel()
        {
            RecentOrders = new ObservableCollection<OrderSummary>();
            TopShops = new ObservableCollection<ShopSummary>();
            RevenueTrend = new ObservableCollection<RevenueTrendPoint>();
            CategoryShares = new ObservableCollection<CategorySharePoint>();

            LoadRealData();
        }

        private void LoadRealData()
        {
            try
            {
                _context = new TmdtContext();
                
                // 1. Core Stats
                TotalUsers = _context.Users.Count(u => u.Role.RoleName == "User");
                TotalShops = _context.Shops.Count();
                PendingShops = _context.Shops.Count(s => s.IsActive == null);
                
                TotalProducts = _context.Products.Count();
                PendingProducts = _context.Products.Count(p => p.Status == "Pending" || p.ApprovedAt == null);

                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // Revenue & Commission (Current Month)
                var monthlyOrders = _context.Orders
                    .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Month == currentMonth && o.OrderDate.Value.Year == currentYear)
                    .ToList();

                MonthlyRevenue = monthlyOrders.Sum(o => o.TotalAmount ?? 0);
                CommissionsEarned = monthlyOrders.Sum(o => o.PlatformFee ?? 0);

                // Withdraws (Mocked count for now as there's no explicit Withdraw table referenced yet)
                WithdrawPendingCount = 0; 

                // 2. Recent Orders
                var recentOrders = _context.Orders
                    .Include(o => o.Buyer)
                    .Include(o => o.Shop)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToList();

                foreach (var order in recentOrders)
                {
                    RecentOrders.Add(new OrderSummary 
                    { 
                        OrderId = order.OrderCode ?? $"ORD-{order.OrderId}", 
                        BuyerName = order.Buyer?.FullName ?? "Khách hàng", 
                        ShopName = order.Shop?.ShopName ?? "Cửa hàng", 
                        TotalAmount = order.TotalAmount ?? 0, 
                        Commission = order.PlatformFee ?? 0, 
                        PaymentMethod = order.PaymentMethod ?? "Online", 
                        Status = order.OrderStatus ?? "Hoàn thành" 
                    });
                }

                // 3. Top Shops (By Wallet Balance or Order sum)
                var topShops = _context.Shops
                    .OrderByDescending(s => s.WalletBalance)
                    .Take(4)
                    .ToList();

                foreach (var shop in topShops)
                {
                    TopShops.Add(new ShopSummary 
                    { 
                        ShopName = shop.ShopName, 
                        TotalSales = shop.WalletBalance ?? 0, 
                        Category = "Đa ngành" // Can join with categories later
                    });
                }

                // 4. Chart Data: Revenue Trend (Last 7 Days)
                var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Now.Date.AddDays(-i)).Reverse().ToList();
                var rawTrend = new List<RevenueTrendPoint>();

                foreach (var date in last7Days)
                {
                    var dailyOrders = _context.Orders
                        .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == date)
                        .ToList();

                    rawTrend.Add(new RevenueTrendPoint 
                    { 
                        DayName = GetVietnameseDayOfWeek(date.DayOfWeek), 
                        TotalAmount = dailyOrders.Sum(o => o.TotalAmount ?? 0), 
                        Commission = dailyOrders.Sum(o => o.PlatformFee ?? 0) 
                    });
                }

                decimal maxAmount = rawTrend.Any() ? rawTrend.Max(t => t.TotalAmount) : 0;
                decimal maxCommission = rawTrend.Any() ? rawTrend.Max(t => t.Commission) : 0;

                // Ensure it's not 0 to avoid division by zero
                if (maxAmount == 0) maxAmount = 1;
                if (maxCommission == 0) maxCommission = 1;

                foreach (var p in rawTrend)
                {
                    // Add minimum height for visual if no data
                    p.AmountHeight = p.TotalAmount > 0 ? (double)(p.TotalAmount / maxAmount * 150) : 5;
                    p.CommissionHeight = p.Commission > 0 ? (double)(p.Commission / maxCommission * 150) : 5;
                    RevenueTrend.Add(p);
                }

                // 5. Chart Data: Category Share (By Product Count)
                var topCategories = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Category != null)
                    .GroupBy(p => p.Category.CategoryName)
                    .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(4)
                    .ToList();

                double totalProductsWithCat = topCategories.Sum(c => c.Count);
                if (totalProductsWithCat == 0) totalProductsWithCat = 1;

                string[] colors = { "#593AD8", "#10B981", "#EA580C", "#EC4899" };
                for (int i = 0; i < topCategories.Count; i++)
                {
                    var cat = topCategories[i];
                    double percentage = Math.Round((cat.Count / totalProductsWithCat) * 100);
                    
                    CategoryShares.Add(new CategorySharePoint 
                    { 
                        CategoryName = cat.CategoryName, 
                        Percentage = percentage, 
                        DisplayValue = $"{percentage}% ({cat.Count})", 
                        ColorHex = colors[i % colors.Length] 
                    });
                }
            }
            catch (Exception ex)
            {
                // Optionally log exception or handle it
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
        }

        private string GetVietnameseDayOfWeek(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return "Thứ 2";
                case DayOfWeek.Tuesday: return "Thứ 3";
                case DayOfWeek.Wednesday: return "Thứ 4";
                case DayOfWeek.Thursday: return "Thứ 5";
                case DayOfWeek.Friday: return "Thứ 6";
                case DayOfWeek.Saturday: return "Thứ 7";
                case DayOfWeek.Sunday: return "Chủ Nhật";
                default: return "";
            }
        }

        private void LoadMockData()
        {
            TotalUsers = 1420;
            TotalShops = 48;
            PendingShops = 5;
            TotalProducts = 890;
            PendingProducts = 24;
            MonthlyRevenue = 328400000;
            CommissionsEarned = 16420000;
            WithdrawPendingCount = 3;

            RecentOrders.Clear();
            RecentOrders.Add(new OrderSummary { OrderId = "ORD-9024", BuyerName = "Nguyễn Hoàng Nam", ShopName = "Hanoi Gadgets Store", TotalAmount = 28990000, Commission = 1449500, PaymentMethod = "Thanh toán Online", Status = "Đã hoàn thành" });
            RecentOrders.Add(new OrderSummary { OrderId = "ORD-9025", BuyerName = "Trần Thị Thanh Vân", ShopName = "Fashionista Zone", TotalAmount = 1490000, Commission = 74500, PaymentMethod = "COD (Nhận hàng trả tiền)", Status = "Đang giao hàng" });
            RecentOrders.Add(new OrderSummary { OrderId = "ORD-9026", BuyerName = "Lê Minh Tuấn", ShopName = "TechWorld Vietnam", TotalAmount = 9490000, Commission = 474500, PaymentMethod = "Thanh toán Online", Status = "Đang xử lý" });
            RecentOrders.Add(new OrderSummary { OrderId = "ORD-9027", BuyerName = "Phạm Quỳnh Chi", ShopName = "Cosmetic & Beauty", TotalAmount = 2650000, Commission = 132500, PaymentMethod = "COD", Status = "Chờ xác nhận" });

            TopShops.Clear();
            TopShops.Add(new ShopSummary { ShopName = "Hanoi Gadgets Store", TotalSales = 124500000, Category = "Công nghệ số" });
            TopShops.Add(new ShopSummary { ShopName = "TechWorld Vietnam", TotalSales = 89000000, Category = "Điện tử gia dụng" });
            TopShops.Add(new ShopSummary { ShopName = "Fashionista Zone", TotalSales = 54200000, Category = "Thời trang" });
            TopShops.Add(new ShopSummary { ShopName = "Cosmetic & Beauty", TotalSales = 41200000, Category = "Mỹ phẩm" });

            RevenueTrend.Clear();
            var rawTrend = new List<RevenueTrendPoint>
            {
                new RevenueTrendPoint { DayName = "Thứ 2", TotalAmount = 24500000, Commission = 1225000 },
                new RevenueTrendPoint { DayName = "Thứ 3", TotalAmount = 38200000, Commission = 1910000 },
                new RevenueTrendPoint { DayName = "Thứ 4", TotalAmount = 29400000, Commission = 1470000 },
                new RevenueTrendPoint { DayName = "Thứ 5", TotalAmount = 45900000, Commission = 2295000 },
                new RevenueTrendPoint { DayName = "Thứ 6", TotalAmount = 52100000, Commission = 2605000 },
                new RevenueTrendPoint { DayName = "Thứ 7", TotalAmount = 74800000, Commission = 3740000 },
                new RevenueTrendPoint { DayName = "Chủ Nhật", TotalAmount = 63500000, Commission = 3175000 }
            };

            decimal maxAmount = rawTrend.Max(t => t.TotalAmount);
            decimal maxCommission = rawTrend.Max(t => t.Commission);
            foreach (var p in rawTrend)
            {
                p.AmountHeight = maxAmount > 0 ? (double)(p.TotalAmount / maxAmount * 150) : 0;
                p.CommissionHeight = maxCommission > 0 ? (double)(p.Commission / maxCommission * 150) : 0;
                RevenueTrend.Add(p);
            }

            CategoryShares.Clear();
            CategoryShares.Add(new CategorySharePoint { CategoryName = "Công nghệ số & Thiết bị", Percentage = 45, DisplayValue = "45% (148M)", ColorHex = "#593AD8" });
            CategoryShares.Add(new CategorySharePoint { CategoryName = "Điện tử gia dụng", Percentage = 27, DisplayValue = "27% (89M)", ColorHex = "#10B981" });
            CategoryShares.Add(new CategorySharePoint { CategoryName = "Thời trang & Phụ kiện", Percentage = 16, DisplayValue = "16% (54M)", ColorHex = "#EA580C" });
            CategoryShares.Add(new CategorySharePoint { CategoryName = "Mỹ phẩm & Làm đẹp", Percentage = 12, DisplayValue = "12% (41M)", ColorHex = "#EC4899" });
        }
    }

    // Helper classes for UI Binding
    public class OrderSummary
    {
        public string OrderId { get; set; }
        public string BuyerName { get; set; }
        public string ShopName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Commission { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
    }

    public class ShopSummary
    {
        public string ShopName { get; set; }
        public decimal TotalSales { get; set; }
        public string Category { get; set; }
    }

    public class RevenueTrendPoint
    {
        public string DayName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Commission { get; set; }
        
        public double AmountHeight { get; set; }
        public double CommissionHeight { get; set; }

        public string AmountDisplay => (TotalAmount / 1000000m).ToString("N1") + "M";
        public string CommissionDisplay => (Commission / 1000m).ToString("N0") + "K";
    }

    public class CategorySharePoint
    {
        public string CategoryName { get; set; }
        public double Percentage { get; set; }
        public string DisplayValue { get; set; }
        public string ColorHex { get; set; }
    }
}


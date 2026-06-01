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

        public ObservableCollection<OrderSummary> RecentOrders { get; set; }
        public ObservableCollection<ShopSummary> TopShops { get; set; }
        public ObservableCollection<RevenueTrendPoint> RevenueTrend { get; set; }
        public ObservableCollection<CategorySharePoint> CategoryShares { get; set; }

        public string TodayDate => DateTime.Now.ToString("dd/MM/yyyy");

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

                TotalUsers = _context.Users.Count();
                TotalShops = _context.Shops.Count();
                PendingShops = _context.Shops.Count(s => s.IsActive == null);

                TotalProducts = _context.Products.Count();
                PendingProducts = _context.Products.Count(p => p.Status == "Pending" || p.ApprovedAt == null);

                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var monthlyOrders = _context.Orders
                    .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Month == currentMonth && o.OrderDate.Value.Year == currentYear)
                    .ToList();

                MonthlyRevenue = monthlyOrders.Sum(o => o.TotalAmount ?? 0);
                CommissionsEarned = monthlyOrders.Sum(o => o.PlatformFee ?? 0);

                WithdrawPendingCount = _context.WithdrawRequests.Count(w => w.Status == "Pending");

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
                        Category = "Đa ngành"
                    });
                }

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
                if (maxAmount == 0) maxAmount = 1;
                if (maxCommission == 0) maxCommission = 1;

                foreach (var p in rawTrend)
                {
                    p.AmountHeight = p.TotalAmount > 0 ? (double)(p.TotalAmount / maxAmount * 150) : 5;
                    p.CommissionHeight = p.Commission > 0 ? (double)(p.Commission / maxCommission * 150) : 5;
                    RevenueTrend.Add(p);
                }

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
    }

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

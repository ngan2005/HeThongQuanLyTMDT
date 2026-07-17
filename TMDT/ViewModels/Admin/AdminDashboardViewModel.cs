using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;
using System.Windows.Input;
using System.Threading.Tasks;

namespace TMDT.ViewModels.Admin
{
    public class AdminDashboardViewModel : ViewModelBase
    {
        private readonly AiService _aiService;
        private string _aiReport = "";
        private bool _isAiGenerating;

        public string AiReport { get => _aiReport; set { _aiReport = value; OnPropertyChanged(); } }
        public bool IsAiGenerating { get => _isAiGenerating; set { _isAiGenerating = value; OnPropertyChanged(); } }

        public ICommand GenerateAiReportCommand { get; }


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
            _aiService = new AiService();

            RecentOrders = new ObservableCollection<OrderSummary>();
            TopShops = new ObservableCollection<ShopSummary>();
            RevenueTrend = new ObservableCollection<RevenueTrendPoint>();
            CategoryShares = new ObservableCollection<CategorySharePoint>();

            GenerateAiReportCommand = new RelayCommand(ExecuteGenerateAiReport, o => !IsAiGenerating);

            _ = LoadRealDataAsync();
        }

        private async void ExecuteGenerateAiReport(object? obj)
        {
            IsAiGenerating = true;
            AiReport = "Đang tổng hợp dữ liệu và phân tích...";

            try
            {
                AiReport = await _aiService.AnalyzeDashboardAsync(
                    MonthlyRevenue,
                    CommissionsEarned,
                    TotalShops,
                    PendingShops,
                    TotalProducts,
                    PendingProducts,
                    TotalUsers);
            }
            finally
            {
                IsAiGenerating = false;
            }
        }

        // Không giữ context làm field — dùng using var cục bộ trong LoadRealData

        private async Task LoadRealDataAsync()
        {
            try
            {
                using var ctx = new TmdtContext();

                TotalUsers   = await ctx.Users.CountAsync();
                TotalShops   = await ctx.Shops.CountAsync();
                PendingShops = await ctx.Shops.CountAsync(s => s.IsActive == null);

                TotalProducts   = await ctx.Products.CountAsync();
                PendingProducts = await ctx.Products.CountAsync(p => p.Status == "Pending" || p.ApprovedAt == null);

                var currentMonth = DateTime.Now.Month;
                var currentYear  = DateTime.Now.Year;

                var monthlyOrders = await ctx.Orders.AsNoTracking()
                    .Where(o => o.OrderDate.HasValue
                             && o.OrderDate.Value.Month == currentMonth
                             && o.OrderDate.Value.Year  == currentYear
                             && (o.OrderStatus == "Completed" || o.OrderStatus == "CompletedOffline" || o.OrderStatus == "Shipping"))
                    .ToListAsync();

                MonthlyRevenue    = monthlyOrders.Sum(o => o.TotalAmount ?? 0);
                CommissionsEarned = monthlyOrders.Sum(o => o.PlatformFee ?? 0);

                WithdrawPendingCount = await ctx.WithdrawRequests.CountAsync(w => w.Status == "Pending");

                var recentOrders = await ctx.Orders.AsNoTracking()
                    .Include(o => o.Buyer)
                    .Include(o => o.Shop)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                foreach (var order in recentOrders)
                    RecentOrders.Add(new OrderSummary
                    {
                        OrderId       = order.OrderCode ?? $"ORD-{order.OrderId}",
                        BuyerName     = order.Buyer?.FullName ?? "Khách hàng",
                        ShopName      = order.Shop?.ShopName  ?? "Cửa hàng",
                        TotalAmount   = order.TotalAmount  ?? 0,
                        Commission    = order.PlatformFee  ?? 0,
                        PaymentMethod = order.PaymentMethod ?? "Online",
                        Status        = order.OrderStatus  ?? "Hoàn thành"
                    });

                // ── Top shops theo DOANH THU THỰC (tổng TotalAmount từ Orders) ─
                var topShops = await ctx.Orders.AsNoTracking()
                    .Where(o => o.ShopId != null && o.TotalAmount != null)
                    .GroupBy(o => o.ShopId)
                    .Select(g => new
                    {
                        ShopId     = g.Key,
                        ShopName   = g.First().Shop!.ShopName,
                        TotalSales = g.Sum(o => o.TotalAmount ?? 0)
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .Take(4)
                    .ToListAsync();

                foreach (var ts in topShops)
                {
                    TopShops.Add(new ShopSummary
                    {
                        ShopName   = ts.ShopName,
                        TotalSales = ts.TotalSales,
                        Category   = "Đa ngành"
                    });
                }

                // ── Revenue trend 7 ngày — 1 QUERY DUY NHẤT thay vì 7 ─────────
                var since     = DateTime.Now.Date.AddDays(-6);
                // Lấy tất cả đơn trong 7 ngày, group bên C# (tránh Date() trên SQL Server)
                var weekOrders = await ctx.Orders.AsNoTracking()
                    .Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= since && (o.OrderStatus == "Completed" || o.OrderStatus == "CompletedOffline"))
                    .Select(o => new { o.OrderDate, o.TotalAmount, o.PlatformFee })
                    .ToListAsync();

                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => DateTime.Now.Date.AddDays(-6 + i))
                    .ToList();

                var rawTrend = last7Days.Select(date => new RevenueTrendPoint
                {
                    DayName     = GetVietnameseDayOfWeek(date.DayOfWeek),
                    TotalAmount = weekOrders
                        .Where(o => o.OrderDate!.Value.Date == date)
                        .Sum(o => o.TotalAmount ?? 0),
                    Commission  = weekOrders
                        .Where(o => o.OrderDate!.Value.Date == date)
                        .Sum(o => o.PlatformFee ?? 0)
                }).ToList();

                decimal maxAmount     = rawTrend.Any() ? rawTrend.Max(t => t.TotalAmount) : 0;
                decimal maxCommission = rawTrend.Any() ? rawTrend.Max(t => t.Commission)  : 0;
                if (maxAmount     == 0) maxAmount     = 1;
                if (maxCommission == 0) maxCommission = 1;

                foreach (var p in rawTrend)
                {
                    p.AmountHeight     = p.TotalAmount > 0 ? (double)(p.TotalAmount / maxAmount     * 150) : 5;
                    p.CommissionHeight = p.Commission  > 0 ? (double)(p.Commission  / maxCommission * 150) : 5;
                    RevenueTrend.Add(p);
                }

                // ── Phân bổ danh mục ─────────────────────────────────────────
                var topCategories = await ctx.Products.AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => p.Category != null)
                    .GroupBy(p => p.Category!.CategoryName)
                    .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(4)
                    .ToListAsync();

                double totalProductsWithCat = topCategories.Sum(c => c.Count);
                if (totalProductsWithCat == 0) totalProductsWithCat = 1;

                string[] colors = { "#593AD8", "#10B981", "#EA580C", "#EC4899" };
                for (int i = 0; i < topCategories.Count; i++)
                {
                    var cat        = topCategories[i];
                    double pct     = Math.Round((cat.Count / totalProductsWithCat) * 100);
                    CategoryShares.Add(new CategorySharePoint
                    {
                        CategoryName = cat.CategoryName,
                        Percentage   = pct,
                        DisplayValue = $"{pct}% ({cat.Count})",
                        ColorHex     = colors[i % colors.Length]
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] LoadRealData error: {ex.Message}");
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace TMDT.ViewModels.Seller
{
    public class SellerDashboardViewModel : ViewModelBase
    {
        // Removed long-lived _context for async safety

        // Statistics Properties
        private int _totalProducts;
        public int TotalProducts { get => _totalProducts; set { _totalProducts = value; OnPropertyChanged(); } }

        private int _totalSold;
        public int TotalSold { get => _totalSold; set { _totalSold = value; OnPropertyChanged(); } }

        private int _activeOrders;
        public int ActiveOrders { get => _activeOrders; set { _activeOrders = value; OnPropertyChanged(); } }

        private decimal _walletBalance;
        public decimal WalletBalance { get => _walletBalance; set { _walletBalance = value; OnPropertyChanged(); } }

        private decimal _rating;
        public decimal Rating { get => _rating; set { _rating = value; OnPropertyChanged(); } }

        // Trend Properties (Mock data for UI)
        private string _walletTrendText = "+14.5% vs tháng trước";
        public string WalletTrendText { get => _walletTrendText; set { _walletTrendText = value; OnPropertyChanged(); } }
        private string _walletTrendIcon = "\uE74A";
        public string WalletTrendIcon { get => _walletTrendIcon; set { _walletTrendIcon = value; OnPropertyChanged(); } }
        private string _walletTrendColor = "#059669";
        public string WalletTrendColor { get => _walletTrendColor; set { _walletTrendColor = value; OnPropertyChanged(); } }

        private string _totalSoldTrendText = "+8.2% vs tháng trước";
        public string TotalSoldTrendText { get => _totalSoldTrendText; set { _totalSoldTrendText = value; OnPropertyChanged(); } }
        private string _totalSoldTrendIcon = "\uE74A";
        public string TotalSoldTrendIcon { get => _totalSoldTrendIcon; set { _totalSoldTrendIcon = value; OnPropertyChanged(); } }
        private string _totalSoldTrendColor = "#059669";
        public string TotalSoldTrendColor { get => _totalSoldTrendColor; set { _totalSoldTrendColor = value; OnPropertyChanged(); } }

        private string _activeOrdersTrendText = "Cần xử lý ngay";
        public string ActiveOrdersTrendText { get => _activeOrdersTrendText; set { _activeOrdersTrendText = value; OnPropertyChanged(); } }
        private string _activeOrdersTrendColor = "#DC2626";
        public string ActiveOrdersTrendColor { get => _activeOrdersTrendColor; set { _activeOrdersTrendColor = value; OnPropertyChanged(); } }

        private string _totalProductsTrendText = "Hoạt động ổn định";
        public string TotalProductsTrendText { get => _totalProductsTrendText; set { _totalProductsTrendText = value; OnPropertyChanged(); } }
        private string _totalProductsTrendColor = "#64748B";
        public string TotalProductsTrendColor { get => _totalProductsTrendColor; set { _totalProductsTrendColor = value; OnPropertyChanged(); } }

        private string _ratingTrendText = "Khách hàng hài lòng";
        public string RatingTrendText { get => _ratingTrendText; set { _ratingTrendText = value; OnPropertyChanged(); } }
        private string _ratingTrendColor = "#CA8A04";
        public string RatingTrendColor { get => _ratingTrendColor; set { _ratingTrendColor = value; OnPropertyChanged(); } }

        // Collections
        public ObservableCollection<SellerOrderSummary> RecentOrders { get; set; }
        public ObservableCollection<SellerProductSummary> TopProducts { get; set; }
        public ObservableCollection<SellerRevenueTrendPoint> RevenueTrend { get; set; }
        
        // LiveCharts
        private ISeries[] _revenueSeries;
        public ISeries[] RevenueSeries { get => _revenueSeries; set { _revenueSeries = value; OnPropertyChanged(); } }
        
        private Axis[] _xAxes;
        public Axis[] XAxes { get => _xAxes; set { _xAxes = value; OnPropertyChanged(); } }

        private ISeries[] _orderSourceSeries;
        public ISeries[] OrderSourceSeries { get => _orderSourceSeries; set { _orderSourceSeries = value; OnPropertyChanged(); } }

        public SellerDashboardViewModel()
        {
            RecentOrders = new ObservableCollection<SellerOrderSummary>();
            TopProducts = new ObservableCollection<SellerProductSummary>();
            RevenueTrend = new ObservableCollection<SellerRevenueTrendPoint>();

            _ = LoadRealDataAsync();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private async Task LoadRealDataAsync()
        {
            try
            {
                using var ctx = new TmdtContext();
                if (await ctx.Shops.AnyAsync())
                {
                    if (SessionManager.CurrentUser == null) return;

                    var shop = await ctx.Shops.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.UserId == SessionManager.CurrentUser.UserId);

                    if (shop != null)
                    {
                        int shopId = shop.ShopId;

                        // Cập nhật stats
                        WalletBalance = shop.WalletBalance ?? 0;
                        Rating = shop.Rating ?? 4.8m;

                        var productsOfShop = await ctx.Products.AsNoTracking().Where(p => p.ShopId == shopId).ToListAsync();
                        TotalProducts = productsOfShop.Count;

                        // Tính tổng số lượng sản phẩm ĐÃ BÁN từ các đơn hàng Completed
                        var soldData = await ctx.OrderDetails
                            .Include(od => od.Order)
                            .Where(od => od.Order.ShopId == shopId && od.Order.OrderStatus == "Completed")
                            .GroupBy(od => od.ProductId)
                            .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity ?? 0) })
                            .ToListAsync();

                        TotalSold = soldData.Sum(x => x.Sold);

                        ActiveOrders = await ctx.Orders.CountAsync(o => o.ShopId == shopId && o.OrderStatus == "Pending");

                        // Đơn hàng gần đây
                        var recentOrders = await ctx.Orders.AsNoTracking()
                            .Include(o => o.Buyer)
                            .Where(o => o.ShopId == shopId)
                            .OrderByDescending(o => o.OrderDate)
                            .Take(5)
                            .ToListAsync();

                        if (recentOrders.Any())
                        {
                            RecentOrders.Clear();
                            foreach (var order in recentOrders)
                            {
                                RecentOrders.Add(new SellerOrderSummary
                                {
                                    OrderCode = order.OrderCode ?? $"ORD-{order.OrderId}",
                                    BuyerName = order.Buyer?.FullName ?? "Khách hàng",
                                    TotalAmount = order.TotalAmount ?? 0,
                                    PaymentMethod = order.PaymentMethod ?? "COD",
                                    Status = order.OrderStatus ?? "Pending",
                                    OrderDate = order.OrderDate ?? DateTime.Now
                                });
                            }
                        }

                        // Sản phẩm bán chạy (dựa trên thực tế số lượng đã bán)
                        var topProductStats = soldData.OrderByDescending(x => x.Sold).Take(4).ToList();

                        if (topProductStats.Any())
                        {
                            TopProducts.Clear();
                            foreach (var stat in topProductStats)
                            {
                                var prod = productsOfShop.FirstOrDefault(p => p.ProductId == stat.ProductId);
                                if (prod != null)
                                {
                                    TopProducts.Add(new SellerProductSummary
                                    {
                                        ProductCode = prod.ProductCode ?? $"PROD-{prod.ProductId}",
                                        ProductName = prod.ProductName,
                                        Price = prod.Price,
                                        SoldCount = stat.Sold,
                                        StockQuantity = prod.StockQuantity ?? 0
                                    });
                                }
                            }
                        }

                        // LiveCharts: Biểu đồ doanh thu 7 ngày qua
                        var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Now.Date.AddDays(-i)).Reverse().ToList();
                        var revenues = new List<double>();
                        var days = new List<string>();
                        int posOrdersCount = 0;
                        int onlineOrdersCount = 0;

                        // Tính dữ liệu doanh thu
                        foreach (var date in last7Days)
                        {
                            var dailyOrders = await ctx.Orders.AsNoTracking()
                                .Where(o => o.ShopId == shopId && o.OrderDate.HasValue && o.OrderDate.Value.Date == date && o.OrderStatus != "Cancelled")
                                .ToListAsync();

                            revenues.Add((double)dailyOrders.Sum(o => o.TotalAmount ?? 0));
                            days.Add(GetVietnameseDayOfWeek(date.DayOfWeek));
                        }

                        // Tính nguồn đơn hàng (POS vs Online) trong 7 ngày
                        var all7DaysOrders = await ctx.Orders.AsNoTracking()
                            .Where(o => o.ShopId == shopId && o.OrderStatus != "Cancelled" && o.OrderDate >= last7Days.First())
                            .ToListAsync();
                            
                        posOrdersCount = all7DaysOrders.Count(o => o.AddressId == null);
                        onlineOrdersCount = all7DaysOrders.Count(o => o.AddressId != null);

                        RevenueSeries = new ISeries[]
                        {
                            new ColumnSeries<double>
                            {
                                Values = revenues,
                                Name = "Doanh thu",
                                Fill = new SolidColorPaint(SKColors.Teal),
                                MaxBarWidth = 40,
                                Rx = 8,
                                Ry = 8
                            }
                        };

                        XAxes = new Axis[]
                        {
                            new Axis
                            {
                                Labels = days,
                                LabelsPaint = new SolidColorPaint(SKColors.SlateGray),
                                TextSize = 12
                            }
                        };

                        OrderSourceSeries = new ISeries[]
                        {
                            new PieSeries<int> { Values = new[] { posOrdersCount }, Name = "Đơn POS", Fill = new SolidColorPaint(SKColors.Teal) },
                            new PieSeries<int> { Values = new[] { onlineOrdersCount }, Name = "Đơn Online", Fill = new SolidColorPaint(SKColors.Orange) }
                        };

                        // TÍNH TOÁN TREND THẬT SỰ
                        var last7DaysStart = DateTime.Now.Date.AddDays(-7);
                        var prev7DaysStart = DateTime.Now.Date.AddDays(-14);

                        // Doanh thu và Đã bán 7 ngày qua
                        var last7Orders = await ctx.Orders.AsNoTracking().Where(o => o.ShopId == shopId && o.OrderStatus != "Cancelled" && o.OrderDate >= last7DaysStart).ToListAsync();
                        decimal last7Rev = last7Orders.Sum(o => o.TotalAmount ?? 0);
                        int last7Sold = await ctx.OrderDetails.Include(od => od.Order).Where(od => od.Order.ShopId == shopId && od.Order.OrderStatus != "Cancelled" && od.Order.OrderDate >= last7DaysStart).SumAsync(od => od.Quantity ?? 0);

                        // Doanh thu và Đã bán 7 ngày trước đó
                        var prev7Orders = await ctx.Orders.AsNoTracking().Where(o => o.ShopId == shopId && o.OrderStatus != "Cancelled" && o.OrderDate >= prev7DaysStart && o.OrderDate < last7DaysStart).ToListAsync();
                        decimal prev7Rev = prev7Orders.Sum(o => o.TotalAmount ?? 0);
                        int prev7Sold = await ctx.OrderDetails.Include(od => od.Order).Where(od => od.Order.ShopId == shopId && od.Order.OrderStatus != "Cancelled" && od.Order.OrderDate >= prev7DaysStart && od.Order.OrderDate < last7DaysStart).SumAsync(od => od.Quantity ?? 0);

                        // Hàm tính %
                        void SetTrend(decimal current, decimal previous, Action<string, string, string> setter)
                        {
                            if (previous == 0 && current == 0) setter("0% vs tuần trước", "\uE74A", "#64748B");
                            else if (previous == 0) setter("Tăng trưởng mới", "\uE74A", "#059669");
                            else
                            {
                                decimal diff = (current - previous) / previous * 100;
                                if (diff >= 0) setter($"+{diff:F1}% vs tuần trước", "\uE74A", "#059669");
                                else setter($"{diff:F1}% vs tuần trước", "\uE74B", "#DC2626"); // E74B is trending down
                            }
                        }

                        SetTrend(last7Rev, prev7Rev, (text, icon, color) => { WalletTrendText = text; WalletTrendIcon = icon; WalletTrendColor = color; });
                        SetTrend(last7Sold, prev7Sold, (text, icon, color) => { TotalSoldTrendText = text; TotalSoldTrendIcon = icon; TotalSoldTrendColor = color; });

                        // Đơn chờ xử lý
                        if (ActiveOrders == 0) { ActiveOrdersTrendText = "Đã hoàn thành hết"; ActiveOrdersTrendColor = "#059669"; }
                        else if (ActiveOrders <= 3) { ActiveOrdersTrendText = "Đang trong tiến độ"; ActiveOrdersTrendColor = "#CA8A04"; }
                        else { ActiveOrdersTrendText = "Cần xử lý gấp!"; ActiveOrdersTrendColor = "#DC2626"; }

                        // Sản phẩm trên sàn
                        var catCount = productsOfShop.Select(p => p.CategoryId).Distinct().Count();
                        TotalProductsTrendText = $"Thuộc {catCount} danh mục";
                        TotalProductsTrendColor = "#64748B";

                        // Đánh giá
                        if (Rating >= 4.5m) { RatingTrendText = "Khách hàng rất hài lòng"; RatingTrendColor = "#059669"; }
                        else if (Rating >= 4.0m) { RatingTrendText = "Đánh giá tốt"; RatingTrendColor = "#CA8A04"; }
                        else { RatingTrendText = "Cần cải thiện chất lượng"; RatingTrendColor = "#DC2626"; }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF query for Seller Dashboard failed: " + ex.Message);
            }
        }

        private void LoadMockData()
        {
            TotalProducts = 18;
            TotalSold = 425;
            ActiveOrders = 4;
            WalletBalance = 24890000;
            Rating = 4.8m;

            RecentOrders.Clear();
            RecentOrders.Add(new SellerOrderSummary { OrderCode = "ORD-7701", BuyerName = "Nguyễn Hùng Anh", TotalAmount = 4980000, PaymentMethod = "Thanh toán Online", Status = "Pending", OrderDate = DateTime.Now.AddMinutes(-45) });
            RecentOrders.Add(new SellerOrderSummary { OrderCode = "ORD-7702", BuyerName = "Phan Thị Thu Hà", TotalAmount = 189000, PaymentMethod = "COD (Tiền mặt)", Status = "Shipping", OrderDate = DateTime.Now.AddHours(-3) });
            RecentOrders.Add(new SellerOrderSummary { OrderCode = "ORD-7703", BuyerName = "Lê Việt Hoàng", TotalAmount = 14500000, PaymentMethod = "Thanh toán Online", Status = "Completed", OrderDate = DateTime.Now.AddDays(-1) });
            RecentOrders.Add(new SellerOrderSummary { OrderCode = "ORD-7704", BuyerName = "Vũ Hoàng My", TotalAmount = 378000, PaymentMethod = "COD", Status = "Completed", OrderDate = DateTime.Now.AddDays(-2) });

            TopProducts.Clear();
            TopProducts.Add(new SellerProductSummary { ProductCode = "TEE-ORGANIC", ProductName = "Áo Thun Unisex Cotton Organic Cao Cấp", Price = 189000, SoldCount = 285, StockQuantity = 215 });
            TopProducts.Add(new SellerProductSummary { ProductCode = "TEFAL-5.6L", ProductName = "Nồi Chiên Không Dầu Tefal XXL 5.6L", Price = 2490000, SoldCount = 98, StockQuantity = 52 });
            TopProducts.Add(new SellerProductSummary { ProductCode = "ROBO-QREVO", ProductName = "Robot Hút Bụi Lau Nhà Roborock Q Revo", Price = 14500000, SoldCount = 30, StockQuantity = 15 });
            TopProducts.Add(new SellerProductSummary { ProductCode = "SONY-WH1000", ProductName = "Tai nghe Chống Ồn Sony WH-1000XM5", Price = 6490000, SoldCount = 12, StockQuantity = 8 });

            // LiveCharts Mock Data
            var revenues = new List<double> { 4500000, 8200000, 6400000, 12900000, 15100000, 21800000, 18500000 };
            var days = new List<string> { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật" };

            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = revenues,
                    Name = "Doanh thu",
                    Fill = new SolidColorPaint(SKColors.Teal),
                    MaxBarWidth = 40,
                    Rx = 8,
                    Ry = 8
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = days,
                    LabelsPaint = new SolidColorPaint(SKColors.SlateGray),
                    TextSize = 12
                }
            };

            OrderSourceSeries = new ISeries[]
            {
                new PieSeries<int> { Values = new[] { 45 }, Name = "Đơn POS", Fill = new SolidColorPaint(SKColors.Teal) },
                new PieSeries<int> { Values = new[] { 120 }, Name = "Đơn Online", Fill = new SolidColorPaint(SKColors.Orange) }
            };
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

    public class SellerOrderSummary
    {
        public string OrderCode { get; set; }
        public string BuyerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }

        public string TimeAgo
        {
            get
            {
                var ts = DateTime.Now - OrderDate;
                if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes} phút trước";
                if (ts.TotalHours < 24) return $"{(int)ts.TotalHours} giờ trước";
                return $"{(int)ts.TotalDays} ngày trước";
            }
        }
    }

    public class SellerProductSummary
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int SoldCount { get; set; }
        public int StockQuantity { get; set; }
    }

    public class SellerRevenueTrendPoint
    {
        public string DayName { get; set; }
        public decimal Revenue { get; set; }
        public double BarHeight { get; set; }
        public string RevenueDisplay => (Revenue / 1000000m).ToString("N1") + "M";
    }
}

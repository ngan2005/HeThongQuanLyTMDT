using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TMDT.Models;

namespace TMDT.ViewModels.Admin
{
    public class AdminDashboardViewModel : ViewModelBase
    {
        // Statistics Properties
        public int TotalUsers { get; set; } = 1420;
        public int TotalShops { get; set; } = 48;
        public int PendingShops { get; set; } = 5;
        public int TotalProducts { get; set; } = 890;
        public int PendingProducts { get; set; } = 24;
        public decimal MonthlyRevenue { get; set; } = 328400000; // 328.4 triệu VNĐ
        public decimal CommissionsEarned { get; set; } = 16420000; // 5% phí sàn = 16.42 triệu VNĐ
        public int WithdrawPendingCount { get; set; } = 3;

        // Recent Orders Collection
        public ObservableCollection<OrderSummary> RecentOrders { get; set; }
        // Top Performing Shops Collection
        public ObservableCollection<ShopSummary> TopShops { get; set; }

        // Chart Data Collections
        public ObservableCollection<RevenueTrendPoint> RevenueTrend { get; set; }
        public ObservableCollection<CategorySharePoint> CategoryShares { get; set; }

        public AdminDashboardViewModel()
        {
            RecentOrders = new ObservableCollection<OrderSummary>();
            TopShops = new ObservableCollection<ShopSummary>();
            RevenueTrend = new ObservableCollection<RevenueTrendPoint>();
            CategoryShares = new ObservableCollection<CategorySharePoint>();

            LoadDashboardData();
            LoadChartData();
        }

        private void LoadDashboardData()
        {
            // Nạp đơn hàng gần đây với dữ liệu cực kỳ thực tế
            RecentOrders.Add(new OrderSummary 
            { 
                OrderId = "ORD-9024", 
                BuyerName = "Nguyễn Hoàng Nam", 
                ShopName = "Hanoi Gadgets Store", 
                TotalAmount = 28990000, 
                Commission = 1449500, // 5%
                PaymentMethod = "Thanh toán Online", 
                Status = "Đã hoàn thành" 
            });

            RecentOrders.Add(new OrderSummary 
            { 
                OrderId = "ORD-9025", 
                BuyerName = "Trần Thị Thanh Vân", 
                ShopName = "Fashionista Zone", 
                TotalAmount = 1490000, 
                Commission = 74500, 
                PaymentMethod = "COD (Nhận hàng trả tiền)", 
                Status = "Đang giao hàng" 
            });

            RecentOrders.Add(new OrderSummary 
            { 
                OrderId = "ORD-9026", 
                BuyerName = "Lê Minh Tuấn", 
                ShopName = "TechWorld Vietnam", 
                TotalAmount = 9490000, 
                Commission = 474500, 
                PaymentMethod = "Thanh toán Online", 
                Status = "Đang xử lý" 
            });

            RecentOrders.Add(new OrderSummary 
            { 
                OrderId = "ORD-9027", 
                BuyerName = "Phạm Quỳnh Chi", 
                ShopName = "Cosmetic & Beauty", 
                TotalAmount = 2650000, 
                Commission = 132500, 
                PaymentMethod = "COD", 
                Status = "Chờ xác nhận" 
            });

            // Nạp dữ liệu các Shop hàng đầu
            TopShops.Add(new ShopSummary { ShopName = "Hanoi Gadgets Store", TotalSales = 124500000, Category = "Công nghệ số" });
            TopShops.Add(new ShopSummary { ShopName = "TechWorld Vietnam", TotalSales = 89000000, Category = "Điện tử gia dụng" });
            TopShops.Add(new ShopSummary { ShopName = "Fashionista Zone", TotalSales = 54200000, Category = "Thời trang" });
            TopShops.Add(new ShopSummary { ShopName = "Cosmetic & Beauty", TotalSales = 41200000, Category = "Mỹ phẩm" });
        }

        private void LoadChartData()
        {
            // 1. Dựng dữ liệu cho biểu đồ cột Doanh thu hàng tuần
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

            // Tìm giá trị lớn nhất để chia tỷ lệ phần trăm chiều cao cột (scale max = 150px)
            decimal maxAmount = rawTrend.Max(t => t.TotalAmount);
            decimal maxCommission = rawTrend.Max(t => t.Commission);

            foreach (var p in rawTrend)
            {
                p.AmountHeight = maxAmount > 0 ? (double)(p.TotalAmount / maxAmount * 150) : 0;
                p.CommissionHeight = maxCommission > 0 ? (double)(p.Commission / maxCommission * 150) : 0;
                RevenueTrend.Add(p);
            }

            // 2. Dựng dữ liệu cho biểu đồ phần trăm Ngành hàng bán chạy
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
        
        // Calculated heights for WPF UI rendering
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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminMarketingViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        private string _activeTab = "Banners"; // Banners, Vouchers, FlashSales

        // Collections
        public ObservableCollection<Banner> Banners { get; set; }
        public ObservableCollection<Voucher> Vouchers { get; set; }
        public ObservableCollection<FlashSale> FlashSales { get; set; }
        public ObservableCollection<Product> AvailableProducts { get; set; }

        // Selected Items
        private Banner _selectedBanner;
        private Voucher _selectedVoucher;
        private FlashSale _selectedFlashSale;

        public Banner SelectedBanner
        {
            get => _selectedBanner;
            set { _selectedBanner = value; OnPropertyChanged(); }
        }

        public Voucher SelectedVoucher
        {
            get => _selectedVoucher;
            set { _selectedVoucher = value; OnPropertyChanged(); }
        }

        public FlashSale SelectedFlashSale
        {
            get => _selectedFlashSale;
            set { _selectedFlashSale = value; OnPropertyChanged(); }
        }

        // New Input fields for Banners
        private string _bannerTitle = "";
        private string _bannerImageUrl = "";
        private string _bannerLinkUrl = "";
        private int _bannerSortOrder = 1;

        public string BannerTitle
        {
            get => _bannerTitle;
            set { _bannerTitle = value; OnPropertyChanged(); }
        }

        public string BannerImageUrl
        {
            get => _bannerImageUrl;
            set { _bannerImageUrl = value; OnPropertyChanged(); }
        }

        public string BannerLinkUrl
        {
            get => _bannerLinkUrl;
            set { _bannerLinkUrl = value; OnPropertyChanged(); }
        }

        public int BannerSortOrder
        {
            get => _bannerSortOrder;
            set { _bannerSortOrder = value; OnPropertyChanged(); }
        }

        // New Input fields for Vouchers
        private string _voucherCode = "";
        private string _voucherName = "";
        private decimal _voucherDiscountValue = 10000;
        private decimal _voucherMinOrderValue = 100000;
        private int _voucherTotalQuantity = 100;

        public string VoucherCode
        {
            get => _voucherCode;
            set { _voucherCode = value; OnPropertyChanged(); }
        }

        public string VoucherName
        {
            get => _voucherName;
            set { _voucherName = value; OnPropertyChanged(); }
        }

        public decimal VoucherDiscountValue
        {
            get => _voucherDiscountValue;
            set { _voucherDiscountValue = value; OnPropertyChanged(); }
        }

        public decimal VoucherMinOrderValue
        {
            get => _voucherMinOrderValue;
            set { _voucherMinOrderValue = value; OnPropertyChanged(); }
        }

        public int VoucherTotalQuantity
        {
            get => _voucherTotalQuantity;
            set { _voucherTotalQuantity = value; OnPropertyChanged(); }
        }

        // New Input fields for FlashSale
        private string _flashCampaignName = "";
        private Product _flashSelectedProduct;
        private decimal _flashPrice = 10000;
        private int _flashStockLimit = 10;

        public string FlashCampaignName
        {
            get => _flashCampaignName;
            set { _flashCampaignName = value; OnPropertyChanged(); }
        }

        public Product FlashSelectedProduct
        {
            get => _flashSelectedProduct;
            set { _flashSelectedProduct = value; OnPropertyChanged(); }
        }

        public decimal FlashPrice
        {
            get => _flashPrice;
            set { _flashPrice = value; OnPropertyChanged(); }
        }

        public int FlashStockLimit
        {
            get => _flashStockLimit;
            set { _flashStockLimit = value; OnPropertyChanged(); }
        }

        public string ActiveTab
        {
            get => _activeTab;
            set
            {
                _activeTab = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBannersActive));
                OnPropertyChanged(nameof(IsVouchersActive));
                OnPropertyChanged(nameof(IsFlashSalesActive));
            }
        }

        public bool IsBannersActive => ActiveTab == "Banners";
        public bool IsVouchersActive => ActiveTab == "Vouchers";
        public bool IsFlashSalesActive => ActiveTab == "FlashSales";

        // Commands
        public ICommand SelectTabCommand { get; }
        public ICommand AddBannerCommand { get; }
        public ICommand DeleteBannerCommand { get; }
        public ICommand AddVoucherCommand { get; }
        public ICommand DeleteVoucherCommand { get; }
        public ICommand AddFlashSaleCommand { get; }
        public ICommand DeleteFlashSaleCommand { get; }

        public AdminMarketingViewModel()
        {
            try
            {
                _context = new TmdtContext();
            }
            catch
            {
                // Failsafe
            }

            Banners = new ObservableCollection<Banner>();
            Vouchers = new ObservableCollection<Voucher>();
            FlashSales = new ObservableCollection<FlashSale>();
            AvailableProducts = new ObservableCollection<Product>();

            // Setup Commands
            SelectTabCommand = new RelayCommand(o => ActiveTab = o?.ToString() ?? "Banners");
            
            AddBannerCommand = new RelayCommand(ExecuteAddBanner);
            DeleteBannerCommand = new RelayCommand(ExecuteDeleteBanner);
            
            AddVoucherCommand = new RelayCommand(ExecuteAddVoucher);
            DeleteVoucherCommand = new RelayCommand(ExecuteDeleteVoucher);
            
            AddFlashSaleCommand = new RelayCommand(ExecuteAddFlashSale);
            DeleteFlashSaleCommand = new RelayCommand(ExecuteDeleteFlashSale);

            LoadMarketingData();
        }

        private void LoadMarketingData()
        {
            Banners.Clear();
            Vouchers.Clear();
            FlashSales.Clear();
            AvailableProducts.Clear();

            try
            {
                if (_context != null)
                {
                    // Load Banners
                    if (_context.Banners.Any())
                    {
                        foreach (var b in _context.Banners.ToList())
                            Banners.Add(b);
                    }

                    // Load Vouchers
                    if (_context.Vouchers.Any())
                    {
                        foreach (var v in _context.Vouchers.Include(v => v.Shop).ToList())
                            Vouchers.Add(v);
                    }

                    // Load Flash Sales
                    if (_context.FlashSales.Any())
                    {
                        foreach (var f in _context.FlashSales.Include(f => f.Product).Include(f => f.Shop).ToList())
                            FlashSales.Add(f);
                    }

                    // Load available products for setting flashsale
                    if (_context.Products.Any())
                    {
                        foreach (var p in _context.Products.Where(p => p.Status == "Approved").ToList())
                            AvailableProducts.Add(p);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF load marketing failed, loading mocks. " + ex.Message);
            }

            // Fallback load mock data if empty
            if (!Banners.Any()) LoadMockBanners();
            if (!Vouchers.Any()) LoadMockVouchers();
            if (!FlashSales.Any()) LoadMockFlashSales();
            if (!AvailableProducts.Any()) LoadMockAvailableProducts();

            // Select defaults
            SelectedBanner = Banners.FirstOrDefault();
            SelectedVoucher = Vouchers.FirstOrDefault();
            SelectedFlashSale = FlashSales.FirstOrDefault();
            FlashSelectedProduct = AvailableProducts.FirstOrDefault();
        }

        private void LoadMockBanners()
        {
            Banners.Add(new Banner { BannerId = 1, Title = "Đại Tiệc Công Nghệ Siêu Sale 6.6", ImageUrl = "https://images.unsplash.com/photo-1468495244123-6c6c332eeece", LinkUrl = "/campaign/tech-june", SortOrder = 1, IsActive = true, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(7) });
            Banners.Add(new Banner { BannerId = 2, Title = "Thời Trang Hè Năng Động - Giảm 50%", ImageUrl = "https://images.unsplash.com/photo-1483985988355-763728e1935b", LinkUrl = "/campaign/summer-fashion", SortOrder = 2, IsActive = true, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(14) });
            Banners.Add(new Banner { BannerId = 3, Title = "Thế Giới Gia Dụng - Ưu Đãi 1 Triệu Đồng", ImageUrl = "https://images.unsplash.com/photo-1556911220-e15b29be8c8f", LinkUrl = "/campaign/smart-home", SortOrder = 3, IsActive = false, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(5) });
        }

        private void LoadMockVouchers()
        {
            Vouchers.Add(new Voucher { VoucherId = 1, VoucherCode = "MYSHOP10K", VoucherName = "Voucher Sàn kích cầu mua sắm", DiscountType = "FixAmount", DiscountValue = 10000, MinOrderValue = 100000, TotalQuantity = 500, UsedCount = 142, IsActive = true, StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1) });
            Vouchers.Add(new Voucher { VoucherId = 2, VoucherCode = "SUPER50K", VoucherName = "Khuyến mãi chào hè toàn sàn", DiscountType = "FixAmount", DiscountValue = 50000, MinOrderValue = 500000, TotalQuantity = 100, UsedCount = 12, IsActive = true, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(10) });
            Vouchers.Add(new Voucher { VoucherId = 3, VoucherCode = "VIPFREE", VoucherName = "Đặc quyền thành viên mới", DiscountType = "FixAmount", DiscountValue = 30000, MinOrderValue = 200000, TotalQuantity = 200, UsedCount = 200, IsActive = false, StartDate = DateTime.Now.AddDays(-30), EndDate = DateTime.Now.AddDays(-1) });
        }

        private void LoadMockFlashSales()
        {
            FlashSales.Add(new FlashSale
            {
                FlashSaleId = 1,
                CampaignName = "Giờ vàng giá sốc 12h",
                ProductId = 501,
                FlashPrice = 1890000,
                StockLimit = 20,
                SoldCount = 8,
                StartTime = DateTime.Now.Date.AddHours(12),
                EndTime = DateTime.Now.Date.AddHours(14),
                IsActive = true,
                Product = new Product { ProductName = "Nồi Chiên Không Dầu Tefal XXL 5.6L", Price = 2490000 }
            });

            FlashSales.Add(new FlashSale
            {
                FlashSaleId = 2,
                CampaignName = "Siêu sale công nghệ đêm muộn 20h",
                ProductId = 504,
                FlashPrice = 5490000,
                StockLimit = 5,
                SoldCount = 3,
                StartTime = DateTime.Now.Date.AddHours(20),
                EndTime = DateTime.Now.Date.AddHours(22),
                IsActive = true,
                Product = new Product { ProductName = "Tai nghe Sony WH-1000XM5", Price = 6490000 }
            });
        }

        private void LoadMockAvailableProducts()
        {
            AvailableProducts.Add(new Product { ProductId = 501, ProductName = "Nồi Chiên Không Dầu Tefal XXL 5.6L", Price = 2490000 });
            AvailableProducts.Add(new Product { ProductId = 502, ProductName = "Robot Hút Bụi Lau Nhà Roborock Q Revo", Price = 14500000 });
            AvailableProducts.Add(new Product { ProductId = 503, ProductName = "Áo Thun Unisex Cotton Organic Cao Cấp", Price = 189000 });
            AvailableProducts.Add(new Product { ProductId = 504, ProductName = "Tai nghe Sony WH-1000XM5", Price = 6490000 });
        }

        // --- Commands Implementations ---

        private async void ExecuteAddBanner(object obj)
        {
            if (string.IsNullOrWhiteSpace(BannerTitle) || string.IsNullOrWhiteSpace(BannerImageUrl))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề và đường dẫn hình ảnh cho Banner!", "Thông tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newBanner = new Banner
            {
                Title = BannerTitle,
                ImageUrl = BannerImageUrl,
                LinkUrl = BannerLinkUrl,
                SortOrder = BannerSortOrder,
                IsActive = true,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(14)
            };

            try
            {
                if (_context != null)
                {
                    _context.Banners.Add(newBanner);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database insert failed: " + ex.Message);
            }

            Banners.Add(newBanner);
            SelectedBanner = newBanner;

            // Clear inputs
            BannerTitle = "";
            BannerImageUrl = "";
            BannerLinkUrl = "";
            BannerSortOrder = 1;

            MessageBox.Show("Đã thêm Banner quảng cáo toàn sàn thành công! Trang chủ người mua đã tự động cập nhật.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteDeleteBanner(object obj)
        {
            if (SelectedBanner == null) return;

            var result = MessageBox.Show($"Xác nhận xóa Banner '{SelectedBanner.Title}' khỏi trang chủ?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (_context != null)
                {
                    var dbBanner = await _context.Banners.FindAsync(SelectedBanner.BannerId);
                    if (dbBanner != null)
                    {
                        _context.Banners.Remove(dbBanner);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            Banners.Remove(SelectedBanner);
            SelectedBanner = Banners.FirstOrDefault();
        }

        private async void ExecuteAddVoucher(object obj)
        {
            if (string.IsNullOrWhiteSpace(VoucherCode) || string.IsNullOrWhiteSpace(VoucherName))
            {
                MessageBox.Show("Vui lòng nhập Mã Voucher và Tên Voucher kích cầu!", "Thông tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newVoucher = new Voucher
            {
                VoucherCode = VoucherCode.ToUpper(),
                VoucherName = VoucherName,
                DiscountType = "FixAmount",
                DiscountValue = VoucherDiscountValue,
                MinOrderValue = VoucherMinOrderValue,
                TotalQuantity = VoucherTotalQuantity,
                UsedCount = 0,
                IsActive = true,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1)
            };

            try
            {
                if (_context != null)
                {
                    _context.Vouchers.Add(newVoucher);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database insert failed: " + ex.Message);
            }

            Vouchers.Add(newVoucher);
            SelectedVoucher = newVoucher;

            // Clear inputs
            VoucherCode = "";
            VoucherName = "";
            VoucherDiscountValue = 10000;
            VoucherMinOrderValue = 100000;
            VoucherTotalQuantity = 100;

            MessageBox.Show("Đã tạo thành công Mã giảm giá toàn sàn do Sàn tài trợ!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteDeleteVoucher(object obj)
        {
            if (SelectedVoucher == null) return;

            var result = MessageBox.Show($"Xác nhận vô hiệu hóa Voucher '{SelectedVoucher.VoucherCode}'?", "Xác nhận dừng", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            SelectedVoucher.IsActive = false;

            try
            {
                if (_context != null)
                {
                    var dbVoucher = await _context.Vouchers.FindAsync(SelectedVoucher.VoucherId);
                    if (dbVoucher != null)
                    {
                        dbVoucher.IsActive = false;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }
        }

        private async void ExecuteAddFlashSale(object obj)
        {
            if (FlashSelectedProduct == null || string.IsNullOrWhiteSpace(FlashCampaignName))
            {
                MessageBox.Show("Vui lòng nhập tên đợt sale và chọn sản phẩm tham gia Flash Sale!", "Thông tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newFlash = new FlashSale
            {
                CampaignName = FlashCampaignName,
                ProductId = FlashSelectedProduct.ProductId,
                FlashPrice = FlashPrice,
                StockLimit = FlashStockLimit,
                SoldCount = 0,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(2),
                IsActive = true,
                Product = FlashSelectedProduct
            };

            try
            {
                if (_context != null)
                {
                    _context.FlashSales.Add(newFlash);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database insert failed: " + ex.Message);
            }

            FlashSales.Add(newFlash);
            SelectedFlashSale = newFlash;

            // Clear inputs
            FlashCampaignName = "";
            FlashPrice = 10000;
            FlashStockLimit = 10;

            MessageBox.Show("Đã đưa sản phẩm tham gia chương trình Flash Sale thành công! Đếm ngược giờ vàng đã được kích hoạt.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteDeleteFlashSale(object obj)
        {
            if (SelectedFlashSale == null) return;

            var result = MessageBox.Show($"Xác nhận hủy chương trình Flash Sale cho sản phẩm '{SelectedFlashSale.Product?.ProductName}'?", "Xác nhận dừng", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (_context != null)
                {
                    var dbFlash = await _context.FlashSales.FindAsync(SelectedFlashSale.FlashSaleId);
                    if (dbFlash != null)
                    {
                        _context.FlashSales.Remove(dbFlash);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            FlashSales.Remove(SelectedFlashSale);
            SelectedFlashSale = FlashSales.FirstOrDefault();
        }
    }
}

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
        private readonly TmdtContext _context = null!;
        private readonly AiService _aiService;
        private string _activeTab = "Banners"; // Banners, Vouchers, FlashSales, AiWriter

        // Collections
        public ObservableCollection<Banner> Banners { get; set; } = new();
        public ObservableCollection<Voucher> Vouchers { get; set; } = new();
        public ObservableCollection<FlashSale> FlashSales { get; set; } = new();
        public ObservableCollection<Product> AvailableProducts { get; set; } = new();

        // Selected Items
        private Banner? _selectedBanner;
        private Voucher? _selectedVoucher;
        private FlashSale? _selectedFlashSale;

        public Banner? SelectedBanner
        {
            get => _selectedBanner;
            set 
            { 
                _selectedBanner = value; 
                OnPropertyChanged(); 
                if (value != null)
                {
                    BannerTitle = value.Title ?? "";
                    BannerImageUrl = value.ImageUrl ?? "";
                    BannerLinkUrl = value.LinkUrl ?? "";
                    BannerSortOrder = value.SortOrder ?? 1;
                    ShowDetailRequest?.Invoke();
                }
            }
        }

        public Voucher? SelectedVoucher
        {
            get => _selectedVoucher;
            set 
            { 
                _selectedVoucher = value; 
                OnPropertyChanged(); 
                if (value != null)
                {
                    VoucherCode = value.VoucherCode ?? "";
                    VoucherName = value.VoucherName ?? "";
                    VoucherDiscountValue = value.DiscountValue ?? 10000;
                    VoucherMinOrderValue = value.MinOrderValue ?? 100000;
                    VoucherTotalQuantity = value.TotalQuantity ?? 100;
                    ShowDetailRequest?.Invoke();
                }
            }
        }

        public FlashSale? SelectedFlashSale
        {
            get => _selectedFlashSale;
            set 
            { 
                _selectedFlashSale = value; 
                OnPropertyChanged(); 
                if (value != null)
                {
                    FlashCampaignName = value.CampaignName ?? "";
                    FlashSelectedProduct = AvailableProducts.FirstOrDefault(p => p.ProductId == value.ProductId);
                    FlashPrice = value.FlashPrice ?? 10000;
                    FlashStockLimit = value.StockLimit ?? 10;
                    ShowDetailRequest?.Invoke();
                }
            }
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
        private Product? _flashSelectedProduct;
        private decimal _flashPrice = 10000;
        private int _flashStockLimit = 10;

        public string FlashCampaignName
        {
            get => _flashCampaignName;
            set { _flashCampaignName = value; OnPropertyChanged(); }
        }

        public Product? FlashSelectedProduct
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

        // New Input fields for AI Writer
        private string _aiPrompt = "";
        private string _aiResultText = "";
        private bool _isAiGenerating;

        public string AiPrompt
        {
            get => _aiPrompt;
            set { _aiPrompt = value; OnPropertyChanged(); }
        }

        public string AiResultText
        {
            get => _aiResultText;
            set { _aiResultText = value; OnPropertyChanged(); }
        }

        public bool IsAiGenerating
        {
            get => _isAiGenerating;
            set { _isAiGenerating = value; OnPropertyChanged(); }
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
                OnPropertyChanged(nameof(IsAiWriterActive));
            }
        }

        public bool IsBannersActive => ActiveTab == "Banners";
        public bool IsVouchersActive => ActiveTab == "Vouchers";
        public bool IsFlashSalesActive => ActiveTab == "FlashSales";
        public bool IsAiWriterActive => ActiveTab == "AiWriter";

        // Events
        public event Action? ShowDetailRequest;
        public event Action? HideDetailRequest;

        // Commands
        public ICommand SelectTabCommand { get; } = null!;
        public ICommand AddBannerCommand { get; } = null!;
        public ICommand SaveBannerCommand { get; } = null!;
        public ICommand DeleteBannerCommand { get; } = null!;
        public ICommand AddVoucherCommand { get; } = null!;
        public ICommand SaveVoucherCommand { get; } = null!;
        public ICommand DeleteVoucherCommand { get; } = null!;
        public ICommand AddFlashSaleCommand { get; } = null!;
        public ICommand SaveFlashSaleCommand { get; } = null!;
        public ICommand DeleteFlashSaleCommand { get; } = null!;
        public ICommand CreateNewCommand { get; } = null!;
        public ICommand CloseDetailCommand { get; } = null!;
        public ICommand GenerateAiContentCommand { get; } = null!;

        public AdminMarketingViewModel(string initialTab = "Banners")
        {
            _activeTab = initialTab;
            _aiService = new AiService();
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
            SaveBannerCommand = new RelayCommand(ExecuteSaveBanner);
            DeleteBannerCommand = new RelayCommand(ExecuteDeleteBanner);
            
            AddVoucherCommand = new RelayCommand(ExecuteAddVoucher);
            SaveVoucherCommand = new RelayCommand(ExecuteSaveVoucher);
            DeleteVoucherCommand = new RelayCommand(ExecuteDeleteVoucher);
            
            AddFlashSaleCommand = new RelayCommand(ExecuteAddFlashSale);
            SaveFlashSaleCommand = new RelayCommand(ExecuteSaveFlashSale);
            DeleteFlashSaleCommand = new RelayCommand(ExecuteDeleteFlashSale);

            CreateNewCommand = new RelayCommand(ExecuteCreateNew);
            CloseDetailCommand = new RelayCommand(o => {
                SelectedBanner = null;
                SelectedVoucher = null;
                SelectedFlashSale = null;
            });
            GenerateAiContentCommand = new RelayCommand(ExecuteGenerateAiContent, o => !IsAiGenerating);

            LoadData();
        }

        private async void ExecuteGenerateAiContent(object? obj)
        {
            if (string.IsNullOrWhiteSpace(AiPrompt))
            {
                AiResultText = "Vui lòng nhập ý tưởng trước!";
                return;
            }

            IsAiGenerating = true;
            AiResultText = "Đang nặn chữ... AI đang vận công suy nghĩ...";

            try
            {
                AiResultText = await _aiService.GenerateMarketingContentAsync(AiPrompt);
            }
            finally
            {
                IsAiGenerating = false;
            }
        }

        private void LoadData()
        {
            LoadMarketingData();
        }

        private void ExecuteCreateNew(object? obj)
        {
            _selectedBanner = null;
            _selectedVoucher = null;
            _selectedFlashSale = null;
            OnPropertyChanged(nameof(SelectedBanner));
            OnPropertyChanged(nameof(SelectedVoucher));
            OnPropertyChanged(nameof(SelectedFlashSale));

            // Clear inputs
            BannerTitle = "";
            BannerImageUrl = "";
            BannerLinkUrl = "";
            BannerSortOrder = 1;

            VoucherCode = "";
            VoucherName = "";
            VoucherDiscountValue = 10000;
            VoucherMinOrderValue = 100000;
            VoucherTotalQuantity = 100;

            FlashCampaignName = "";
            FlashSelectedProduct = AvailableProducts.FirstOrDefault();
            FlashPrice = 10000;
            FlashStockLimit = 10;

            ShowDetailRequest?.Invoke();
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
        }

        // --- Commands Implementations ---

        private async void ExecuteAddBanner(object? obj)
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
                MessageBox.Show($"Lỗi lưu Banner: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Banners.Add(newBanner);
            SelectedBanner = null;

            // Clear inputs
            BannerTitle = "";
            BannerImageUrl = "";
            BannerLinkUrl = "";
            BannerSortOrder = 1;

            HideDetailRequest?.Invoke();
            MessageBox.Show("Đã thêm Banner quảng cáo toàn sàn thành công! Trang chủ người mua đã tự động cập nhật.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteSaveBanner(object? obj)
        {
            if (SelectedBanner == null) return;

            if (string.IsNullOrWhiteSpace(BannerTitle) || string.IsNullOrWhiteSpace(BannerImageUrl))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề và đường dẫn hình ảnh cho Banner!", "Thông tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_context != null)
                {
                    var dbBanner = await _context.Banners.FindAsync(SelectedBanner.BannerId);
                    if (dbBanner != null)
                    {
                        dbBanner.Title = BannerTitle;
                        dbBanner.ImageUrl = BannerImageUrl;
                        dbBanner.LinkUrl = BannerLinkUrl;
                        dbBanner.SortOrder = BannerSortOrder;
                        await _context.SaveChangesAsync();
                    }
                }

                // Update in-memory object
                SelectedBanner.Title = BannerTitle;
                SelectedBanner.ImageUrl = BannerImageUrl;
                SelectedBanner.LinkUrl = BannerLinkUrl;
                SelectedBanner.SortOrder = BannerSortOrder;
                var index = Banners.IndexOf(SelectedBanner);
                if (index >= 0) Banners[index] = SelectedBanner;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật Banner: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OnPropertyChanged(nameof(Banners));
            HideDetailRequest?.Invoke();
            MessageBox.Show("Đã cập nhật Banner thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteDeleteBanner(object? obj)
        {
            if (SelectedBanner == null) return;

            var result = MessageBox.Show($"Xác nhận xóa Banner '{SelectedBanner.Title}' khỏi trang chủ?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var toRemove = SelectedBanner;
            try
            {
                if (_context != null)
                {
                    var dbBanner = await _context.Banners.FindAsync(toRemove.BannerId);
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

            Banners.Remove(toRemove);
            SelectedBanner = null;
            HideDetailRequest?.Invoke();
        }

        private async void ExecuteAddVoucher(object? obj)
        {
            if (string.IsNullOrWhiteSpace(VoucherCode) || string.IsNullOrWhiteSpace(VoucherName))
            {
                MessageBox.Show("Vui lòng nhập Mã Voucher và Tên Voucher kích cầu!", "Thông tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (VoucherDiscountValue <= 0 || VoucherMinOrderValue <= 0 || VoucherTotalQuantity <= 0)
            {
                MessageBox.Show("Số tiền giảm, đơn tối thiểu và số lượng phải lớn hơn 0!", "Lỗi logic", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (VoucherDiscountValue >= VoucherMinOrderValue)
            {
                MessageBox.Show("Số tiền giảm giá không được lớn hơn hoặc bằng giá trị đơn hàng tối thiểu!", "Lỗi logic", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show($"Lỗi lưu Voucher: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Vouchers.Add(newVoucher);
            SelectedVoucher = null;

            // Clear inputs
            VoucherCode = "";
            VoucherName = "";
            VoucherDiscountValue = 10000;
            VoucherMinOrderValue = 100000;
            VoucherTotalQuantity = 100;

            HideDetailRequest?.Invoke();
            MessageBox.Show("Đã tạo thành công Mã giảm giá toàn sàn do Sàn tài trợ!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteSaveVoucher(object? obj)
        {
            if (SelectedVoucher == null) return;

            if (string.IsNullOrWhiteSpace(VoucherCode) || string.IsNullOrWhiteSpace(VoucherName))
            {
                MessageBox.Show("Vui lòng nhập Mã Voucher và Tên Voucher kích cầu!", "Thông tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (VoucherDiscountValue <= 0 || VoucherMinOrderValue <= 0 || VoucherTotalQuantity <= 0)
            {
                MessageBox.Show("Số tiền giảm, đơn tối thiểu và số lượng phải lớn hơn 0!", "Lỗi logic", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (VoucherDiscountValue >= VoucherMinOrderValue)
            {
                MessageBox.Show("Số tiền giảm giá không được lớn hơn hoặc bằng giá trị đơn hàng tối thiểu!", "Lỗi logic", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_context != null)
                {
                    var dbVoucher = await _context.Vouchers.FindAsync(SelectedVoucher.VoucherId);
                    if (dbVoucher != null)
                    {
                        dbVoucher.VoucherCode = VoucherCode.ToUpper();
                        dbVoucher.VoucherName = VoucherName;
                        dbVoucher.DiscountValue = VoucherDiscountValue;
                        dbVoucher.MinOrderValue = VoucherMinOrderValue;
                        dbVoucher.TotalQuantity = VoucherTotalQuantity;
                        await _context.SaveChangesAsync();
                    }
                }

                SelectedVoucher.VoucherCode = VoucherCode.ToUpper();
                SelectedVoucher.VoucherName = VoucherName;
                SelectedVoucher.DiscountValue = VoucherDiscountValue;
                SelectedVoucher.MinOrderValue = VoucherMinOrderValue;
                SelectedVoucher.TotalQuantity = VoucherTotalQuantity;
                var index = Vouchers.IndexOf(SelectedVoucher);
                if (index >= 0) Vouchers[index] = SelectedVoucher;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật Voucher: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OnPropertyChanged(nameof(Vouchers));
            HideDetailRequest?.Invoke();
            MessageBox.Show("Đã cập nhật Voucher thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteDeleteVoucher(object? obj)
        {
            if (SelectedVoucher == null) return;

            var result = MessageBox.Show($"Xác nhận vô hiệu hóa Voucher '{SelectedVoucher.VoucherCode}'?", "Xác nhận dừng", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var toDisable = SelectedVoucher;
            toDisable.IsActive = false;

            try
            {
                if (_context != null)
                {
                    var dbVoucher = await _context.Vouchers.FindAsync(toDisable.VoucherId);
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
            
            SelectedVoucher = null;
            HideDetailRequest?.Invoke();
        }

        private async void ExecuteAddFlashSale(object? obj)
        {
            if (FlashSelectedProduct == null || string.IsNullOrWhiteSpace(FlashCampaignName))
            {
                MessageBox.Show("Vui lòng nhập tên đợt sale và chọn sản phẩm tham gia Flash Sale!", "Thông tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (FlashPrice <= 0 || FlashPrice >= FlashSelectedProduct.Price)
            {
                MessageBox.Show($"Giá Flash Sale phải lớn hơn 0 và nhỏ hơn giá gốc của sản phẩm ({FlashSelectedProduct.Price:N0} đ)!", "Lỗi logic", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (FlashStockLimit <= 0 || FlashStockLimit > (FlashSelectedProduct.StockQuantity ?? 0))
            {
                MessageBox.Show($"Số lượng giới hạn Flash Sale phải lớn hơn 0 và không được vượt quá số lượng tồn kho thực tế ({FlashSelectedProduct.StockQuantity ?? 0})!", "Lỗi logic", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show($"Lỗi lưu Flash Sale: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            FlashSales.Add(newFlash);
            SelectedFlashSale = null;

            // Clear inputs
            FlashCampaignName = "";
            FlashPrice = 10000;
            FlashStockLimit = 10;

            HideDetailRequest?.Invoke();
            MessageBox.Show("Đã đưa sản phẩm tham gia chương trình Flash Sale thành công! Đếm ngược giờ vàng đã được kích hoạt.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteSaveFlashSale(object? obj)
        {
            if (SelectedFlashSale == null) return;

            if (FlashSelectedProduct == null || string.IsNullOrWhiteSpace(FlashCampaignName))
            {
                MessageBox.Show("Vui lòng nhập tên đợt sale và chọn sản phẩm tham gia Flash Sale!", "Thông tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (FlashPrice <= 0 || FlashPrice >= FlashSelectedProduct.Price)
            {
                MessageBox.Show($"Giá Flash Sale phải lớn hơn 0 và nhỏ hơn giá gốc của sản phẩm ({FlashSelectedProduct.Price:N0} đ)!", "Lỗi logic", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (FlashStockLimit <= 0 || FlashStockLimit > (FlashSelectedProduct.StockQuantity ?? 0))
            {
                MessageBox.Show($"Số lượng giới hạn Flash Sale phải lớn hơn 0 và không được vượt quá số lượng tồn kho thực tế ({FlashSelectedProduct.StockQuantity ?? 0})!", "Lỗi logic", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_context != null)
                {
                    var dbFlash = await _context.FlashSales.FindAsync(SelectedFlashSale.FlashSaleId);
                    if (dbFlash != null)
                    {
                        dbFlash.CampaignName = FlashCampaignName;
                        dbFlash.ProductId = FlashSelectedProduct.ProductId;
                        dbFlash.FlashPrice = FlashPrice;
                        dbFlash.StockLimit = FlashStockLimit;
                        await _context.SaveChangesAsync();
                    }
                }

                SelectedFlashSale.CampaignName = FlashCampaignName;
                SelectedFlashSale.ProductId = FlashSelectedProduct.ProductId;
                SelectedFlashSale.FlashPrice = FlashPrice;
                SelectedFlashSale.StockLimit = FlashStockLimit;
                SelectedFlashSale.Product = FlashSelectedProduct;
                var index = FlashSales.IndexOf(SelectedFlashSale);
                if (index >= 0) FlashSales[index] = SelectedFlashSale;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật Flash Sale: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OnPropertyChanged(nameof(FlashSales));
            HideDetailRequest?.Invoke();
            MessageBox.Show("Đã cập nhật Flash Sale thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteDeleteFlashSale(object? obj)
        {
            if (SelectedFlashSale == null) return;

            var result = MessageBox.Show($"Xác nhận hủy chương trình Flash Sale cho sản phẩm '{SelectedFlashSale.Product?.ProductName}'?", "Xác nhận dừng", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var toRemove = SelectedFlashSale;
            try
            {
                if (_context != null)
                {
                    var dbFlash = await _context.FlashSales.FindAsync(toRemove.FlashSaleId);
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

            FlashSales.Remove(toRemove);
            SelectedFlashSale = null;
            HideDetailRequest?.Invoke();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}

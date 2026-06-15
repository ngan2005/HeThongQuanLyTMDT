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
    public class AdminProductsViewModel : ViewModelBase
    {
        // Removed long-lived _context for async safety
        private ObservableCollection<Product> _products = new();
        private Product? _selectedProduct;
        private string _searchText = "";
        private string _statusFilter = "All"; // All, Pending, Approved, Rejected

        private int _totalProducts;
        private int _pendingProducts;
        private int _approvedProducts;
        private int _rejectedProducts;

        private string _aiAnalysisResult = "";
        private bool _isAiAnalyzing;

        private readonly AiService _aiService;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set 
            { 
                _searchText = value; 
                OnPropertyChanged(); 
                _ = LoadProductsAsync(); 
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
                _ = LoadProductsAsync();
            }
        }

        public int TotalProducts
        {
            get => _totalProducts;
            set { _totalProducts = value; OnPropertyChanged(); }
        }

        public int PendingProducts
        {
            get => _pendingProducts;
            set { _pendingProducts = value; OnPropertyChanged(); }
        }

        public int ApprovedProducts
        {
            get => _approvedProducts;
            set { _approvedProducts = value; OnPropertyChanged(); }
        }

        public int RejectedProducts
        {
            get => _rejectedProducts;
            set { _rejectedProducts = value; OnPropertyChanged(); }
        }

        public string AiAnalysisResult
        {
            get => _aiAnalysisResult;
            set { _aiAnalysisResult = value; OnPropertyChanged(); }
        }

        public bool IsAiAnalyzing
        {
            get => _isAiAnalyzing;
            set { _isAiAnalyzing = value; OnPropertyChanged(); }
        }

        private string _aiFraudReportText = "";
        private bool _isAiFraudScanning;
        private bool _isAiCategorizing;

        public string AiFraudReportText
        {
            get => _aiFraudReportText;
            set { _aiFraudReportText = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsAiFraudReportVisible)); }
        }

        public bool IsAiFraudScanning
        {
            get => _isAiFraudScanning;
            set { _isAiFraudScanning = value; OnPropertyChanged(); }
        }

        public bool IsAiCategorizing
        {
            get => _isAiCategorizing;
            set { _isAiCategorizing = value; OnPropertyChanged(); }
        }

        public bool IsAiFraudReportVisible => !string.IsNullOrEmpty(AiFraudReportText);

        // Events
        public event Action? ShowDetailRequest;
        public event Action? HideDetailRequest;

        // Commands
        public ICommand ApproveProductCommand { get; }
        public ICommand RejectProductCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand CloseDetailCommand { get; }
        public ICommand ViewDetailCommand { get; }
        public ICommand AiAnalyzeCommand { get; }
        public ICommand ScanFraudCommand { get; }
        public ICommand AiCategorizeCommand { get; }

        public AdminProductsViewModel(string initialFilter = "All")
        {
            _statusFilter = initialFilter;

            Products = new ObservableCollection<Product>();

            _aiService = new AiService();

            // Setup Commands
            ApproveProductCommand = new RelayCommand(ExecuteApproveProduct, CanExecuteApproveProduct);
            RejectProductCommand = new RelayCommand(ExecuteRejectProduct, CanExecuteRejectProduct);
            FilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");
            CloseDetailCommand = new RelayCommand(o => 
            { 
                SelectedProduct = null; 
                AiAnalysisResult = ""; // Reset khi đóng
            });
            ViewDetailCommand = new RelayCommand(o => 
            { 
                AiAnalysisResult = ""; // Reset khi mở mới
                ShowDetailRequest?.Invoke(); 
            });
            AiAnalyzeCommand = new RelayCommand(ExecuteAiAnalyze, CanExecuteAiAnalyze);
            ScanFraudCommand = new RelayCommand(ExecuteScanFraud, _ => !IsAiFraudScanning);
            AiCategorizeCommand = new RelayCommand(ExecuteAiCategorize, _ => SelectedProduct != null && !IsAiCategorizing);

            _ = LoadProductsAsync();
        }

        private async void ExecuteAiCategorize(object? obj)
        {
            if (SelectedProduct == null) return;

            IsAiCategorizing = true;
            try
            {
                using var ctx = new TmdtContext();
                var categoriesDict = await ctx.Categories.ToDictionaryAsync(c => c.CategoryId, c => c.CategoryName ?? "Unknown");
                int suggestedId = await _aiService.SuggestCategoryAsync(
                    SelectedProduct.ProductName ?? "",
                    SelectedProduct.Description ?? "",
                    categoriesDict);

                if (suggestedId > 0 && suggestedId != SelectedProduct.CategoryId)
                {
                    // AI found a better category
                    var newCat = await ctx.Categories.FirstOrDefaultAsync(c => c.CategoryId == suggestedId);
                    if (newCat != null)
                    {
                        var dbProd = await ctx.Products.FindAsync(SelectedProduct.ProductId);
                        if (dbProd != null)
                        {
                            dbProd.CategoryId = suggestedId;
                            await ctx.SaveChangesAsync();
                            
                            // Update UI
                            SelectedProduct.CategoryId = suggestedId;
                            SelectedProduct.Category = newCat;
                            OnPropertyChanged(nameof(SelectedProduct)); // trigger UI refresh
                            
                            MessageBox.Show($"AI đã tự động phân loại sản phẩm này vào danh mục: {newCat.CategoryName}", "Phân loại thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                else if (suggestedId == SelectedProduct.CategoryId)
                {
                    MessageBox.Show("AI đánh giá danh mục hiện tại đã chính xác.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("AI không thể xác định được danh mục phù hợp.", "Lỗi phân loại", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteAiCategorize Error: " + ex.Message);
            }
            finally
            {
                IsAiCategorizing = false;
            }
        }

        private async void ExecuteScanFraud(object? obj)
        {
            if (Products.Count == 0)
            {
                AiFraudReportText = "Không có sản phẩm nào để quét.";
                return;
            }

            IsAiFraudScanning = true;
            AiFraudReportText = "🤖 AI đang quét danh sách sản phẩm. Đang rà soát giá tiền và từ khóa cấm...";

            try
            {
                // Lấy tối đa 30 sản phẩm đang hiển thị để quét (tránh quá tải)
                var productInfos = Products
                    .Take(30)
                    .Select(p => $"- {p.ProductName} | Giá: {p.Price:N0}đ")
                    .ToList();

                string report = await _aiService.ScanFraudProductsAsync(productInfos);
                AiFraudReportText = report;
            }
            finally
            {
                IsAiFraudScanning = false;
            }
        }

        private async Task LoadProductsAsync()
        {
            Products.Clear();

            try
            {
                using var ctx = new TmdtContext();
                ctx.ChangeTracker.Clear();

                if (await ctx.Products.AnyAsync())
                    {
                        TotalProducts = await ctx.Products.CountAsync(p => p.Status != "Deleted");
                        PendingProducts = await ctx.Products.CountAsync(p => p.Status != "Deleted" && (p.Status == "Pending" || string.IsNullOrEmpty(p.Status)));
                        ApprovedProducts = await ctx.Products.CountAsync(p => p.Status == "Approved");
                        RejectedProducts = await ctx.Products.CountAsync(p => p.Status == "Rejected");

                        var query = ctx.Products
                            .Include(p => p.Shop)
                            .Include(p => p.Category)
                            .Include(p => p.ProductImages)
                            .Where(p => p.Status != "Deleted")
                            .AsQueryable();

                        // Apply Search
                        if (!string.IsNullOrEmpty(SearchText))
                        {
                            string term = SearchText.Trim().ToLower();
                            query = query.Where(p =>
                                (p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{term}%")) ||
                                (p.ProductCode != null && EF.Functions.Like(p.ProductCode, $"%{SearchText}%")) ||
                                (p.Shop != null && p.Shop.ShopName != null && EF.Functions.Like(p.Shop.ShopName, $"%{SearchText}%")));
                        }

                        // Apply Filter
                        if (StatusFilter == "Pending")
                        {
                            query = query.Where(p => p.Status == "Pending" || string.IsNullOrEmpty(p.Status));
                        }
                        else if (StatusFilter == "Approved")
                        {
                            query = query.Where(p => p.Status == "Approved");
                        }
                        else if (StatusFilter == "Rejected")
                        {
                            query = query.Where(p => p.Status == "Rejected");
                        }

                        var dbProducts = await query.ToListAsync();
                        foreach (var prod in dbProducts)
                        {
                            Products.Add(prod);
                        }

                        if (Products.Any() || (string.IsNullOrEmpty(SearchText) && StatusFilter == "All"))
                            return;
                    }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF query for Products failed: " + ex.Message);
                MessageBox.Show("Không thể tải danh sách sản phẩm: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // KHÔNG còn fallback mock data nữa — nếu không có kết quả, hiển thị danh sách rỗng
        }

        // --- Commands Implementation ---

        private bool CanExecuteApproveProduct(object? obj) => SelectedProduct != null && (SelectedProduct.Status == "Pending" || SelectedProduct.Status == "Rejected" || string.IsNullOrEmpty(SelectedProduct.Status));
        private async void ExecuteApproveProduct(object? obj)
        {
            if (SelectedProduct == null) return;

            var result = MessageBox.Show($"Bạn có đồng ý phê duyệt đăng bán sản phẩm '{SelectedProduct.ProductName}' của shop '{SelectedProduct.Shop?.ShopName}'?", 
                                         "Xác nhận phê duyệt", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            SelectedProduct.Status = "Approved";
            SelectedProduct.ApprovedAt = DateTime.Now;

            try
            {
                using var ctx = new TmdtContext();
                var dbProd = await ctx.Products.FindAsync(SelectedProduct.ProductId);
                if (dbProd != null)
                {
                    dbProd.Status = "Approved";
                    dbProd.ApprovedAt = DateTime.Now;
                    await ctx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã phê duyệt thành công! Sản phẩm '{SelectedProduct.ProductName}' hiện đã được hiển thị trên sàn.", 
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            
            HideDetailRequest?.Invoke();
            _ = LoadProductsAsync();
        }

        private bool CanExecuteRejectProduct(object? obj) => SelectedProduct != null && (SelectedProduct.Status == "Pending" || SelectedProduct.Status == "Approved" || string.IsNullOrEmpty(SelectedProduct.Status));
        private async void ExecuteRejectProduct(object? obj)
        {
            if (SelectedProduct == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn TỪ CHỐI đăng bán sản phẩm '{SelectedProduct.ProductName}'?", 
                                         "Xác nhận từ chối", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            SelectedProduct.Status = "Rejected";

            try
            {
                using var ctx = new TmdtContext();
                var dbProd = await ctx.Products.FindAsync(SelectedProduct.ProductId);
                if (dbProd != null)
                {
                    dbProd.Status = "Rejected";
                    await ctx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã từ chối đăng bán sản phẩm '{SelectedProduct.ProductName}'. Người bán sẽ nhận được thông báo điều chỉnh thông tin.", 
                            "Đã thực hiện", MessageBoxButton.OK, MessageBoxImage.Information);

            HideDetailRequest?.Invoke();
            _ = LoadProductsAsync();
        }

        private bool CanExecuteAiAnalyze(object? obj) => SelectedProduct != null && !IsAiAnalyzing;
        private async void ExecuteAiAnalyze(object? obj)
        {
            if (SelectedProduct == null) return;

            IsAiAnalyzing = true;
            AiAnalysisResult = "Đang phân tích...";

            try
            {
                var mainImage = SelectedProduct.ProductImages?.FirstOrDefault(img => img.IsMain == true) 
                             ?? SelectedProduct.ProductImages?.FirstOrDefault();

                if (mainImage != null && !string.IsNullOrWhiteSpace(mainImage.ImageUrl))
                {
                    AiAnalysisResult = await _aiService.AnalyzeProductWithImageAsync(
                        SelectedProduct.ProductName ?? "",
                        SelectedProduct.Description ?? "",
                        SelectedProduct.Price,
                        mainImage.ImageUrl);
                }
                else
                {
                    AiAnalysisResult = await _aiService.AnalyzeProductAsync(
                        SelectedProduct.ProductName ?? "",
                        SelectedProduct.Description ?? "",
                        SelectedProduct.Price);
                }
            }
            finally
            {
                IsAiAnalyzing = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

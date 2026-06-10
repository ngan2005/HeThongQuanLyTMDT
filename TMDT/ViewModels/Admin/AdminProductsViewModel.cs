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
        private readonly TmdtContext _context = null!;
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
                LoadProducts(); 
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
                LoadProducts();
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

        public AdminProductsViewModel(string initialFilter = "All")
        {
            _statusFilter = initialFilter;
            try
            {
                _context = new TmdtContext();
            }
            catch
            {
                // Failsafe
            }

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

            LoadProducts();
        }

        private void LoadProducts()
        {
            Products.Clear();

            try
            {
                if (_context != null)
                {
                    _context.ChangeTracker.Clear();

                    if (_context.Products.Any())
                    {
                        TotalProducts = _context.Products.Count(p => p.Status != "Deleted");
                        PendingProducts = _context.Products.Count(p => p.Status != "Deleted" && (p.Status == "Pending" || string.IsNullOrEmpty(p.Status)));
                        ApprovedProducts = _context.Products.Count(p => p.Status == "Approved");
                        RejectedProducts = _context.Products.Count(p => p.Status == "Rejected");

                        var query = _context.Products
                            .Include(p => p.Shop)
                            .Include(p => p.Category)
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

                        var dbProducts = query.ToList();
                        foreach (var prod in dbProducts)
                        {
                            Products.Add(prod);
                        }

                        if (Products.Any() || (string.IsNullOrEmpty(SearchText) && StatusFilter == "All"))
                            return;
                    }
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
                if (_context != null)
                {
                    var dbProd = await _context.Products.FindAsync(SelectedProduct.ProductId);
                    if (dbProd != null)
                    {
                        dbProd.Status = "Approved";
                        dbProd.ApprovedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã phê duyệt thành công! Sản phẩm '{SelectedProduct.ProductName}' hiện đã được hiển thị trên sàn.", 
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            
            HideDetailRequest?.Invoke();
            LoadProducts();
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
                if (_context != null)
                {
                    var dbProd = await _context.Products.FindAsync(SelectedProduct.ProductId);
                    if (dbProd != null)
                    {
                        dbProd.Status = "Rejected";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã từ chối đăng bán sản phẩm '{SelectedProduct.ProductName}'. Người bán sẽ nhận được thông báo điều chỉnh thông tin.", 
                            "Đã thực hiện", MessageBoxButton.OK, MessageBoxImage.Information);

            HideDetailRequest?.Invoke();
            LoadProducts();
        }

        private bool CanExecuteAiAnalyze(object? obj) => SelectedProduct != null && !IsAiAnalyzing;
        private async void ExecuteAiAnalyze(object? obj)
        {
            if (SelectedProduct == null) return;

            IsAiAnalyzing = true;
            AiAnalysisResult = "Đang phân tích...";

            try
            {
                AiAnalysisResult = await _aiService.AnalyzeProductAsync(
                    SelectedProduct.ProductName ?? "",
                    SelectedProduct.Description ?? "",
                    SelectedProduct.Price);
            }
            finally
            {
                IsAiAnalyzing = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}

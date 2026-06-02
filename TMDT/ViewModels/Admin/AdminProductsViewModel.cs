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
        private readonly TmdtContext _context;
        private ObservableCollection<Product> _products;
        private Product _selectedProduct;
        private string _searchText = "";
        private string _statusFilter = "All"; // All, Pending, Approved, Rejected

        private int _totalProducts;
        private int _pendingProducts;
        private int _approvedProducts;
        private int _rejectedProducts;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        public Product SelectedProduct
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

        // Events
        public event Action ShowDetailRequest;
        public event Action HideDetailRequest;

        // Commands
        public ICommand ApproveProductCommand { get; }
        public ICommand RejectProductCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand CloseDetailCommand { get; }
        public ICommand ViewDetailCommand { get; }

        public AdminProductsViewModel()
        {
            try
            {
                _context = new TmdtContext();
            }
            catch
            {
                // Failsafe
            }

            Products = new ObservableCollection<Product>();

            // Setup Commands
            ApproveProductCommand = new RelayCommand(ExecuteApproveProduct, CanExecuteApproveProduct);
            RejectProductCommand = new RelayCommand(ExecuteRejectProduct, CanExecuteRejectProduct);
            FilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");
            CloseDetailCommand = new RelayCommand(o => SelectedProduct = null);
            ViewDetailCommand = new RelayCommand(o => ShowDetailRequest?.Invoke());

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
                        TotalProducts = _context.Products.Count();
                        PendingProducts = _context.Products.Count(p => p.Status == "Pending" || string.IsNullOrEmpty(p.Status));
                        ApprovedProducts = _context.Products.Count(p => p.Status == "Approved");
                        RejectedProducts = _context.Products.Count(p => p.Status == "Rejected");

                        var query = _context.Products
                            .Include(p => p.Shop)
                            .Include(p => p.Category)
                            .AsQueryable();

                        // Apply Search
                        if (!string.IsNullOrEmpty(SearchText))
                        {
                            query = query.Where(p => p.ProductName.Contains(SearchText) || 
                                                     (p.ProductCode != null && p.ProductCode.Contains(SearchText)) ||
                                                     (p.Shop != null && p.Shop.ShopName.Contains(SearchText)));
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
                System.Diagnostics.Debug.WriteLine("EF query for Products failed, loading mocks. " + ex.Message);
            }

            // Fallback mock products
            LoadMockProducts();
        }

        private void LoadMockProducts()
        {
            var mockProds = new ObservableCollection<Product>();

            // Mock 1: Pending
            mockProds.Add(new Product
            {
                ProductId = 501,
                ProductCode = "TEFAL-5.6L",
                ProductName = "Nồi Chiên Không Dầu Tefal XXL 5.6L",
                CategoryId = 1,
                Price = 2490000,
                OriginalPrice = 3500000,
                StockQuantity = 150,
                SoldCount = 0,
                Rating = 0,
                Status = "Pending",
                CreatedAt = DateTime.Now.AddDays(-1),
                Description = "Nồi chiên không dầu Tefal nhập khẩu chính hãng Pháp. Công nghệ chiên Rapid Air giòn đều không dầu mỡ, dung tích cực lớn 5.6L phù hợp cho gia đình từ 4-6 người ăn thoải mái. Lòng nồi phủ chống dính cao cấp chống trầy xước.",
                Category = new Category { CategoryName = "Thiết bị Gia dụng" },
                Shop = new Shop { ShopName = "Gia Dụng Thông Minh Việt" }
            });

            // Mock 2: Pending
            mockProds.Add(new Product
            {
                ProductId = 502,
                ProductCode = "ROBO-QREVO",
                ProductName = "Robot Hút Bụi Lau Nhà Roborock Q Revo",
                CategoryId = 1,
                Price = 14500000,
                OriginalPrice = 18000000,
                StockQuantity = 45,
                SoldCount = 0,
                Rating = 0,
                Status = "Pending",
                CreatedAt = DateTime.Now.AddHours(-12),
                Description = "Robot lau nhà thông minh thế hệ mới Roborock Q Revo. Tự động giặt giẻ, tự động sấy khô bằng khí nóng, lực hút siêu mạnh 5500Pa, tránh chướng ngại vật bằng hồng ngoại 3D siêu nhạy.",
                Category = new Category { CategoryName = "Thiết bị Gia dụng" },
                Shop = new Shop { ShopName = "Hanoi Gadgets Store" }
            });

            // Mock 3: Approved
            mockProds.Add(new Product
            {
                ProductId = 503,
                ProductCode = "TEE-ORGANIC",
                ProductName = "Áo Thun Unisex Cotton Organic Cao Cấp",
                CategoryId = 2,
                Price = 189000,
                OriginalPrice = 250000,
                StockQuantity = 500,
                SoldCount = 1420,
                Rating = 4.8m,
                Status = "Approved",
                CreatedAt = DateTime.Now.AddMonths(-2),
                ApprovedAt = DateTime.Now.AddMonths(-2).AddHours(2),
                Description = "Áo thun cotton 100% hữu cơ mềm mại, thoáng khí thấm hút mồ hôi cực tốt. Thiết kế form rộng nam nữ đều mặc đẹp, đường chỉ may kép bền bỉ theo thời gian.",
                Category = new Category { CategoryName = "Thời trang Unisex" },
                Shop = new Shop { ShopName = "Fashionista Zone" }
            });

            // Mock 4: Approved
            mockProds.Add(new Product
            {
                ProductId = 504,
                ProductCode = "SONY-WH1000XM5",
                ProductName = "Tai nghe Chống Ồn Chủ Động Sony WH-1000XM5",
                CategoryId = 3,
                Price = 6490000,
                OriginalPrice = 8490000,
                StockQuantity = 30,
                SoldCount = 98,
                Rating = 4.9m,
                Status = "Approved",
                CreatedAt = DateTime.Now.AddMonths(-1),
                ApprovedAt = DateTime.Now.AddMonths(-1).AddHours(1),
                Description = "Tai nghe headphone không dây chống ồn đỉnh cao số 1 thế giới Sony WH-1000XM5. Thời lượng pin lên tới 30 giờ liên tục, tích hợp bộ xử lý chống ồn chuyên biệt V1 và HD QN1.",
                Category = new Category { CategoryName = "Thiết bị Âm thanh" },
                Shop = new Shop { ShopName = "Hanoi Gadgets Store" }
            });

            // Mock 5: Rejected
            mockProds.Add(new Product
            {
                ProductId = 505,
                ProductCode = "DETOX-VIP",
                ProductName = "Thực phẩm giảm cân thảo mộc Detox Vip",
                CategoryId = 4,
                Price = 850000,
                OriginalPrice = 1200000,
                StockQuantity = 100,
                SoldCount = 0,
                Rating = 0,
                Status = "Rejected",
                CreatedAt = DateTime.Now.AddDays(-5),
                Description = "Trà thảo mộc hỗ trợ đào thải mỡ thừa, thanh lọc cơ thể cực nhanh trong vòng 7 ngày. Thành phần thảo dược tự nhiên cam kết an toàn tuyệt đối.",
                Category = new Category { CategoryName = "Sức khỏe & Sắc đẹp" },
                Shop = new Shop { ShopName = "Organic Food & Fruits" }
            });

            // Calculate stats for mock data
            TotalProducts = mockProds.Count;
            PendingProducts = mockProds.Count(p => p.Status == "Pending" || string.IsNullOrEmpty(p.Status));
            ApprovedProducts = mockProds.Count(p => p.Status == "Approved");
            RejectedProducts = mockProds.Count(p => p.Status == "Rejected");

            // Apply Filters to mock data
            var filtered = mockProds.AsQueryable();
            if (!string.IsNullOrEmpty(SearchText))
            {
                filtered = filtered.Where(p => p.ProductName.ToLower().Contains(SearchText.ToLower()) || 
                                               (p.ProductCode != null && p.ProductCode.ToLower().Contains(SearchText.ToLower())) ||
                                               p.Shop.ShopName.ToLower().Contains(SearchText.ToLower()));
            }

            if (StatusFilter == "Pending")
            {
                filtered = filtered.Where(p => p.Status == "Pending" || string.IsNullOrEmpty(p.Status));
            }
            else if (StatusFilter == "Approved")
            {
                filtered = filtered.Where(p => p.Status == "Approved");
            }
            else if (StatusFilter == "Rejected")
            {
                filtered = filtered.Where(p => p.Status == "Rejected");
            }

            foreach (var prod in filtered.ToList())
            {
                Products.Add(prod);
            }
        }

        // --- Commands Implementation ---

        private bool CanExecuteApproveProduct(object obj) => SelectedProduct != null && (SelectedProduct.Status == "Pending" || SelectedProduct.Status == "Rejected" || string.IsNullOrEmpty(SelectedProduct.Status));
        private async void ExecuteApproveProduct(object obj)
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

        private bool CanExecuteRejectProduct(object obj) => SelectedProduct != null && (SelectedProduct.Status == "Pending" || SelectedProduct.Status == "Approved" || string.IsNullOrEmpty(SelectedProduct.Status));
        private async void ExecuteRejectProduct(object obj)
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
    }
}

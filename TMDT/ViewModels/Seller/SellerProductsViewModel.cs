using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class SellerProductsViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        private ObservableCollection<Product> _products;
        private ObservableCollection<Category> _categories;
        private Product _selectedProduct;
        private string _searchText = "";
        private string _statusFilter = "All"; // All, Pending, Approved, Rejected

        // Inspector fields for add/edit
        private string _productNameInput;
        private string _productCodeInput;
        private decimal _priceInput;
        private decimal _originalPriceInput;
        private int _stockInput;
        private string _descriptionInput;
        private Category _selectedCategoryInput;
        private bool _isEditMode;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct == value) return;
                _selectedProduct = value;
                OnPropertyChanged();
                PopulateInspectorFields();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); LoadProducts(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); LoadProducts(); }
        }

        #region Inspector Properties
        public string ProductNameInput
        {
            get => _productNameInput;
            set { _productNameInput = value; OnPropertyChanged(); }
        }
        public string ProductCodeInput
        {
            get => _productCodeInput;
            set { _productCodeInput = value; OnPropertyChanged(); }
        }
        public decimal PriceInput
        {
            get => _priceInput;
            set { _priceInput = value; OnPropertyChanged(); }
        }
        public decimal OriginalPriceInput
        {
            get => _originalPriceInput;
            set { _originalPriceInput = value; OnPropertyChanged(); }
        }
        public int StockInput
        {
            get => _stockInput;
            set { _stockInput = value; OnPropertyChanged(); }
        }
        public string DescriptionInput
        {
            get => _descriptionInput;
            set { _descriptionInput = value; OnPropertyChanged(); }
        }
        public Category SelectedCategoryInput
        {
            get => _selectedCategoryInput;
            set { _selectedCategoryInput = value; OnPropertyChanged(); }
        }
        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(); }
        }
        #endregion

        // Commands
        public ICommand SaveProductCommand { get; }
        public ICommand ResetFieldsCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand SetFilterCommand { get; }

        public SellerProductsViewModel()
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
            Categories = new ObservableCollection<Category>();

            SaveProductCommand = new RelayCommand(ExecuteSaveProduct);
            ResetFieldsCommand = new RelayCommand(o => ResetInspector());
            DeleteProductCommand = new RelayCommand(ExecuteDeleteProduct, o => SelectedProduct != null);
            SetFilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");

            LoadCategories();
            LoadProducts();
            ResetInspector();
        }

        private void LoadCategories()
        {
            Categories.Clear();
            try
            {
                if (_context != null && _context.Categories.Any())
                {
                    foreach (var cat in _context.Categories.ToList())
                    {
                        Categories.Add(cat);
                    }
                }
            }
            catch {}

            if (!Categories.Any())
            {
                Categories.Add(new Category { CategoryId = 1, CategoryName = "Thiết bị Gia dụng" });
                Categories.Add(new Category { CategoryId = 2, CategoryName = "Thời trang Unisex" });
                Categories.Add(new Category { CategoryId = 3, CategoryName = "Thiết bị Âm thanh" });
                Categories.Add(new Category { CategoryId = 4, CategoryName = "Sức khỏe & Sắc đẹp" });
            }
        }

        private void LoadProducts()
        {
            Products.Clear();
            int currentShopId = GetCurrentShopId();

            try
            {
                if (_context != null && _context.Products.Any())
                {
                    var query = _context.Products
                        .Include(p => p.Category)
                        .Where(p => p.ShopId == currentShopId)
                        .AsQueryable();

                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        query = query.Where(p => p.ProductName.Contains(SearchText) ||
                                                 (p.ProductCode != null && p.ProductCode.Contains(SearchText)));
                    }

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

                    if (Products.Any()) return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load products from DB: " + ex.Message);
            }

            LoadMockProducts();
        }

        private void LoadMockProducts()
        {
            var mockProds = new ObservableCollection<Product>();

            mockProds.Add(new Product
            {
                ProductId = 7001,
                ProductCode = "TEE-ORGANIC",
                ProductName = "Áo Thun Unisex Cotton Organic Cao Cấp",
                CategoryId = 2,
                Price = 189000,
                OriginalPrice = 250000,
                StockQuantity = 215,
                SoldCount = 285,
                Rating = 4.8m,
                Status = "Approved",
                Description = "Áo thun 100% cotton hữu cơ cao cấp, thoáng mát mịn màng.",
                Category = Categories.FirstOrDefault(c => c.CategoryId == 2)
            });

            mockProds.Add(new Product
            {
                ProductId = 7002,
                ProductCode = "TEFAL-5.6L",
                ProductName = "Nồi Chiên Không Dầu Tefal XXL 5.6L",
                CategoryId = 1,
                Price = 2490000,
                OriginalPrice = 3500000,
                StockQuantity = 52,
                SoldCount = 98,
                Rating = 4.7m,
                Status = "Approved",
                Description = "Nồi chiên Tefal dung tích 5.6L lý tưởng cho gia đình.",
                Category = Categories.FirstOrDefault(c => c.CategoryId == 1)
            });

            mockProds.Add(new Product
            {
                ProductId = 7003,
                ProductCode = "ROBO-QREVO",
                ProductName = "Robot Hút Bụi Lau Nhà Roborock Q Revo",
                CategoryId = 1,
                Price = 14500000,
                OriginalPrice = 18000000,
                StockQuantity = 15,
                SoldCount = 30,
                Rating = 4.9m,
                Status = "Approved",
                Description = "Robot lau nhà đa năng, tự giặt giẻ và sấy khô giẻ tiện lợi.",
                Category = Categories.FirstOrDefault(c => c.CategoryId == 1)
            });

            mockProds.Add(new Product
            {
                ProductId = 7004,
                ProductCode = "SONY-WH1000",
                ProductName = "Tai nghe Chống Ồn Sony WH-1000XM5",
                CategoryId = 3,
                Price = 6490000,
                OriginalPrice = 8490000,
                StockQuantity = 8,
                SoldCount = 12,
                Rating = 4.8m,
                Status = "Approved",
                Description = "Tai nghe chụp tai Sony chống ồn thế hệ thứ 5.",
                Category = Categories.FirstOrDefault(c => c.CategoryId == 3)
            });

            mockProds.Add(new Product
            {
                ProductId = 7005,
                ProductCode = "IPHONE-15PRO",
                ProductName = "Điện thoại Apple iPhone 15 Pro Max 256GB",
                CategoryId = 3,
                Price = 29490000,
                OriginalPrice = 34990000,
                StockQuantity = 12,
                SoldCount = 4,
                Rating = 0,
                Status = "Pending",
                Description = "Siêu phẩm điện thoại cao cấp nhất từ nhà Táo với khung vỏ titanium.",
                Category = Categories.FirstOrDefault(c => c.CategoryId == 3)
            });

            var filtered = mockProds.AsQueryable();

            if (!string.IsNullOrEmpty(SearchText))
            {
                filtered = filtered.Where(p => p.ProductName.ToLower().Contains(SearchText.ToLower()) ||
                                               (p.ProductCode != null && p.ProductCode.ToLower().Contains(SearchText.ToLower())));
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

        private void PopulateInspectorFields()
        {
            if (SelectedProduct != null)
            {
                ProductNameInput = SelectedProduct.ProductName;
                ProductCodeInput = SelectedProduct.ProductCode;
                PriceInput = SelectedProduct.Price;
                OriginalPriceInput = SelectedProduct.OriginalPrice ?? SelectedProduct.Price;
                StockInput = SelectedProduct.StockQuantity ?? 0;
                DescriptionInput = SelectedProduct.Description;
                SelectedCategoryInput = Categories.FirstOrDefault(c => c.CategoryId == SelectedProduct.CategoryId) ?? Categories.FirstOrDefault();
                IsEditMode = true;
            }
            else
            {
                ResetInspector();
            }
        }

        private void ResetInspector()
        {
            _selectedProduct = null;
            OnPropertyChanged(nameof(SelectedProduct));
            ProductNameInput = "";
            ProductCodeInput = "";
            PriceInput = 0;
            OriginalPriceInput = 0;
            StockInput = 0;
            DescriptionInput = "";
            SelectedCategoryInput = Categories.FirstOrDefault();
            IsEditMode = false;
        }

        private async void ExecuteSaveProduct(object obj)
        {
            if (string.IsNullOrWhiteSpace(ProductNameInput))
            {
                MessageBox.Show("Vui lòng điền tên sản phẩm!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PriceInput <= 0)
            {
                MessageBox.Show("Giá bán phải lớn hơn 0!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int currentShopId = GetCurrentShopId();

            if (IsEditMode && SelectedProduct != null)
            {
                // Update existing
                SelectedProduct.ProductName = ProductNameInput;
                SelectedProduct.ProductCode = ProductCodeInput;
                SelectedProduct.Price = PriceInput;
                SelectedProduct.OriginalPrice = OriginalPriceInput;
                SelectedProduct.StockQuantity = StockInput;
                SelectedProduct.Description = DescriptionInput;
                SelectedProduct.CategoryId = SelectedCategoryInput?.CategoryId ?? 1;
                SelectedProduct.Category = SelectedCategoryInput;

                try
                {
                    if (_context != null)
                    {
                        var dbProd = await _context.Products.FindAsync(SelectedProduct.ProductId);
                        if (dbProd != null)
                        {
                            dbProd.ProductName = ProductNameInput;
                            dbProd.ProductCode = ProductCodeInput;
                            dbProd.Price = PriceInput;
                            dbProd.OriginalPrice = OriginalPriceInput;
                            dbProd.StockQuantity = StockInput;
                            dbProd.Description = DescriptionInput;
                            dbProd.CategoryId = SelectedCategoryInput?.CategoryId ?? 1;

                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("EF update product failed: " + ex.Message);
                }

                MessageBox.Show("Đã cập nhật sản phẩm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Add new (Pending status)
                var newProd = new Product
                {
                    ShopId = currentShopId,
                    ProductName = ProductNameInput,
                    ProductCode = string.IsNullOrWhiteSpace(ProductCodeInput) ? "PROD-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper() : ProductCodeInput,
                    Price = PriceInput,
                    OriginalPrice = OriginalPriceInput > 0 ? OriginalPriceInput : PriceInput,
                    StockQuantity = StockInput,
                    Description = DescriptionInput,
                    CategoryId = SelectedCategoryInput?.CategoryId ?? 1,
                    Status = "Pending", // Seller products are Pending until Admin approves
                    CreatedAt = DateTime.Now,
                    SoldCount = 0,
                    Rating = 0,
                    Category = SelectedCategoryInput
                };

                try
                {
                    if (_context != null)
                    {
                        _context.Products.Add(newProd);
                        await _context.SaveChangesAsync();
                        newProd.ProductId = newProd.ProductId; // DB assigned ID
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("EF insert product failed: " + ex.Message);
                    // generate mock ID
                    newProd.ProductId = new Random().Next(8000, 9999);
                }

                MessageBox.Show("Đã thêm mới sản phẩm! Vui lòng chờ Admin phê duyệt để sản phẩm hiển thị trên sàn.", "Đăng bán thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadProducts();
            ResetInspector();
        }

        private async void ExecuteDeleteProduct(object obj)
        {
            if (SelectedProduct == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa sản phẩm '{SelectedProduct.ProductName}'?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (_context != null)
                {
                    var dbProd = await _context.Products.FindAsync(SelectedProduct.ProductId);
                    if (dbProd != null)
                    {
                        _context.Products.Remove(dbProd);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF delete product failed: " + ex.Message);
            }

            Products.Remove(SelectedProduct);
            ResetInspector();
            MessageBox.Show("Đã xóa sản phẩm thành công!", "Xóa thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private int GetCurrentShopId()
        {
            try
            {
                if (_context != null)
                {
                    var shop = _context.Shops
                        .Include(s => s.User)
                        .FirstOrDefault(s => s.User != null && s.User.Email == "seller@myshop.com")
                        ?? _context.Shops.FirstOrDefault();
                    if (shop != null) return shop.ShopId;
                }
            }
            catch {}
            return 1; // Default mock shop ID
        }
    }
}

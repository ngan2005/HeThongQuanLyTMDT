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
        private string _statusFilter = "All";

        // Inspector fields
        private string _productNameInput;
        private string _productCodeInput;
        private decimal _priceInput;
        private decimal? _originalPriceInput;
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
        public decimal? OriginalPriceInput
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
            set { _isEditMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormTitle)); OnPropertyChanged(nameof(FormStatusBadge)); }
        }

        // Dynamic form title
        public string FormTitle => IsEditMode ? "Chi tiết sản phẩm" : "Thêm sản phẩm mới";

        // Dynamic status badge text
        public string FormStatusBadge
        {
            get
            {
                if (!IsEditMode) return "Chế độ thêm mới";
                return SelectedProduct?.Status switch
                {
                    "Pending" => "⏳ Chờ Admin duyệt",
                    "Approved" => "✅ Đang bán",
                    "Rejected" => "❌ Bị từ chối",
                    "Deleted" => "🗑 Đã xóa",
                    _ => "—"
                };
            }
        }
        #endregion

        // Commands
        public ICommand SaveProductCommand { get; }
        public ICommand ResetFieldsCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand SetFilterCommand { get; }

        public SellerProductsViewModel()
        {
            try { _context = new TmdtContext(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Init TmdtContext failed: " + ex.Message); }

            Products = new ObservableCollection<Product>();
            Categories = new ObservableCollection<Category>();

            SaveProductCommand = new RelayCommand(_ => ExecuteSaveProduct());
            ResetFieldsCommand = new RelayCommand(_ => ResetInspector());
            DeleteProductCommand = new RelayCommand(_ => ExecuteDeleteProduct(), _ => SelectedProduct != null && SelectedProduct.Status != "Deleted");
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
                        Categories.Add(cat);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadCategories failed: " + ex.Message); }

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
            if (currentShopId <= 0) return;

            try
            {
                if (_context == null) return;

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
                    query = query.Where(p => p.Status == "Pending" || string.IsNullOrEmpty(p.Status));
                else if (StatusFilter == "Approved")
                    query = query.Where(p => p.Status == "Approved");
                else if (StatusFilter == "Rejected")
                    query = query.Where(p => p.Status == "Rejected");
                else if (StatusFilter == "Deleted")
                    query = query.Where(p => p.Status == "Deleted");
                else
                    query = query.Where(p => p.Status != "Deleted");

                foreach (var prod in query.ToList())
                    Products.Add(prod);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadProducts failed: " + ex.Message);
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
                OnPropertyChanged(nameof(FormStatusBadge));
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
            OriginalPriceInput = null;
            StockInput = 0;
            DescriptionInput = "";
            SelectedCategoryInput = Categories.FirstOrDefault();
            IsEditMode = false;
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(FormStatusBadge));
        }

        private async void ExecuteSaveProduct()
        {
            if (string.IsNullOrWhiteSpace(ProductNameInput))
            {
                MessageBox.Show("Vui lòng nhập tên sản phẩm!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (PriceInput <= 0)
            {
                MessageBox.Show("Giá bán phải lớn hơn 0!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedCategoryInput == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục sản phẩm!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int currentShopId = GetCurrentShopId();
            if (currentShopId <= 0)
            {
                MessageBox.Show("Không tìm thấy cửa hàng. Vui lòng đăng nhập lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsEditMode && SelectedProduct != null)
            {
                // Kiểm tra sản phẩm thuộc shop hiện tại
                if (SelectedProduct.ShopId != currentShopId)
                {
                    MessageBox.Show("Bạn không có quyền sửa sản phẩm này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // SelectedProduct đã được EF track, FindAsync trả về cùng object
                    SelectedProduct.ProductName = ProductNameInput;
                    SelectedProduct.ProductCode = ProductCodeInput;
                    SelectedProduct.Price = PriceInput;
                    SelectedProduct.OriginalPrice = OriginalPriceInput;
                    SelectedProduct.StockQuantity = StockInput;
                    SelectedProduct.Description = DescriptionInput;
                    SelectedProduct.CategoryId = SelectedCategoryInput.CategoryId;
                    SelectedProduct.Category = SelectedCategoryInput;

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Update product failed: " + ex.Message);
                    MessageBox.Show("Lỗi khi cập nhật sản phẩm: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                AuditLogHelper.Log("UPDATE_PRODUCT", $"Sửa '{ProductNameInput}' (ID:{SelectedProduct.ProductId})", "Product", "Normal");
                MessageBox.Show("Đã cập nhật sản phẩm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var newProd = new Product
                {
                    ShopId = currentShopId,
                    ProductName = ProductNameInput,
                    ProductCode = string.IsNullOrWhiteSpace(ProductCodeInput)
                        ? "PROD-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()
                        : ProductCodeInput,
                    Price = PriceInput,
                    OriginalPrice = OriginalPriceInput,
                    StockQuantity = StockInput,
                    Description = DescriptionInput,
                    CategoryId = SelectedCategoryInput.CategoryId,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    SoldCount = 0,
                    Rating = 0,
                    Category = SelectedCategoryInput
                };

                try
                {
                    _context.Products.Add(newProd);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Insert product failed: " + ex.Message);
                    MessageBox.Show("Lỗi khi thêm sản phẩm: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                AuditLogHelper.Log("ADD_PRODUCT", $"Thêm '{ProductNameInput}' (Code:{newProd.ProductCode})", "Product", "Normal");
                MessageBox.Show("Đã thêm sản phẩm! Vui lòng chờ Admin phê duyệt để hiển thị trên sàn.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadProducts();
            ResetInspector();
        }

        private async void ExecuteDeleteProduct()
        {
            if (SelectedProduct == null) return;

            // Kiểm tra sản phẩm thuộc shop hiện tại
            int currentShopId = GetCurrentShopId();
            if (SelectedProduct.ShopId != currentShopId)
            {
                MessageBox.Show("Bạn không có quyền xóa sản phẩm này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Xóa sản phẩm '{SelectedProduct.ProductName}'?\nSản phẩm sẽ được chuyển vào thùng rác.",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var dbProd = await _context.Products.FindAsync(SelectedProduct.ProductId);
                if (dbProd != null)
                {
                    dbProd.Status = "Deleted";           // Soft delete
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Soft delete failed: " + ex.Message);
            }

            AuditLogHelper.Log("DELETE_PRODUCT", $"Xóa '{SelectedProduct.ProductName}' (ID:{SelectedProduct.ProductId})", "Product", "Warning");
            Products.Remove(SelectedProduct);
            ResetInspector();
            MessageBox.Show("Đã xóa sản phẩm!", "Xóa thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private int GetCurrentShopId()
        {
            try
            {
                if (_context == null) return 0;
                if (SessionManager.CurrentUser == null) return 0;

                var shop = _context.Shops
                    .FirstOrDefault(s => s.UserId == SessionManager.CurrentUser.UserId);
                return shop?.ShopId ?? 0;
            }
            catch { return 0; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}

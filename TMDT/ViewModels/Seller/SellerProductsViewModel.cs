using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using TMDT.Models;
using TMDT.Utilities;
using TMDT.Services;
using TMDT.Services.Interfaces;

namespace TMDT.ViewModels.Seller
{
    public class SellerProductsViewModel : ViewModelBase
    {
        // Removed long-lived _context for async safety
        private readonly IImageUploadService _imageUploadService;
        private ObservableCollection<Product> _products = new();
        private ObservableCollection<Category> _categories = new();
        private Product? _selectedProduct;
        private string _searchText = "";
        private string _statusFilter = "All";

        // Inspector fields
        private string _productNameInput = "";
        private string _productCodeInput = "";
        private decimal _priceInput;
        private decimal? _originalPriceInput;
        private int _stockInput;
        private string _descriptionInput = "";
        private Category? _selectedCategoryInput;
        private bool _isEditMode;

        private ObservableCollection<ProductImage> _productImagesPreview = new();
        public ObservableCollection<ProductImage> ProductImagesPreview
        {
            get => _productImagesPreview;
            set { _productImagesPreview = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ProductVariant> _productVariantsPreview = new();
        public ObservableCollection<ProductVariant> ProductVariantsPreview
        {
            get => _productVariantsPreview;
            set { _productVariantsPreview = value; OnPropertyChanged(); }
        }

        // Variant Input Fields
        private string _variantNameInput = "";
        private decimal _variantExtraPriceInput;
        private int _variantQuantityInput;
        private string _variantSkuInput = "";

        public string VariantNameInput
        {
            get => _variantNameInput;
            set { _variantNameInput = value; OnPropertyChanged(); }
        }
        public decimal VariantExtraPriceInput
        {
            get => _variantExtraPriceInput;
            set { _variantExtraPriceInput = value; OnPropertyChanged(); }
        }
        public int VariantQuantityInput
        {
            get => _variantQuantityInput;
            set { _variantQuantityInput = value; OnPropertyChanged(); }
        }
        public string VariantSkuInput
        {
            get => _variantSkuInput;
            set { _variantSkuInput = value; OnPropertyChanged(); }
        }

        private List<string> _selectedLocalImagePaths = new();
        public List<string> SelectedLocalImagePaths
        {
            get => _selectedLocalImagePaths;
            set { _selectedLocalImagePaths = value; OnPropertyChanged(); }
        }

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

        public Product? SelectedProduct
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
            set { _searchText = value; OnPropertyChanged(); _ = LoadProductsAsync(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); _ = LoadProductsAsync(); }
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
        
        // Logistics Inputs
        private int? _weightInput;
        public int? WeightInput
        {
            get => _weightInput;
            set { _weightInput = value; OnPropertyChanged(); }
        }
        private int? _lengthInput;
        public int? LengthInput
        {
            get => _lengthInput;
            set { _lengthInput = value; OnPropertyChanged(); }
        }
        private int? _widthInput;
        public int? WidthInput
        {
            get => _widthInput;
            set { _widthInput = value; OnPropertyChanged(); }
        }
        private int? _heightInput;
        public int? HeightInput
        {
            get => _heightInput;
            set { _heightInput = value; OnPropertyChanged(); }
        }

        public string DescriptionInput
        {
            get => _descriptionInput;
            set { _descriptionInput = value; OnPropertyChanged(); }
        }
        public Category? SelectedCategoryInput
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
        public ICommand SaveProductCommand { get; } = null!;
        public ICommand ResetFieldsCommand { get; } = null!;
        public ICommand DeleteProductCommand { get; } = null!;
        public ICommand SetFilterCommand { get; } = null!;
        public ICommand SelectImageCommand { get; } = null!;
        public ICommand ClearImagesCommand { get; } = null!;
        public ICommand AddVariantCommand { get; } = null!;
        public ICommand RemoveVariantCommand { get; } = null!;
        public ICommand ShowBarcodeCommand { get; } = null!;

        public SellerProductsViewModel()
        {
            _imageUploadService = new CloudinaryService();

            Products = new ObservableCollection<Product>();
            Categories = new ObservableCollection<Category>();

            SaveProductCommand = new RelayCommand(_ => ExecuteSaveProduct());
            ResetFieldsCommand = new RelayCommand(_ => ResetInspector());
            DeleteProductCommand = new RelayCommand(_ => ExecuteDeleteProduct(), _ => SelectedProduct != null && SelectedProduct.Status != "Deleted");
            SetFilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");
            SelectImageCommand = new RelayCommand(_ => ExecuteSelectImages());
            ClearImagesCommand = new RelayCommand(_ => ExecuteClearImages());
            AddVariantCommand = new RelayCommand(_ => ExecuteAddVariant());
            RemoveVariantCommand = new RelayCommand(o => ExecuteRemoveVariant(o as ProductVariant));
            ShowBarcodeCommand = new RelayCommand(o => ExecuteShowBarcode(o as Product));

            _ = LoadCategoriesAsync();
            _ = LoadProductsAsync();
            ResetInspector();
        }

        private void ExecuteShowBarcode(Product? product)
        {
            if (product == null) return;
            var dlg = new TMDT.Views.Seller.BarcodeDialog
            {
                DataContext = new BarcodeViewModel(product),
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();
        }

        private void ExecuteSelectImages()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn hình ảnh sản phẩm",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    SelectedLocalImagePaths.Add(file);
                    ProductImagesPreview.Add(new ProductImage
                    {
                        ImageUrl = file,
                        IsMain = ProductImagesPreview.Count == 0 // Ảnh đầu tiên là ảnh bìa
                    });
                }
            }
        }

        private void ExecuteClearImages()
        {
            SelectedLocalImagePaths.Clear();
            ProductImagesPreview.Clear();
            
            // Nếu đang sửa sản phẩm, chúng ta sẽ xóa các ảnh trên Database khi bấm Lưu
            // Hoặc có thể tự xóa ngay lập tức nhưng an toàn hơn là để lúc Lưu
        }

        private void ExecuteAddVariant()
        {
            if (string.IsNullOrWhiteSpace(VariantNameInput))
            {
                MessageBox.Show("Vui lòng nhập tên phân loại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProductVariantsPreview.Add(new ProductVariant
            {
                VariantName = VariantNameInput.Trim(),
                ExtraPrice = VariantExtraPriceInput,
                Quantity = VariantQuantityInput,
                Sku = string.IsNullOrWhiteSpace(VariantSkuInput) ? $"VAR-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}" : VariantSkuInput.Trim()
            });

            // Reset variant input fields
            VariantNameInput = "";
            VariantExtraPriceInput = 0;
            VariantQuantityInput = 0;
            VariantSkuInput = "";
        }

        private void ExecuteRemoveVariant(ProductVariant? variant)
        {
            if (variant != null && ProductVariantsPreview.Contains(variant))
            {
                ProductVariantsPreview.Remove(variant);
            }
        }

        private async Task LoadCategoriesAsync()
        {
            Categories.Clear();
            try
            {
                using var ctx = new TmdtContext();
                if (await ctx.Categories.AnyAsync())
                {
                    var cats = await ctx.Categories.ToListAsync();
                    foreach (var cat in cats)
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

        private async Task LoadProductsAsync()
        {
            Products.Clear();
            int currentShopId = await GetCurrentShopIdAsync();
            if (currentShopId <= 0) return;

            try
            {
                using var ctx = new TmdtContext();

                var query = ctx.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .Where(p => p.ShopId == currentShopId)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(SearchText))
                {
                    string keyword = SearchText.Trim();
                    query = query.Where(p => EF.Functions.Like(p.ProductName, $"%{keyword}%") ||
                                            (p.ProductCode != null && EF.Functions.Like(p.ProductCode, $"%{keyword}%")));
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

                var prods = await query.ToListAsync();
                foreach (var prod in prods)
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
                OriginalPriceInput = SelectedProduct.OriginalPrice;
                StockInput = SelectedProduct.StockQuantity ?? 0;
                DescriptionInput = SelectedProduct.Description ?? "";
                SelectedCategoryInput = Categories.FirstOrDefault(c => c.CategoryId == SelectedProduct.CategoryId);
                
                ProductImagesPreview.Clear();
                SelectedLocalImagePaths.Clear();
                if (SelectedProduct.ProductImages != null)
                {
                    foreach (var img in SelectedProduct.ProductImages.OrderBy(i => i.SortOrder))
                    {
                        ProductImagesPreview.Add(new ProductImage
                        {
                            ImageId = img.ImageId,
                            ProductId = img.ProductId,
                            ImageUrl = img.ImageUrl,
                            IsMain = img.IsMain,
                            SortOrder = img.SortOrder
                        });
                    }
                }

                ProductVariantsPreview.Clear();
                if (SelectedProduct.ProductVariants != null)
                {
                    foreach (var variant in SelectedProduct.ProductVariants)
                    {
                        ProductVariantsPreview.Add(new ProductVariant
                        {
                            VariantId = variant.VariantId,
                            ProductId = variant.ProductId,
                            VariantName = variant.VariantName,
                            ExtraPrice = variant.ExtraPrice,
                            Quantity = variant.Quantity,
                            Sku = variant.Sku
                        });
                    }
                }

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
            ProductCodeInput = "PROD-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            PriceInput = 0;
            OriginalPriceInput = null;
            StockInput = 0;
            DescriptionInput = "";
            SelectedCategoryInput = Categories.FirstOrDefault();
            ProductImagesPreview.Clear();
            SelectedLocalImagePaths.Clear();
            ProductVariantsPreview.Clear();
            VariantNameInput = "";
            VariantExtraPriceInput = 0;
            VariantQuantityInput = 0;
            VariantSkuInput = "";
            WeightInput = null;
            LengthInput = null;
            WidthInput = null;
            HeightInput = null;
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

            int currentShopId = await GetCurrentShopIdAsync();
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
            }

            try
            {
                using var ctx = new TmdtContext();
                Product targetProd;

                if (IsEditMode && SelectedProduct != null)
                {
                    targetProd = await ctx.Products.FindAsync(SelectedProduct.ProductId) ?? SelectedProduct;
                    targetProd.ProductName = ProductNameInput;
                    targetProd.ProductCode = ProductCodeInput;
                    targetProd.Price = PriceInput;
                    targetProd.OriginalPrice = OriginalPriceInput;
                    targetProd.StockQuantity = StockInput;
                    targetProd.Description = DescriptionInput;
                    targetProd.CategoryId = SelectedCategoryInput?.CategoryId;
                }
                else
                {
                    targetProd = new Product
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
                        CategoryId = SelectedCategoryInput?.CategoryId,
                        Status = SystemSettingsHelper.Current.RequireProductApproval ? "Pending" : "Approved",
                        CreatedAt = DateTime.Now,
                        SoldCount = 0,
                        Rating = 0
                    };
                    ctx.Products.Add(targetProd);
                }

                // Lưu Product trước để có ProductId
                await ctx.SaveChangesAsync();

                // Cập nhật ProductImages
                var oldImages = await ctx.ProductImages.Where(i => i.ProductId == targetProd.ProductId).ToListAsync();
                ctx.ProductImages.RemoveRange(oldImages);
                
                var finalImages = new List<ProductImage>();
                int sortOrder = 0;

                foreach (var img in ProductImagesPreview)
                {
                    if (string.IsNullOrEmpty(img.ImageUrl)) continue;

                    if (!SelectedLocalImagePaths.Contains(img.ImageUrl))
                    {
                        // Ảnh cũ (đã có URL từ Cloudinary)
                        finalImages.Add(new ProductImage
                        {
                            ProductId = targetProd.ProductId,
                            ImageUrl = img.ImageUrl,
                            IsMain = sortOrder == 0,
                            SortOrder = sortOrder++
                        });
                    }
                    else
                    {
                        // Upload ảnh mới
                        string uploadedUrl = await _imageUploadService.UploadImageAsync(img.ImageUrl);
                        if (!string.IsNullOrEmpty(uploadedUrl))
                        {
                            finalImages.Add(new ProductImage
                            {
                                ProductId = targetProd.ProductId,
                                ImageUrl = uploadedUrl,
                                IsMain = sortOrder == 0,
                                SortOrder = sortOrder++
                            });
                        }
                    }
                }

                if (finalImages.Any())
                {
                    ctx.ProductImages.AddRange(finalImages);
                    await ctx.SaveChangesAsync();
                }

                // Cập nhật ProductVariants
                var oldVariants = await ctx.ProductVariants.Where(v => v.ProductId == targetProd.ProductId).ToListAsync();
                ctx.ProductVariants.RemoveRange(oldVariants);

                var finalVariants = new List<ProductVariant>();
                foreach (var variant in ProductVariantsPreview)
                {
                    finalVariants.Add(new ProductVariant
                    {
                        ProductId = targetProd.ProductId,
                        VariantName = variant.VariantName,
                        ExtraPrice = variant.ExtraPrice,
                        Quantity = variant.Quantity,
                        Sku = variant.Sku
                    });
                }

                if (finalVariants.Any())
                {
                    ctx.ProductVariants.AddRange(finalVariants);
                    await ctx.SaveChangesAsync();
                }

                if (IsEditMode)
                {
                    AuditLogHelper.Log("UPDATE_PRODUCT", $"Sửa '{ProductNameInput}' (ID:{targetProd.ProductId})", "Product", "Normal");
                    MessageBox.Show("Đã cập nhật sản phẩm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AuditLogHelper.Log("ADD_PRODUCT", $"Thêm '{ProductNameInput}' (Code:{targetProd.ProductCode})", "Product", "Normal");
                    MessageBox.Show("Đã thêm sản phẩm! Vui lòng chờ Admin phê duyệt.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                _ = LoadProductsAsync();
                ResetInspector();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Save product failed: " + ex.Message);
                MessageBox.Show("Lỗi khi lưu sản phẩm: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteDeleteProduct()
        {
            if (SelectedProduct == null) return;

            // Kiểm tra sản phẩm thuộc shop hiện tại
            int currentShopId = await GetCurrentShopIdAsync();
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
                using var ctx = new TmdtContext();
                var dbProd = await ctx.Products.FindAsync(SelectedProduct.ProductId);
                if (dbProd != null)
                {
                    dbProd.Status = "Deleted";           // Soft delete
                    await ctx.SaveChangesAsync();
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

        private async Task<int> GetCurrentShopIdAsync()
        {
            try
            {
                if (SessionManager.CurrentUser == null) return 0;

                using var ctx = new TmdtContext();
                var shop = await ctx.Shops
                    .FirstOrDefaultAsync(s => s.UserId == SessionManager.CurrentUser.UserId);
                return shop?.ShopId ?? 0;
            }
            catch { return 0; }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

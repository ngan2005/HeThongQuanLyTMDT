using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerProductsViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        
        private string _searchQuery = "";
        private Category? _selectedCategory;
        private decimal? _minPrice;
        private decimal? _maxPrice;

        public ObservableCollection<ProductWrapper> Products { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        public string SearchQuery
        {
            get => _searchQuery;
            set { SetProperty(ref _searchQuery, value); }
        }

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set { SetProperty(ref _selectedCategory, value); }
        }

        public decimal? MinPrice
        {
            get => _minPrice;
            set { SetProperty(ref _minPrice, value); }
        }

        public decimal? MaxPrice
        {
            get => _maxPrice;
            set { SetProperty(ref _maxPrice, value); }
        }

        public ICommand FilterCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand OpenProductCommand { get; }
        public ICommand ToggleWishlistCommand { get; }

        public BuyerProductsViewModel(BuyerMainViewModel mainVm, string initialSearchQuery = "")
        {
            _mainVm = mainVm;
            _searchQuery = initialSearchQuery;

            FilterCommand = new RelayCommand(_ => LoadProducts());
            ClearFilterCommand = new RelayCommand(_ => ClearFilters());
            OpenProductCommand = new RelayCommand(p => _mainVm.NavigateProductDetail(p is ProductWrapper w ? w.Product : p as Product));
            ToggleWishlistCommand = new RelayCommand(p => ExecuteToggleWishlist(p as ProductWrapper));

            LoadCategories();
            LoadProducts();
        }

        private async void LoadCategories()
        {
            try
            {
                using var context = new TmdtContext();
                var cats = await context.Categories.AsNoTracking().Where(c => c.IsActive == true).OrderBy(c => c.SortOrder).ToListAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    Categories.Add(new Category { CategoryId = 0, CategoryName = "Tất cả danh mục" });
                    foreach (var c in cats) Categories.Add(c);
                    
                    SelectedCategory = Categories.First();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load categories failed: " + ex.Message);
            }
        }

        public async void LoadProducts()
        {
            try
            {
                using var context = new TmdtContext();
                var query = context.Products.AsNoTracking()
                    .Include(p => p.Shop)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == "Approved" && (p.Shop == null || p.Shop.IsActive == true))
                    .AsQueryable();

                // 1. Search Query filter
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    var t = SearchQuery.Trim();
                    query = query.Where(p => 
                        (p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{t}%")) ||
                        (p.Description != null && EF.Functions.Like(p.Description, $"%{t}%")));
                }

                // 2. Category filter
                if (SelectedCategory != null && SelectedCategory.CategoryId > 0)
                {
                    query = query.Where(p => p.CategoryId == SelectedCategory.CategoryId);
                }

                // 3. Price filter
                if (MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= MinPrice.Value);
                }
                if (MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= MaxPrice.Value);
                }

                // Sorting
                query = query.OrderByDescending(p => p.SoldCount).ThenByDescending(p => p.CreatedAt);

                var items = await query.ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Products.Clear();
                    foreach (var p in items)
                    {
                        bool inWishlist = SessionManager.IsLoggedIn
                            && context.Wishlists.Any(w => w.UserId == SessionManager.CurrentUser!.UserId && w.ProductId == p.ProductId);
                        Products.Add(new ProductWrapper(p, inWishlist));
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load products failed: " + ex.Message);
            }
        }

        private void ClearFilters()
        {
            SearchQuery = "";
            MinPrice = null;
            MaxPrice = null;
            if (Categories.Count > 0)
            {
                SelectedCategory = Categories.First();
            }
            LoadProducts();
        }

        private void ExecuteToggleWishlist(ProductWrapper? wrapper)
        {
            if (wrapper == null) return;

            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập để thêm sản phẩm yêu thích.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var ctx = new TmdtContext();
                var existing = ctx.Wishlists
                    .FirstOrDefault(w => w.UserId == SessionManager.CurrentUser!.UserId
                                      && w.ProductId == wrapper.Product.ProductId);

                if (existing != null)
                {
                    ctx.Wishlists.Remove(existing);
                    ctx.SaveChanges();
                    wrapper.IsWishlisted = false;
                }
                else
                {
                    ctx.Wishlists.Add(new Wishlist
                    {
                        UserId = SessionManager.CurrentUser!.UserId,
                        ProductId = wrapper.Product.ProductId,
                        AddedAt = DateTime.Now
                    });
                    ctx.SaveChanges();
                    wrapper.IsWishlisted = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Toggle wishlist failed: " + ex.Message);
            }
        }
    }
}

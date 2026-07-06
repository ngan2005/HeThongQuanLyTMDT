using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;
using Microsoft.EntityFrameworkCore;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerMainViewModel : ViewModelBase, IDisposable
    {
        private ViewModelBase _currentViewModel;
        private Product? _selectedProduct;
        private int _cartBadgeCount;
        private string _pageTitle = "Trang chủ";
        private string _searchQuery = "";
        private string _currentPage = "Home";

        public BuyerChatViewModel ChatViewModel { get; } = new BuyerChatViewModel();

        public ObservableCollection<object> Notifications { get; } = new();

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set { SetProperty(ref _currentViewModel, value); }
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set { SetProperty(ref _selectedProduct, value); }
        }

        public int CartBadgeCount
        {
            get => _cartBadgeCount;
            set { SetProperty(ref _cartBadgeCount, value); }
        }

        public string PageTitle
        {
            get => _pageTitle;
            set { SetProperty(ref _pageTitle, value); }
        }

        public bool IsLoggedIn => SessionManager.IsLoggedIn;
        public bool IsSeller => SessionManager.IsSeller;
        public bool IsBuyer => SessionManager.IsBuyer;
        public string UserName => SessionManager.CurrentUser?.FullName ?? "Khách";

        public string SearchQuery
        {
            get => _searchQuery;
            set { SetProperty(ref _searchQuery, value); }
        }

        public string CurrentPage
        {
            get => _currentPage;
            set { SetProperty(ref _currentPage, value); }
        }

        public ICommand GoHomeCommand { get; }
        public ICommand GoProductsCommand { get; }
        public ICommand GoPromotionsCommand { get; }
        public ICommand GoGuideCommand { get; }
        public ICommand GoContactCommand { get; }
        public ICommand GoCartCommand { get; }
        public ICommand GoOrdersCommand { get; }
        public ICommand GoProfileCommand { get; }
        public ICommand GoWishlistCommand { get; }
        public ICommand OpenProductCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenSellerPortalCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand BecomeSellerCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ToggleWishlistCommand { get; }

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<ProductWrapper> FeaturedProducts { get; } = new();
        public ObservableCollection<Banner> Banners { get; } = new();

        private Banner? _currentBanner;
        private int _currentBannerIndex = 0;
        public Banner? CurrentBanner
        {
            get => _currentBanner;
            set => SetProperty(ref _currentBanner, value);
        }

        public BuyerMainViewModel()
        {
            _currentViewModel = new BuyerHomeViewModel(this);

            CartService.Instance.CartChanged += UpdateCartBadge;

            GoHomeCommand = new RelayCommand(_ => NavigateHome());
            GoProductsCommand = new RelayCommand(_ => NavigateProducts());
            GoPromotionsCommand = new RelayCommand(_ => NavigatePromotions());
            GoGuideCommand = new RelayCommand(_ => NavigateGuide());
            GoContactCommand = new RelayCommand(_ => NavigateContact());
            GoCartCommand = new RelayCommand(_ => NavigateCart());
            GoOrdersCommand = new RelayCommand(_ => NavigateOrders());
            GoProfileCommand = new RelayCommand(_ => NavigateProfile());
            GoWishlistCommand = new RelayCommand(_ => NavigateWishlist());
            OpenProductCommand = new RelayCommand(p => NavigateProductDetail(p is ProductWrapper w ? w.Product : p as Product));
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            OpenSellerPortalCommand = new RelayCommand(_ => ExecuteOpenSellerPortal(), _ => IsLoggedIn && IsSeller);
            LoginCommand = new RelayCommand(_ => ExecuteLogin());
            BecomeSellerCommand = new RelayCommand(_ => ExecuteBecomeSeller(), _ => IsLoggedIn && IsBuyer);
            SearchCommand = new RelayCommand(_ => SearchProducts(SearchQuery));
            ToggleWishlistCommand = new RelayCommand(p => ExecuteToggleWishlist(p as ProductWrapper));

            _ = LoadCategoriesAsync();
            _ = LoadFeaturedProductsAsync();
            _ = LoadBannersAsync();
            UpdateCartBadge();
        }

        public void NavigateHome()
        {
            PageTitle = "Trang chủ";
            CurrentPage = "Home";
            CurrentViewModel = new BuyerHomeViewModel(this);
        }

        public void NavigateCart()
        {
            PageTitle = "Giỏ hàng";
            CurrentPage = "Cart";
            CurrentViewModel = new CartViewModel(this);
        }

        public void NavigateOrders()
        {
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập để xem đơn hàng.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            PageTitle = "Đơn hàng của tôi";
            CurrentPage = "Orders";
            CurrentViewModel = new BuyerOrdersViewModel(this);
        }

        public void NavigateProfile()
        {
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            PageTitle = "Tài khoản của tôi";
            CurrentPage = "Profile";
            CurrentViewModel = new BuyerProfileViewModel(this);
        }

        public void NavigateWishlist()
        {
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            PageTitle = "Yêu thích";
            CurrentPage = "Wishlist";
            CurrentViewModel = new WishlistViewModel(this);
        }

        public void NavigateProducts(string initialSearchQuery = "", int? shopId = null)
        {
            PageTitle = "Sản phẩm";
            CurrentPage = "Products";
            CurrentViewModel = new BuyerProductsViewModel(this, initialSearchQuery, shopId);
        }

        public void NavigateShop(int shopId)
        {
            PageTitle = "Cửa hàng";
            CurrentPage = "Shop";
            CurrentViewModel = new BuyerShopViewModel(shopId, this);
        }

        public void NavigatePromotions()
        {
            PageTitle = "Khuyến mãi";
            CurrentPage = "Promotions";
            CurrentViewModel = new BuyerPromotionsViewModel(this);
        }

        public void NavigateGuide()
        {
            PageTitle = "Hướng dẫn";
            CurrentPage = "Guide";
            CurrentViewModel = new BuyerGuideViewModel(this);
        }

        public void NavigateContact()
        {
            PageTitle = "Liên hệ";
            CurrentPage = "Contact";
            CurrentViewModel = new BuyerContactViewModel(this);
        }

        public void NavigateProductDetail(Product? product)
        {
            if (product == null) return;
            SelectedProduct = product;
            PageTitle = "Chi tiết sản phẩm";
            CurrentPage = "Product";
            CurrentViewModel = new ProductDetailViewModel(product, this);
        }

        public void SearchProducts(string term)
        {
            NavigateProducts(term);
        }

        public async Task SearchProductsAsync(string term)
        {
            try
            {
                using var context = new TmdtContext();
                var query = context.Products.AsNoTracking()
                    .Include(p => p.Shop)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == "Approved" && (p.Shop == null || p.Shop.IsActive == true))
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    var t = term.Trim();
                    query = query.Where(p =>
                        (p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{t}%")) ||
                        (p.Description != null && EF.Functions.Like(p.Description, $"%{t}%")));
                }

                var items = await query.OrderByDescending(p => p.SoldCount).Take(20).ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FeaturedProducts.Clear();
                    foreach (var p in items)
                    {
                        bool inWishlist = SessionManager.IsLoggedIn
                            && context.Wishlists.Any(w => w.UserId == SessionManager.CurrentUser!.UserId && w.ProductId == p.ProductId);
                        FeaturedProducts.Add(new ProductWrapper(p, inWishlist));
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Search failed: " + ex.Message);
            }
        }

        public void NextBanner()
        {
            if (Banners.Count <= 1) return;
            _currentBannerIndex = (_currentBannerIndex + 1) % Banners.Count;
            CurrentBanner = Banners[_currentBannerIndex];
        }

        public void PrevBanner()
        {
            if (Banners.Count <= 1) return;
            _currentBannerIndex = (_currentBannerIndex - 1 + Banners.Count) % Banners.Count;
            CurrentBanner = Banners[_currentBannerIndex];
        }

        public void SearchByCategory(Category cat)
        {
            if (cat != null) _ = SearchByCategoryAsync(cat);
        }

        public async Task SearchByCategoryAsync(Category cat)
        {
            try
            {
                using var context = new TmdtContext();
                var items = await context.Products.AsNoTracking()
                    .Include(p => p.Shop)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == "Approved" && (p.Shop == null || p.Shop.IsActive == true))
                    .Where(p => p.CategoryId == cat.CategoryId)
                    .OrderByDescending(p => p.SoldCount)
                    .Take(20).ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FeaturedProducts.Clear();
                    foreach (var p in items)
                    {
                        bool inWishlist = SessionManager.IsLoggedIn
                            && context.Wishlists.Any(w => w.UserId == SessionManager.CurrentUser!.UserId && w.ProductId == p.ProductId);
                        FeaturedProducts.Add(new ProductWrapper(p, inWishlist));
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Search by category failed: " + ex.Message);
            }
        }

        public void ShowAllFeatured()
        {
            _ = LoadFeaturedProductsAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                using var context = new TmdtContext();
                var cats = await context.Categories.AsNoTracking().Where(c => c.IsActive == true).OrderBy(c => c.SortOrder).ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var c in cats)
                        Categories.Add(c);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load categories failed: " + ex.Message);
            }
        }

        private async Task LoadFeaturedProductsAsync()
        {
            try
            {
                using var context = new TmdtContext();
                var items = await context.Products.AsNoTracking()
                    .Include(p => p.Shop)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == "Approved" && (p.Shop == null || p.Shop.IsActive == true))
                    .OrderByDescending(p => p.SoldCount)
                    .Take(10)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FeaturedProducts.Clear();
                    foreach (var p in items)
                    {
                        bool inWishlist = SessionManager.IsLoggedIn
                            && context.Wishlists.Any(w => w.UserId == SessionManager.CurrentUser!.UserId && w.ProductId == p.ProductId);
                        FeaturedProducts.Add(new ProductWrapper(p, inWishlist));
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load featured products failed: " + ex.Message);
            }
        }

        private async Task LoadBannersAsync()
        {
            try
            {
                using var context = new TmdtContext();
                var items = await context.Banners.AsNoTracking()
                    .Where(b => b.IsActive == true)
                    .OrderBy(b => b.SortOrder)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Banners.Clear();
                    foreach (var p in items)
                        Banners.Add(p);
                    if (Banners.Count > 0)
                    {
                        _currentBannerIndex = 0;
                        CurrentBanner = Banners[0];
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load banners failed: " + ex.Message);
            }
        }

        private void UpdateCartBadge()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CartBadgeCount = CartService.Instance.TotalItems;
            });
        }

        private void ExecuteLogout()
        {
            SessionManager.Clear();
            CartService.Instance.Clear();
            GoogleAuthService.Logout(); // Xóa bộ nhớ đệm của Google
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(UserName));
            NavigateHome();
        }

        private void ExecuteOpenSellerPortal()
        {
            var sellerWindow = new Views.Seller.SellerMainView();
            sellerWindow.Show();
            Application.Current.MainWindow?.Close();
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

        private void ExecuteLogin()
        {
            var loginView = new Views.Auth.LoginView();
            loginView.ShowDialog();
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(IsBuyer));
            OnPropertyChanged(nameof(IsSeller));
            OnPropertyChanged(nameof(UserName));
        }

        private void ExecuteBecomeSeller()
        {
            if (!SessionManager.IsBuyer)
            {
                MessageBox.Show("Bạn đã là Người bán hoặc không có quyền.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dialog = new Views.Seller.ShopRegistrationDialog
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show("Yêu cầu đăng ký shop đã được gửi!\nVui lòng chờ Admin phê duyệt.",
                    "Đang chờ duyệt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CartService.Instance.CartChanged -= UpdateCartBadge;
            }
            base.Dispose(disposing);
        }
    }
}

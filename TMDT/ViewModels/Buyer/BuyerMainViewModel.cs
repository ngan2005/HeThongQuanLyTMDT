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

        public ICommand GoHomeCommand { get; }
        public ICommand GoCartCommand { get; }
        public ICommand GoOrdersCommand { get; }
        public ICommand OpenProductCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenSellerPortalCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand BecomeSellerCommand { get; }
        public ICommand SearchCommand { get; }

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Product> FeaturedProducts { get; } = new();
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
            GoCartCommand = new RelayCommand(_ => NavigateCart());
            GoOrdersCommand = new RelayCommand(_ => NavigateOrders());
            OpenProductCommand = new RelayCommand(p => NavigateProductDetail(p as Product));
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            OpenSellerPortalCommand = new RelayCommand(_ => ExecuteOpenSellerPortal(), _ => IsLoggedIn && IsSeller);
            LoginCommand = new RelayCommand(_ => ExecuteLogin());
            BecomeSellerCommand = new RelayCommand(_ => ExecuteBecomeSeller(), _ => IsLoggedIn && IsBuyer);
            SearchCommand = new RelayCommand(_ => SearchProducts(SearchQuery));

            _ = LoadCategoriesAsync();
            _ = LoadFeaturedProductsAsync();
            _ = LoadBannersAsync();
            UpdateCartBadge();
        }

        public void NavigateHome()
        {
            PageTitle = "Trang chủ";
            CurrentViewModel = new BuyerHomeViewModel(this);
        }

        public void NavigateCart()
        {
            PageTitle = "Giỏ hàng";
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
            CurrentViewModel = new BuyerOrdersViewModel(this);
        }

        public void NavigateProductDetail(Product? product)
        {
            if (product == null) return;
            SelectedProduct = product;
            PageTitle = "Chi tiết sản phẩm";
            CurrentViewModel = new ProductDetailViewModel(product, this);
        }

        public void SearchProducts(string term)
        {
            _ = SearchProductsAsync(term);
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
                        FeaturedProducts.Add(p);
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
                        FeaturedProducts.Add(p);
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
                        FeaturedProducts.Add(p);
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

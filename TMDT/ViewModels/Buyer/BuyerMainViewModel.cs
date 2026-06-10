using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerMainViewModel : ViewModelBase, IDisposable
    {
        private ViewModelBase _currentViewModel;
        private Product? _selectedProduct;
        private int _cartBadgeCount;
        private string _pageTitle = "Trang chủ";

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
        public string UserName => SessionManager.CurrentUser?.FullName ?? "Khách";

        public ICommand GoHomeCommand { get; }
        public ICommand GoCartCommand { get; }
        public ICommand GoOrdersCommand { get; }
        public ICommand OpenProductCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenSellerPortalCommand { get; }
        public ICommand SearchCommand { get; }

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Product> FeaturedProducts { get; } = new();

        public BuyerMainViewModel()
        {
            _currentViewModel = new BuyerHomeViewModel(this);

            CartService.Instance.CartChanged += UpdateCartBadge;

            GoHomeCommand = new RelayCommand(_ => NavigateHome());
            GoCartCommand = new RelayCommand(_ => NavigateCart());
            GoOrdersCommand = new RelayCommand(_ => NavigateOrders());
            OpenProductCommand = new RelayCommand(p => NavigateProductDetail(p as Product));
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            OpenSellerPortalCommand = new RelayCommand(_ => ExecuteOpenSellerPortal());
            SearchCommand = new RelayCommand(term => SearchProducts(term?.ToString() ?? ""));

            LoadCategories();
            LoadFeaturedProducts();
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
            try
            {
                using var context = new TmdtContext();
                var query = context.Products.Where(p => p.Status == "Approved").AsQueryable();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    var t = term.Trim().ToLower();
                    query = query.Where(p =>
                        (p.ProductName != null && p.ProductName.ToLower().Contains(t)) ||
                        (p.Description != null && p.Description.ToLower().Contains(t)));
                }

                var items = query.OrderByDescending(p => p.SoldCount).Take(20).ToList();

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

        private void LoadCategories()
        {
            try
            {
                using var context = new TmdtContext();
                var cats = context.Categories.Where(c => c.IsActive == true).OrderBy(c => c.SortOrder).ToList();

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

        private void LoadFeaturedProducts()
        {
            try
            {
                using var context = new TmdtContext();
                var items = context.Products
                    .Where(p => p.Status == "Approved")
                    .OrderByDescending(p => p.SoldCount)
                    .Take(10)
                    .ToList();

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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Services.Interfaces;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class HomeViewModel : ViewModelBase
    {
        private string _searchQuery = "";
        public string SearchQuery
        {
            get => _searchQuery;
            set { _searchQuery = value; OnPropertyChanged(); SearchProducts(); }
        }

        public ObservableCollection<Category> Categories { get; set; }
        public ObservableCollection<Product> FeaturedProducts { get; set; }
        public ObservableCollection<Banner> Banners { get; set; }

        public ICommand LoginCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand BecomeSellerCommand { get; }
        public ICommand OpenSellerPortalCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ProductClickCommand { get; }

        public bool IsLoggedIn => SessionManager.IsLoggedIn;
        public bool IsBuyer => SessionManager.IsBuyer;
        public bool IsSeller => SessionManager.IsSeller;
        public string UserName => SessionManager.CurrentUser?.FullName ?? "";

        public HomeViewModel()
        {
            Categories = new ObservableCollection<Category>();
            FeaturedProducts = new ObservableCollection<Product>();
            Banners = new ObservableCollection<Banner>();

            LoginCommand = new RelayCommand(_ => ExecuteLogin());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            BecomeSellerCommand = new RelayCommand(_ => ExecuteBecomeSeller(), _ => IsLoggedIn && IsBuyer);
            OpenSellerPortalCommand = new RelayCommand(_ => ExecuteOpenSellerPortal(), _ => IsLoggedIn && IsSeller);
            SearchCommand = new RelayCommand(_ => SearchProducts());
            ProductClickCommand = new RelayCommand(p => ExecuteProductClick(p as Product));

            LoadCategories();
            LoadFeaturedProducts();
            LoadBanners();
        }

        private void LoadCategories()
        {
            try
            {
                using var context = new TmdtContext();
                var cats = context.Categories.Where(c => c.IsActive == true).OrderBy(c => c.SortOrder).Take(8).ToList();
                Categories.Clear();
                foreach (var c in cats)
                    Categories.Add(c);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadCategories failed: " + ex.Message); }
        }

        private void LoadFeaturedProducts()
        {
            try
            {
                using var context = new TmdtContext();
                var products = context.Products
                    .Where(p => p.Status == "Approved")
                    .OrderByDescending(p => p.SoldCount)
                    .Take(10)
                    .ToList();
                FeaturedProducts.Clear();
                foreach (var p in products)
                    FeaturedProducts.Add(p);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadFeaturedProducts failed: " + ex.Message); }
        }

        private void LoadBanners()
        {
            // Banner data can be loaded from DB or defined statically.
            // For now keep empty; can be extended with Banner table.
        }

        private void SearchProducts()
        {
            try
            {
                using var context = new TmdtContext();
                var query = context.Products.Where(p => p.Status == "Approved").AsQueryable();

                if (!string.IsNullOrWhiteSpace(_searchQuery))
                {
                    var term = _searchQuery.Trim().ToLower();
                    query = query.Where(p =>
                        (p.ProductName != null && p.ProductName.ToLower().Contains(term)) ||
                        (p.Description != null && p.Description.ToLower().Contains(term)));
                }

                var products = query.OrderByDescending(p => p.SoldCount).Take(10).ToList();
                FeaturedProducts.Clear();
                foreach (var p in products)
                    FeaturedProducts.Add(p);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("SearchProducts failed: " + ex.Message); }
        }

        private void ExecuteProductClick(Product product)
        {
            if (product == null) return;
            // TODO: Navigate to ProductDetailView
            MessageBox.Show($"Xem chi tiết: {product.ProductName}", "Sản phẩm", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ExecuteLogout()
        {
            SessionManager.Clear();
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(IsBuyer));
            OnPropertyChanged(nameof(IsSeller));
            OnPropertyChanged(nameof(UserName));
        }

        private void ExecuteBecomeSeller()
        {
            if (!SessionManager.IsBuyer)
            {
                MessageBox.Show("Bạn đã là Người bán hoặc không có quyền thực hiện.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Views.Seller.ShopRegistrationDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show(
                    "Yêu cầu đăng ký shop đã được gửi!\nVui lòng chờ Admin phê duyệt để bắt đầu bán hàng.",
                    "Đang chờ duyệt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteOpenSellerPortal()
        {
            var sellerWindow = new Views.Seller.SellerMainView();
            sellerWindow.Show();
            Application.Current.MainWindow?.Close();
        }
    }
}

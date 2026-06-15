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
using Microsoft.EntityFrameworkCore;

namespace TMDT.ViewModels.Buyer
{
    public class HomeViewModel : ViewModelBase
    {
        private string _searchQuery = "";
        public string SearchQuery
        {
            get => _searchQuery;
            set { _searchQuery = value; OnPropertyChanged(); _ = SearchProductsAsync(); }
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
            SearchCommand = new RelayCommand(_ => _ = SearchProductsAsync());
            ProductClickCommand = new RelayCommand(p => ExecuteProductClick(p as Product));

            _ = LoadCategoriesAsync();
            _ = LoadFeaturedProductsAsync();
            LoadBanners();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                using var context = new TmdtContext();
                var cats = await context.Categories.AsNoTracking().Where(c => c.IsActive == true).OrderBy(c => c.SortOrder).Take(8).ToListAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var c in cats)
                        Categories.Add(c);
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadCategories failed: " + ex.Message); }
        }

        private async Task LoadFeaturedProductsAsync()
        {
            try
            {
                using var context = new TmdtContext();
                var products = await context.Products.AsNoTracking()
                    .Include(p => p.Shop)
                    .Where(p => p.Status == "Approved" && (p.Shop == null || p.Shop.IsActive == true))
                    .OrderByDescending(p => p.SoldCount)
                    .Take(10)
                    .ToListAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FeaturedProducts.Clear();
                    foreach (var p in products)
                        FeaturedProducts.Add(p);
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadFeaturedProducts failed: " + ex.Message); }
        }

        private void LoadBanners()
        {
            // Banner data can be loaded from DB or defined statically.
            // For now keep empty; can be extended with Banner table.
        }

        private async Task SearchProductsAsync()
        {
            try
            {
                using var context = new TmdtContext();
                var query = context.Products.AsNoTracking()
                    .Include(p => p.Shop)
                    .Where(p => p.Status == "Approved" && (p.Shop == null || p.Shop.IsActive == true))
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(_searchQuery))
                {
                    var term = _searchQuery.Trim();
                    query = query.Where(p =>
                        (p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{term}%")) ||
                        (p.Description != null && EF.Functions.Like(p.Description, $"%{term}%")));
                }

                var products = await query.OrderByDescending(p => p.SoldCount).Take(10).ToListAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FeaturedProducts.Clear();
                    foreach (var p in products)
                        FeaturedProducts.Add(p);
                });
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

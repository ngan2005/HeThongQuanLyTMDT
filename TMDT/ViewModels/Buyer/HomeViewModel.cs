using System;
using System.Collections.ObjectModel;
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
        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set { _searchQuery = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Category> Categories { get; set; }
        public ObservableCollection<Product> FeaturedProducts { get; set; }
        public ObservableCollection<Banner> Banners { get; set; }

        public ICommand LoginCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand BecomeSellerCommand { get; }
        public ICommand OpenSellerPortalCommand { get; }

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

            LoadCategories();
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
            catch { }
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

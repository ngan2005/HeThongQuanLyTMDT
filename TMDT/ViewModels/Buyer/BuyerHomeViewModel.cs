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
    public class BuyerHomeViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;

        public string SearchQuery { get; set; } = "";

        public ObservableCollection<Category> Categories => _mainVm.Categories;
        public ObservableCollection<Product> FeaturedProducts => _mainVm.FeaturedProducts;

        public ICommand SearchCommand { get; }
        public ICommand ProductClickCommand { get; }
        public ICommand CartCommand { get; }
        public ICommand OrdersCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand BecomeSellerCommand { get; }
        public ICommand OpenSellerPortalCommand { get; }

        public bool IsLoggedIn => SessionManager.IsLoggedIn;
        public bool IsBuyer => SessionManager.IsBuyer;
        public bool IsSeller => SessionManager.IsSeller;
        public string UserName => SessionManager.CurrentUser?.FullName ?? "";

        public int CartBadgeCount => CartService.Instance.TotalItems;

        public BuyerHomeViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;

            SearchCommand = new RelayCommand(_ => ExecuteSearch());
            ProductClickCommand = new RelayCommand(p => _mainVm.NavigateProductDetail(p as Product));
            CartCommand = new RelayCommand(_ => _mainVm.NavigateCart());
            OrdersCommand = new RelayCommand(_ => _mainVm.NavigateOrders());
            LoginCommand = new RelayCommand(_ => ExecuteLogin());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            BecomeSellerCommand = new RelayCommand(_ => ExecuteBecomeSeller(), _ => IsLoggedIn && IsBuyer);
            OpenSellerPortalCommand = new RelayCommand(_ => ExecuteOpenSellerPortal(), _ => IsLoggedIn && IsSeller);

            CartService.Instance.CartChanged += () => OnPropertyChanged(nameof(CartBadgeCount));
        }

        private void ExecuteSearch()
        {
            _mainVm.SearchProducts(SearchQuery);
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
            CartService.Instance.Clear();
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

        private void ExecuteOpenSellerPortal()
        {
            var sellerWindow = new Views.Seller.SellerMainView();
            sellerWindow.Show();
            Application.Current.MainWindow?.Close();
        }
    }
}

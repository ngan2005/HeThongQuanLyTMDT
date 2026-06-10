using System;
using System.Windows;
using System.Windows.Input;
using TMDT.Utilities;
using TMDT.Services;
using TMDT.Services.Interfaces;

namespace TMDT.ViewModels.Seller
{
    public class SellerMainViewModel : ViewModelBase
    {
        private ViewModelBase _currentView;
        public ViewModelBase CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        private string _sellerName = "Seller";
        public string SellerName
        {
            get => _sellerName;
            set { _sellerName = value; OnPropertyChanged(); }
        }

        private string _shopName = "";
        public string ShopName
        {
            get => _shopName;
            set { _shopName = value; OnPropertyChanged(); }
        }

        private string _activeMenu = "Dashboard";
        public string ActiveMenu
        {
            get => _activeMenu;
            set { _activeMenu = value; OnPropertyChanged(); }
        }

        private bool _isSidebarExpanded = true;
        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set { _isSidebarExpanded = value; OnPropertyChanged(); }
        }

        private bool _hasShop;
        public bool HasShop
        {
            get => _hasShop;
            set { _hasShop = value; OnPropertyChanged(); }
        }

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowOrdersCommand { get; }
        public ICommand ShowVouchersCommand { get; }
        public ICommand ShowWalletCommand { get; }
        public ICommand ShowProfileCommand { get; }
        public ICommand RegisterShopCommand { get; }
        public ICommand LogoutCommand { get; }

        public SellerMainViewModel()
        {
            if (!SessionManager.IsSeller)
            {
                MessageBox.Show("Bạn không có quyền truy cập trang Seller.", "Cảnh báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
                return;
            }

            if (SessionManager.CurrentUser != null)
            {
                SellerName = SessionManager.CurrentUser.FullName ?? "Seller";
                ShopName = SessionManager.CurrentUser.ShopName ?? "";
                HasShop = SessionManager.CurrentUser.ShopId.HasValue;
            }

            // Nếu seller chưa có shop, tạo dashboard placeholder
            if (!HasShop)
            {
                // Không load dashboard bình thường, hiện thông báo đăng ký shop
                HasShop = CheckHasShopInDb();
            }

            ShowDashboardCommand = new RelayCommand(o => { CurrentView = new SellerDashboardViewModel(); ActiveMenu = "Dashboard"; });
            ShowProductsCommand = new RelayCommand(o => { CurrentView = new SellerProductsViewModel(); ActiveMenu = "Products"; }, _ => HasShop);
            ShowOrdersCommand = new RelayCommand(o => { CurrentView = new SellerOrdersViewModel(); ActiveMenu = "Orders"; }, _ => HasShop);
            ShowVouchersCommand = new RelayCommand(o => { CurrentView = new SellerVouchersViewModel(); ActiveMenu = "Vouchers"; }, _ => HasShop);
            ShowWalletCommand = new RelayCommand(o => { CurrentView = new SellerWalletViewModel(); ActiveMenu = "Wallet"; }, _ => HasShop);
            ShowProfileCommand = new RelayCommand(o => { CurrentView = new SellerProfileViewModel(); ActiveMenu = "Profile"; });
            RegisterShopCommand = new RelayCommand(_ => ExecuteRegisterShop());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());

            CurrentView = new SellerDashboardViewModel();
        }

        private bool CheckHasShopInDb()
        {
            try
            {
                var user = SessionManager.CurrentUser;
                if (user == null) return false;
                var service = new ShopService(new Models.TmdtContext());
                return service.HasShopForUserAsync(user.UserId).GetAwaiter().GetResult();
            }
            catch { return false; }
        }

        private void ExecuteRegisterShop()
        {
            var dialog = new Views.Seller.ShopRegistrationDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                // Reload thông tin sau khi đăng ký thành công
                var user = SessionManager.CurrentUser;
                if (user != null)
                {
                    var service = new ShopService(new Models.TmdtContext());
                    var hasShop = service.HasShopForUserAsync(user.UserId).GetAwaiter().GetResult();
                    if (hasShop)
                    {
                        MessageBox.Show(
                            "Shop đã được gửi yêu cầu đăng ký.\nVui lòng chờ Admin phê duyệt trước khi bắt đầu bán hàng.",
                            "Chờ duyệt", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    HasShop = hasShop;
                }
            }
        }

        private void ExecuteLogout()
        {
            SessionManager.Clear();
            Application.Current.Shutdown();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) (_currentView as IDisposable)?.Dispose();
            base.Dispose(disposing);
        }
    }
}

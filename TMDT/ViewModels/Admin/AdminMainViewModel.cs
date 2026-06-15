using System;
using System.Windows;
using System.Windows.Input;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminMainViewModel : ViewModelBase
    {
        private ViewModelBase _currentView;
        public ViewModelBase CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        private AdminChatViewModel _chatViewModel;
        public AdminChatViewModel ChatViewModel
        {
            get => _chatViewModel;
            set { _chatViewModel = value; OnPropertyChanged(); }
        }

        public string AdminName   => SessionManager.CurrentUser?.FullName ?? "Administrator";
        public string AdminRole   => SessionManager.CurrentUser?.RoleName ?? "Admin";
        public string AdminAvatar => SessionManager.CurrentUser?.Avatar   ?? "";

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

        // Navigation Commands
        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowOrdersCommand { get; }
        public ICommand ShowOrdersPendingCommand { get; }
        public ICommand ShowOrdersShippingCommand { get; }
        public ICommand ShowOrdersRefundCommand { get; }
        public ICommand ShowShopsCommand { get; }
        public ICommand ShowShopsPendingCommand { get; }
        public ICommand ShowShopsActiveCommand { get; }
        public ICommand ShowShopsSuspendedCommand { get; }
        public ICommand ShowProductsPendingCommand { get; }
        public ICommand ShowProductsActiveCommand { get; }
        public ICommand ShowProductsReviewsCommand { get; }
        public ICommand ShowWithdrawsCommand { get; }
        public ICommand ShowComplaintsCommand { get; }
        public ICommand ShowMarketingBannersCommand { get; }
        public ICommand ShowMarketingVouchersCommand { get; }
        public ICommand ShowMarketingFlashSalesCommand { get; }
        public ICommand ShowCategoriesCommand { get; }
        public ICommand ShowReportsCommand { get; }
        public ICommand ShowAdminsCommand { get; }
        public ICommand ShowSellersCommand { get; }
        public ICommand ShowBuyersCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowAuditLogsCommand { get; }
        public ICommand ShowProfileCommand { get; }
        public ICommand LogoutCommand { get; }

        public AdminMainViewModel()
        {
            // Kiểm tra quyền — chặn Buyer/Seller vào Admin portal
            if (!SessionManager.IsAdmin)
            {
                MessageBox.Show("Bạn không có quyền truy cập trang quản trị.", "Cảnh báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
                return;
            }

            // Initialize Chat ViewModel
            _chatViewModel = new AdminChatViewModel();

            // Set default view on startup
            _currentView = new AdminDashboardViewModel();
            _activeMenu = "Dashboard";

            // Initialize Commands
            ShowDashboardCommand = new RelayCommand(o => { CurrentView = new AdminDashboardViewModel(); ActiveMenu = "Dashboard"; });
            ShowOrdersCommand = new RelayCommand(o => { CurrentView = new AdminOrdersViewModel("Tất cả"); ActiveMenu = "Orders"; });
            ShowOrdersPendingCommand = new RelayCommand(o => { CurrentView = new AdminOrdersViewModel("Chờ xác nhận"); ActiveMenu = "OrdersPending"; });
            ShowOrdersShippingCommand = new RelayCommand(o => { CurrentView = new AdminOrdersViewModel("Đang giao hàng"); ActiveMenu = "OrdersShipping"; });
            ShowOrdersRefundCommand = new RelayCommand(o => { CurrentView = new AdminOrdersViewModel("Hoàn tiền"); ActiveMenu = "OrdersRefund"; });
            
            ShowShopsCommand = new RelayCommand(o => { CurrentView = new AdminShopsViewModel("All"); ActiveMenu = "Shops"; });
            ShowShopsPendingCommand = new RelayCommand(o => { CurrentView = new AdminShopsViewModel("Pending"); ActiveMenu = "ShopsPending"; });
            ShowShopsActiveCommand = new RelayCommand(o => { CurrentView = new AdminShopsViewModel("Active"); ActiveMenu = "ShopsActive"; });
            ShowShopsSuspendedCommand = new RelayCommand(o => { CurrentView = new AdminShopsViewModel("Suspended"); ActiveMenu = "ShopsSuspended"; });
            ShowProductsPendingCommand = new RelayCommand(o => { CurrentView = new AdminProductsViewModel("Pending"); ActiveMenu = "ProductsPending"; });
            ShowProductsActiveCommand = new RelayCommand(o => { CurrentView = new AdminProductsViewModel("Approved"); ActiveMenu = "ProductsActive"; });
            ShowProductsReviewsCommand = new RelayCommand(o => { CurrentView = new AdminProductsViewModel("Reviews"); ActiveMenu = "ProductsReviews"; });
            ShowWithdrawsCommand = new RelayCommand(o => { CurrentView = new AdminWithdrawsViewModel(); ActiveMenu = "Withdraws"; });
            ShowComplaintsCommand = new RelayCommand(o => { CurrentView = new AdminComplaintsViewModel(); ActiveMenu = "Complaints"; });
            ShowMarketingBannersCommand = new RelayCommand(o => { CurrentView = new AdminMarketingViewModel("Banners"); ActiveMenu = "MarketingBanners"; });
            ShowMarketingVouchersCommand = new RelayCommand(o => { CurrentView = new AdminMarketingViewModel("Vouchers"); ActiveMenu = "MarketingVouchers"; });
            ShowMarketingFlashSalesCommand = new RelayCommand(o => { CurrentView = new AdminMarketingViewModel("FlashSales"); ActiveMenu = "MarketingFlashSales"; });
            ShowCategoriesCommand = new RelayCommand(o => { CurrentView = new AdminCategoriesViewModel(); ActiveMenu = "Categories"; });
            ShowReportsCommand = new RelayCommand(o => { CurrentView = new AdminReportsViewModel(); ActiveMenu = "Reports"; });
            ShowAdminsCommand = new RelayCommand(o => { CurrentView = new AdminUsersViewModel(SessionManager.RoleAdmin); ActiveMenu = "Admins"; });
            ShowSellersCommand = new RelayCommand(o => { CurrentView = new AdminUsersViewModel(SessionManager.RoleSeller); ActiveMenu = "Sellers"; });
            ShowBuyersCommand = new RelayCommand(o => { CurrentView = new AdminUsersViewModel(SessionManager.RoleBuyer); ActiveMenu = "Buyers"; });
            ShowSettingsCommand = new RelayCommand(o => { CurrentView = new AdminSettingsViewModel(); ActiveMenu = "Settings"; });
            ShowAuditLogsCommand = new RelayCommand(o => { CurrentView = new AdminAuditLogsViewModel(); ActiveMenu = "AuditLogs"; });
            ShowProfileCommand = new RelayCommand(o =>
            {
                // Tạo mới ViewModel mỗi lần vào Profile để đảm bảo dữ liệu mới nhất từ DB
                var profileVm = new AdminProfileViewModel();
                CurrentView = profileVm;
                ActiveMenu = "Profile";
            });
            LogoutCommand = new RelayCommand(o => ExecuteLogout());

            // Set default view
            CurrentView = new AdminDashboardViewModel();
        }

        private void ExecuteLogout()
        {
            SessionManager.Clear();
            var mainWindow = new Views.MainWindow();
            mainWindow.Show();

            var windowsToClose = new System.Collections.Generic.List<Window>();
            foreach (Window win in Application.Current.Windows)
            {
                if (win is Views.Admin.AdminMainView)
                {
                    windowsToClose.Add(win);
                }
            }

            foreach (var win in windowsToClose)
            {
                win.Close();
            }
        }
    }
}

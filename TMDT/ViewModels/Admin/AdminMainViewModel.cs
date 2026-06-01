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

        public string AdminName => SessionManager.CurrentUser?.FullName ?? "Administrator";
        public string AdminRole => SessionManager.CurrentUser?.RoleName ?? "Admin";

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
        public ICommand ShowShopsCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowWithdrawsCommand { get; }
        public ICommand ShowComplaintsCommand { get; }
        public ICommand ShowMarketingCommand { get; }
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

            // Initialize Commands
            ShowDashboardCommand = new RelayCommand(o => { CurrentView = new AdminDashboardViewModel(); ActiveMenu = "Dashboard"; });
            ShowOrdersCommand = new RelayCommand(o => { CurrentView = new AdminOrdersViewModel(); ActiveMenu = "Orders"; });
            ShowShopsCommand = new RelayCommand(o => { CurrentView = new AdminShopsViewModel(); ActiveMenu = "Shops"; });
            ShowProductsCommand = new RelayCommand(o => { CurrentView = new AdminProductsViewModel(); ActiveMenu = "Products"; });
            ShowWithdrawsCommand = new RelayCommand(o => { CurrentView = new AdminWithdrawsViewModel(); ActiveMenu = "Withdraws"; });
            ShowComplaintsCommand = new RelayCommand(o => { CurrentView = new AdminComplaintsViewModel(); ActiveMenu = "Complaints"; });
            ShowMarketingCommand = new RelayCommand(o => { CurrentView = new AdminMarketingViewModel(); ActiveMenu = "Marketing"; });
            ShowCategoriesCommand = new RelayCommand(o => { CurrentView = new AdminCategoriesViewModel(); ActiveMenu = "Categories"; });
            ShowReportsCommand = new RelayCommand(o => { CurrentView = new AdminReportsViewModel(); ActiveMenu = "Reports"; });
            ShowAdminsCommand = new RelayCommand(o => { CurrentView = new AdminUsersViewModel("Admin"); ActiveMenu = "Admins"; });
            ShowSellersCommand = new RelayCommand(o => { CurrentView = new AdminUsersViewModel("Seller"); ActiveMenu = "Sellers"; });
            ShowBuyersCommand = new RelayCommand(o => { CurrentView = new AdminUsersViewModel("Buyer"); ActiveMenu = "Buyers"; });
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
            Application.Current.Shutdown();
        }
    }
}

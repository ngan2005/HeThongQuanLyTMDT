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
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        private string _adminName = "Administrator";
        public string AdminName
        {
            get => _adminName;
            set
            {
                _adminName = value;
                OnPropertyChanged();
            }
        }

        private string _activeMenu = "Dashboard";
        public string ActiveMenu
        {
            get => _activeMenu;
            set
            {
                _activeMenu = value;
                OnPropertyChanged();
            }
        }

        // Navigation Commands
        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowShopsCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowWithdrawsCommand { get; }
        public ICommand ShowComplaintsCommand { get; }
        public ICommand ShowMarketingCommand { get; }
        public ICommand ShowCategoriesCommand { get; }
        public ICommand ShowReportsCommand { get; }
        public ICommand ShowUsersCommand { get; }
        public ICommand ShowSettingsCommand { get; }

        public AdminMainViewModel()
        {
            // Initialize Commands
            ShowDashboardCommand = new RelayCommand(o => { CurrentView = new AdminDashboardViewModel(); ActiveMenu = "Dashboard"; });
            ShowShopsCommand = new RelayCommand(o => { CurrentView = new AdminShopsViewModel(); ActiveMenu = "Shops"; });
            ShowProductsCommand = new RelayCommand(o => { CurrentView = new AdminProductsViewModel(); ActiveMenu = "Products"; });
            ShowWithdrawsCommand = new RelayCommand(o => { CurrentView = new AdminWithdrawsViewModel(); ActiveMenu = "Withdraws"; });
            ShowComplaintsCommand = new RelayCommand(o => { CurrentView = new AdminComplaintsViewModel(); ActiveMenu = "Complaints"; });
            ShowMarketingCommand = new RelayCommand(o => { CurrentView = new AdminMarketingViewModel(); ActiveMenu = "Marketing"; });
            ShowCategoriesCommand = new RelayCommand(o => { CurrentView = new AdminCategoriesViewModel(); ActiveMenu = "Categories"; });
            ShowReportsCommand = new RelayCommand(o => { CurrentView = new AdminReportsViewModel(); ActiveMenu = "Reports"; });
            ShowUsersCommand = new RelayCommand(o => { CurrentView = new AdminUsersViewModel(); ActiveMenu = "Users"; });
            ShowSettingsCommand = new RelayCommand(o => { CurrentView = new AdminSettingsViewModel(); ActiveMenu = "Settings"; });

            // Set default view
            CurrentView = new AdminDashboardViewModel();
        }
    }
}

using System.Windows.Input;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class SellerMainViewModel : ViewModelBase
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

        private string _sellerName = "Chủ Shop Đẹp Trai";
        public string SellerName
        {
            get => _sellerName;
            set
            {
                _sellerName = value;
                OnPropertyChanged();
            }
        }

        private string _shopName = "MyShop Premium Store";
        public string ShopName
        {
            get => _shopName;
            set
            {
                _shopName = value;
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

        private bool _isSidebarExpanded = true;
        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set
            {
                _isSidebarExpanded = value;
                OnPropertyChanged();
            }
        }

        // Navigation Commands
        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowOrdersCommand { get; }
        public ICommand ShowVouchersCommand { get; }
        public ICommand ShowWalletCommand { get; }
        public ICommand ShowProfileCommand { get; }

        public SellerMainViewModel()
        {
            // Initialize Commands
            ShowDashboardCommand = new RelayCommand(o => { CurrentView = new SellerDashboardViewModel(); ActiveMenu = "Dashboard"; });
            ShowProductsCommand = new RelayCommand(o => { CurrentView = new SellerProductsViewModel(); ActiveMenu = "Products"; });
            ShowOrdersCommand = new RelayCommand(o => { CurrentView = new SellerOrdersViewModel(); ActiveMenu = "Orders"; });
            ShowVouchersCommand = new RelayCommand(o => { CurrentView = new SellerVouchersViewModel(); ActiveMenu = "Vouchers"; });
            ShowWalletCommand = new RelayCommand(o => { CurrentView = new SellerWalletViewModel(); ActiveMenu = "Wallet"; });
            ShowProfileCommand = new RelayCommand(o => { CurrentView = new SellerProfileViewModel(); ActiveMenu = "Profile"; });

            // Set default view
            CurrentView = new SellerDashboardViewModel();
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TMDT.Utilities;
using TMDT.Services;
using TMDT.Services.Interfaces;

namespace TMDT.ViewModels.Seller
{
    public class SellerMainViewModel : ViewModelBase
    {
        private ViewModelBase? _currentView;
        public ViewModelBase? CurrentView
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

        private string _shopLogo = "";
        public string ShopLogo
        {
            get => _shopLogo;
            set { _shopLogo = value; OnPropertyChanged(); }
        }

        private string _activeMenu = "Dashboard";
        public string ActiveMenu
        {
            get => _activeMenu;
            set { _activeMenu = value; OnPropertyChanged(); }
        }

        /// <summary>🟢 Số SKU sắp hết hàng của shop — bind cho badge sidebar.</summary>
        private int _lowStockCount;
        public int LowStockCount
        {
            get => _lowStockCount;
            set { _lowStockCount = value; OnPropertyChanged(); }
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

        public bool IsOwner => SessionManager.IsSeller;

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowPosCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowOrdersCommand { get; }
        public ICommand ShowReturnRequestsCommand { get; }
        public ICommand ShowVouchersCommand { get; }
        public ICommand ShowReviewsCommand { get; }
        public ICommand ShowChatCommand { get; }
        public ICommand ShowWalletCommand { get; }
        public ICommand ShowProfileCommand { get; }
        public ICommand ShowSalesHistoryCommand { get; }
        public ICommand ShowCustomersCommand { get; }
        public ICommand ShowInventoryCommand { get; }
        public ICommand RegisterShopCommand { get; }
        public ICommand LogoutCommand { get; }

        public SellerMainViewModel()
        {
            if (!SessionManager.IsSeller && !SessionManager.IsStaff)
            {
                MessageBox.Show("Bạn không có quyền truy cập trang Seller/Staff.", "Cảnh báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
                return;
            }

            if (SessionManager.CurrentUser != null)
            {
                SellerName = SessionManager.CurrentUser.FullName ?? (SessionManager.IsStaff ? "Nhân viên" : "Seller");
                ShopName = SessionManager.CurrentUser.ShopName ?? "";
                HasShop = SessionManager.CurrentUser.ShopId.HasValue;
                LoadShopLogoFromDb();
            }

            // Nếu seller chưa có shop, tạo dashboard placeholder
            if (!HasShop)
            {
                // Không load dashboard bình thường, hiện thông báo đăng ký shop
                HasShop = CheckHasShopInDb();
            }

            ShowDashboardCommand = new RelayCommand(o => { CurrentView = new SellerDashboardViewModel(); ActiveMenu = "Dashboard"; });
            ShowPosCommand = new RelayCommand(o => { CurrentView = new SellerPosViewModel(); ActiveMenu = "POS"; }, _ => HasShop);
            ShowProductsCommand = new RelayCommand(o => { CurrentView = new SellerProductsViewModel(); ActiveMenu = "Products"; }, _ => HasShop);
            ShowOrdersCommand = new RelayCommand(o => { CurrentView = new SellerOrdersViewModel(); ActiveMenu = "Orders"; }, _ => HasShop);
            ShowReturnRequestsCommand = new RelayCommand(o => { CurrentView = new SellerReturnRequestsViewModel(); ActiveMenu = "ReturnRequests"; }, _ => HasShop);
            ShowVouchersCommand = new RelayCommand(o => { CurrentView = new SellerVouchersViewModel(); ActiveMenu = "Vouchers"; }, _ => HasShop);
            ShowReviewsCommand = new RelayCommand(o => { CurrentView = new SellerReviewsViewModel(); ActiveMenu = "Reviews"; }, _ => HasShop);
            ShowChatCommand = new RelayCommand(o => { CurrentView = new SellerChatViewModel(); ActiveMenu = "Chat"; }, _ => HasShop);
            ShowWalletCommand = new RelayCommand(o => { CurrentView = new SellerWalletViewModel(); ActiveMenu = "Wallet"; }, _ => HasShop);
            ShowProfileCommand = new RelayCommand(o => { 
                var vm = new SellerProfileViewModel();
                vm.RequestNavigateToWallet += () => {
                    CurrentView = new SellerWalletViewModel();
                    ActiveMenu = "Wallet";
                };
                vm.CloseProfileRequest += () => {
                    LoadShopLogoFromDb();
                    // Cập nhật lại tên shop nếu bị đổi
                    if (SessionManager.CurrentUser != null && vm.Shop != null)
                    {
                        ShopName = vm.ShopNameInput;
                    }
                };
                CurrentView = vm; 
                ActiveMenu = "Profile"; 
            });
            ShowSalesHistoryCommand = new RelayCommand(o => { CurrentView = new SellerSalesHistoryViewModel(); ActiveMenu = "SalesHistory"; }, _ => HasShop);
            ShowCustomersCommand = new RelayCommand(o => { CurrentView = new SellerCustomersViewModel(); ActiveMenu = "Customers"; }, _ => HasShop);
            // 🟢 Inventory: cần cập nhật badge low-stock mỗi lần mở trang
            ShowInventoryCommand = new RelayCommand(o =>
            {
                CurrentView = new SellerInventoryViewModel();
                ActiveMenu = "Inventory";
                _ = RefreshLowStockCountAsync();
            }, _ => HasShop);
            RegisterShopCommand = new RelayCommand(_ => ExecuteRegisterShop());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());

            // 🟢 Refresh badge low-stock lúc khởi tạo
            _ = RefreshLowStockCountAsync();

            if (IsOwner)
            {
                CurrentView = new SellerDashboardViewModel();
                ActiveMenu = "Dashboard";
            }
            else
            {
                CurrentView = new SellerPosViewModel();
                ActiveMenu = "POS";
            }
        }

        private void LoadShopLogoFromDb()
        {
            try
            {
                int currentShopId = SessionManager.CurrentUser?.ShopId ?? 0;
                if (currentShopId <= 0) return;

                using var ctx = new Models.TmdtContext();
                var shop = ctx.Shops.Find(currentShopId);
                if (shop != null)
                {
                    ShopLogo = shop.Logo ?? "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadShopLogoFromDb error: " + ex.Message);
            }
        }

        /// <summary>🟢 Cập nhật badge LowStockCount ở sidebar — chạy async, fire-and-forget.</summary>
        private async Task RefreshLowStockCountAsync()
        {
            try
            {
                int shopId = SessionManager.CurrentUser?.ShopId ?? 0;
                if (shopId <= 0)
                {
                    var user = SessionManager.CurrentUser;
                    if (user != null)
                    {
                        using var ctx = new Models.TmdtContext();
                        shopId = ctx.Shops.Where(s => s.UserId == user.UserId).Select(s => s.ShopId).FirstOrDefault();
                    }
                }
                if (shopId <= 0)
                {
                    LowStockCount = 0;
                    return;
                }
                LowStockCount = await InventoryService.Instance.GetLowStockCountAsync(shopId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RefreshLowStockCountAsync error: " + ex.Message);
            }
        }

        private bool CheckHasShopInDb()
        {
            try
            {
                var user = SessionManager.CurrentUser;
                if (user == null) return false;
                using var ctx = new Models.TmdtContext();
                return System.Linq.Queryable.Any(ctx.Shops, s => s.UserId == user.UserId);
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
                    using var ctx = new Models.TmdtContext();
                    var hasShop = System.Linq.Queryable.Any(ctx.Shops, s => s.UserId == user.UserId);
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
            var mainWindow = new Views.MainWindow();
            mainWindow.Show();

            var windowsToClose = new System.Collections.Generic.List<Window>();
            foreach (Window win in Application.Current.Windows)
            {
                if (win is Views.Seller.SellerMainView)
                {
                    windowsToClose.Add(win);
                }
            }

            foreach (var win in windowsToClose)
            {
                win.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) (_currentView as IDisposable)?.Dispose();
            base.Dispose(disposing);
        }
    }
}

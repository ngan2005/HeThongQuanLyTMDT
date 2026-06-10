using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class SellerProfileViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        private Shop _shop;

        private string _shopNameInput;
        private string _logoInput;
        private string _warehouseAddressInput;
        private decimal _commissionRate;
        private bool _vacationMode;
        private string _openedAtDisplay;

        private int _totalProducts;
        private int _totalOrders;
        private decimal _walletBalance;
        private decimal _shopRating;

        public Shop Shop
        {
            get => _shop;
            set { _shop = value; OnPropertyChanged(); }
        }

        #region Input Properties
        public string ShopNameInput
        {
            get => _shopNameInput;
            set { _shopNameInput = value; OnPropertyChanged(); }
        }
        public string LogoInput
        {
            get => _logoInput;
            set { _logoInput = value; OnPropertyChanged(); }
        }
        public string WarehouseAddressInput
        {
            get => _warehouseAddressInput;
            set { _warehouseAddressInput = value; OnPropertyChanged(); }
        }
        public decimal CommissionRate
        {
            get => _commissionRate;
            set { _commissionRate = value; OnPropertyChanged(); }
        }
        public bool VacationMode
        {
            get => _vacationMode;
            set
            {
                _vacationMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VacationBg));
                OnPropertyChanged(nameof(VacationFg));
                OnPropertyChanged(nameof(VacationText));
            }
        }
        public string OpenedAtDisplay
        {
            get => _openedAtDisplay;
            set { _openedAtDisplay = value; OnPropertyChanged(); }
        }
        #endregion

        #region Dashboard Stats
        public int TotalProducts
        {
            get => _totalProducts;
            set { _totalProducts = value; OnPropertyChanged(); }
        }
        public int TotalOrders
        {
            get => _totalOrders;
            set { _totalOrders = value; OnPropertyChanged(); }
        }
        public decimal WalletBalance
        {
            get => _walletBalance;
            set { _walletBalance = value; OnPropertyChanged(); OnPropertyChanged(nameof(WalletBalanceDisplay)); }
        }
        public string WalletBalanceDisplay => WalletBalance >= 1000000
            ? (WalletBalance / 1000000m).ToString("N1") + "M"
            : WalletBalance.ToString("N0");
        public decimal ShopRating
        {
            get => _shopRating;
            set { _shopRating = value; OnPropertyChanged(); }
        }
        public string VacationText => VacationMode ? "Đang tạm nghỉ" : "Bình thường";
        public Brush VacationBg => VacationMode
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7ED"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));
        public Brush VacationFg => VacationMode
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EA580C"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
        #endregion

        #region Events
        public event Action OpenProfileRequest;
        public event Action CloseProfileRequest;
        #endregion

        #region Commands
        public ICommand SaveProfileCommand { get; }
        public ICommand OpenProfileCommand { get; }
        public ICommand ToggleVacationCommand { get; }
        public ICommand WithdrawCommand { get; }
        #endregion

        public SellerProfileViewModel()
        {
            try { _context = new TmdtContext(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Init TmdtContext failed: " + ex.Message); }

            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile);
            OpenProfileCommand = new RelayCommand(_ => OpenProfileRequest?.Invoke());
            ToggleVacationCommand = new RelayCommand(_ => ExecuteToggleVacation());
            WithdrawCommand = new RelayCommand(_ => MessageBox.Show("Tính năng rút tiền đang được phát triển.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information));

            LoadShopProfile();
        }

        private void LoadShopProfile()
        {
            int currentShopId = GetCurrentShopId();
            if (currentShopId <= 0) return;

            try
            {
                if (_context == null) return;

                var dbShop = _context.Shops.Find(currentShopId);
                if (dbShop == null) return;

                Shop = dbShop;
                PopulateFields();

                // Load stats
                TotalProducts = _context.Products.Count(p => p.ShopId == currentShopId && p.Status == "Approved");
                TotalOrders = _context.Orders.Count(o => o.ShopId == currentShopId);
                WalletBalance = dbShop.WalletBalance ?? 0;
                ShopRating = dbShop.Rating ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load shop profile: " + ex.Message);
            }
        }

        private void PopulateFields()
        {
            if (Shop == null) return;
            ShopNameInput = Shop.ShopName;
            LogoInput = Shop.Logo;
            WarehouseAddressInput = Shop.WarehouseAddress;
            CommissionRate = Shop.CommissionRate ?? 3.0m;
            VacationMode = Shop.VacationMode ?? false;
            OpenedAtDisplay = Shop.OpenedAt.HasValue ? Shop.OpenedAt.Value.ToString("dd/MM/yyyy") : DateTime.Now.ToString("dd/MM/yyyy");
        }

        private void ExecuteToggleVacation()
        {
            VacationMode = !VacationMode;
            Shop.VacationMode = VacationMode;

            try
            {
                var dbShop = _context?.Shops.Find(Shop.ShopId);
                if (dbShop != null)
                {
                    dbShop.VacationMode = VacationMode;
                    _context?.SaveChanges();
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("ExecuteToggleVacation failed: " + ex.Message); }

            MessageBox.Show(VacationMode
                ? "Đã bật chế độ tạm nghỉ. Khách hàng không thể đặt đơn hàng."
                : "Đã tắt chế độ tạm nghỉ. Shop hoạt động bình thường.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExecuteSaveProfile(object obj)
        {
            if (string.IsNullOrWhiteSpace(ShopNameInput))
            {
                MessageBox.Show("Tên cửa hàng không được để trống!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(WarehouseAddressInput))
            {
                MessageBox.Show("Địa chỉ kho không được để trống!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Shop.ShopName = ShopNameInput;
            Shop.Logo = LogoInput;
            Shop.WarehouseAddress = WarehouseAddressInput;
            Shop.VacationMode = VacationMode;

            try
            {
                var dbShop = await _context.Shops.FindAsync(Shop.ShopId);
                if (dbShop != null)
                {
                    dbShop.ShopName = ShopNameInput;
                    dbShop.Logo = LogoInput;
                    dbShop.WarehouseAddress = WarehouseAddressInput;
                    dbShop.VacationMode = VacationMode;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF save failed: " + ex.Message);
            }

            MessageBox.Show("Đã lưu cấu hình thông tin Shop thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseProfileRequest?.Invoke();
        }

        private int GetCurrentShopId()
        {
            try
            {
                if (_context == null) return 0;
                if (SessionManager.CurrentUser == null) return 0;

                var shop = _context.Shops
                    .FirstOrDefault(s => s.UserId == SessionManager.CurrentUser.UserId);
                return shop?.ShopId ?? 0;
            }
            catch { return 0; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}

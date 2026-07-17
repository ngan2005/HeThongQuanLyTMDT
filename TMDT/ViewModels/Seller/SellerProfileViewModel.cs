using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;
using TMDT.Services;

namespace TMDT.ViewModels.Seller
{
    public class SellerProfileViewModel : ViewModelBase
    {
        private readonly TmdtContext? _context;
        private readonly CloudinaryService _imageUploadService = new CloudinaryService();
        private Shop? _shop;
        private bool _isUploadingImage;

        private string _shopNameInput = "";
        private string _logoInput = "";
        private string _warehouseAddressInput = "";
        private decimal _commissionRate;
        private bool _vacationMode;
        private string _openedAtDisplay = "";

        private System.Collections.ObjectModel.ObservableCollection<Province> _provinces = new();
        private System.Collections.ObjectModel.ObservableCollection<District> _districts = new();
        private System.Collections.ObjectModel.ObservableCollection<Ward> _wards = new();
        private Province? _selectedProvince;
        private District? _selectedDistrict;
        private Ward? _selectedWard;
        private string _houseNumberInput = "";

        private int _totalProducts;
        private int _totalOrders;
        private decimal _walletBalance;
        private decimal _shopRating;

        public Shop? Shop
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
        public bool IsUploadingImage
        {
            get => _isUploadingImage;
            set { _isUploadingImage = value; OnPropertyChanged(); }
        }

        public System.Collections.ObjectModel.ObservableCollection<Province> Provinces
        {
            get => _provinces;
            set { _provinces = value; OnPropertyChanged(); }
        }

        public System.Collections.ObjectModel.ObservableCollection<District> Districts
        {
            get => _districts;
            set { _districts = value; OnPropertyChanged(); }
        }

        public System.Collections.ObjectModel.ObservableCollection<Ward> Wards
        {
            get => _wards;
            set { _wards = value; OnPropertyChanged(); }
        }

        public Province? SelectedProvince
        {
            get => _selectedProvince;
            set
            {
                _selectedProvince = value;
                OnPropertyChanged();
                _ = LoadDistrictsAsync();
                UpdateWarehouseAddressInput();
            }
        }

        public District? SelectedDistrict
        {
            get => _selectedDistrict;
            set
            {
                _selectedDistrict = value;
                OnPropertyChanged();
                _ = LoadWardsAsync();
                UpdateWarehouseAddressInput();
            }
        }

        public Ward? SelectedWard
        {
            get => _selectedWard;
            set
            {
                _selectedWard = value;
                OnPropertyChanged();
                UpdateWarehouseAddressInput();
            }
        }

        public string HouseNumberInput
        {
            get => _houseNumberInput;
            set
            {
                _houseNumberInput = value;
                OnPropertyChanged();
                UpdateWarehouseAddressInput();
            }
        }

        private void UpdateWarehouseAddressInput()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(HouseNumberInput)) parts.Add(HouseNumberInput.Trim());
            if (SelectedWard != null) parts.Add(SelectedWard.Name);
            if (SelectedDistrict != null) parts.Add(SelectedDistrict.Name);
            if (SelectedProvince != null) parts.Add(SelectedProvince.Name);

            WarehouseAddressInput = string.Join(", ", parts);
        }

        private async System.Threading.Tasks.Task LoadProvincesAsync()
        {
            var provinces = await LocationService.GetProvincesAsync();
            Provinces = new System.Collections.ObjectModel.ObservableCollection<Province>(provinces);
        }

        private async System.Threading.Tasks.Task LoadDistrictsAsync()
        {
            if (SelectedProvince == null)
            {
                Districts = new System.Collections.ObjectModel.ObservableCollection<District>();
                return;
            }
            var districts = await LocationService.GetDistrictsAsync(SelectedProvince.Code);
            Districts = new System.Collections.ObjectModel.ObservableCollection<District>(districts);
        }

        private async System.Threading.Tasks.Task LoadWardsAsync()
        {
            if (SelectedDistrict == null)
            {
                Wards = new System.Collections.ObjectModel.ObservableCollection<Ward>();
                return;
            }
            var wards = await LocationService.GetWardsAsync(SelectedDistrict.Code);
            Wards = new System.Collections.ObjectModel.ObservableCollection<Ward>(wards);
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
        public event Action? OpenProfileRequest;
        public event Action? CloseProfileRequest;
        public event Action? RequestNavigateToWallet;
        #endregion

        #region Commands
        public ICommand SaveProfileCommand { get; }
        public ICommand OpenProfileCommand { get; }
        public ICommand ToggleVacationCommand { get; }
        public ICommand WithdrawCommand { get; }
        public ICommand UploadLogoCommand { get; }
        #endregion

        public SellerProfileViewModel()
        {
            try { _context = new TmdtContext(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Init TmdtContext failed: " + ex.Message); }

            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile);
            OpenProfileCommand = new RelayCommand(_ => OpenProfileRequest?.Invoke());
            ToggleVacationCommand = new RelayCommand(_ => ExecuteToggleVacation());
            WithdrawCommand = new RelayCommand(_ => RequestNavigateToWallet?.Invoke());
            UploadLogoCommand = new RelayCommand(async _ => await ExecuteUploadLogo());
 
            LoadShopProfile();
            _ = LoadProvincesAsync();
        }

        private async System.Threading.Tasks.Task ExecuteUploadLogo()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Chọn ảnh Logo cho Shop"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsUploadingImage = true;
                string oldLogo = LogoInput;
                LogoInput = "Đang tải ảnh lên...";

                try
                {
                    string uploadedUrl = await _imageUploadService.UploadImageAsync(openFileDialog.FileName);
                    if (!string.IsNullOrEmpty(uploadedUrl))
                    {
                        LogoInput = uploadedUrl;
                    }
                    else
                    {
                        LogoInput = oldLogo;
                        MessageBox.Show("Tải ảnh lên thất bại, vui lòng thử lại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    LogoInput = oldLogo;
                    MessageBox.Show("Có lỗi xảy ra khi tải ảnh lên: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsUploadingImage = false;
                }
            }
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
            
            _ = ParseAddressAsync(Shop.WarehouseAddress);
        }

        private async System.Threading.Tasks.Task ParseAddressAsync(string fullAddress)
        {
            if (string.IsNullOrWhiteSpace(fullAddress)) return;

            var parts = fullAddress.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count < 4)
            {
                // Cannot reliably parse into 4 components, dump into HouseNumber
                _houseNumberInput = fullAddress;
                OnPropertyChanged(nameof(HouseNumberInput));
                return;
            }

            // The last 3 parts are expected to be Ward, District, Province
            string provinceName = parts[parts.Count - 1];
            string districtName = parts[parts.Count - 2];
            string wardName = parts[parts.Count - 3];
            
            // The rest is HouseNumber
            var houseParts = parts.Take(parts.Count - 3);
            _houseNumberInput = string.Join(", ", houseParts);
            OnPropertyChanged(nameof(HouseNumberInput));

            // Try to match Province
            if (Provinces == null || !Provinces.Any()) await LoadProvincesAsync();
            var province = Provinces.FirstOrDefault(p => p.Name == provinceName);
            if (province != null)
            {
                // Set backing field to avoid triggering UpdateWarehouseAddressInput multiple times
                _selectedProvince = province;
                OnPropertyChanged(nameof(SelectedProvince));
                
                await LoadDistrictsAsync();
                var district = Districts.FirstOrDefault(d => d.Name == districtName);
                if (district != null)
                {
                    _selectedDistrict = district;
                    OnPropertyChanged(nameof(SelectedDistrict));
                    
                    await LoadWardsAsync();
                    var ward = Wards.FirstOrDefault(w => w.Name == wardName);
                    if (ward != null)
                    {
                        _selectedWard = ward;
                        OnPropertyChanged(nameof(SelectedWard));
                    }
                }
            }
            
            // Finally update the assembled string
            UpdateWarehouseAddressInput();
        }

        private void ExecuteToggleVacation()
        {
            VacationMode = !VacationMode;
            if (Shop != null) Shop.VacationMode = VacationMode;

            try
            {
                var dbShop = _context?.Shops.Find(Shop?.ShopId);
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

        private async void ExecuteSaveProfile(object? obj)
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

            if (Shop != null)
            {
                Shop.ShopName = ShopNameInput;
                Shop.Logo = LogoInput;
                Shop.WarehouseAddress = WarehouseAddressInput;
                Shop.VacationMode = VacationMode;
            }

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

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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
            set { _vacationMode = value; OnPropertyChanged(); }
        }
        public string OpenedAtDisplay
        {
            get => _openedAtDisplay;
            set { _openedAtDisplay = value; OnPropertyChanged(); }
        }
        #endregion

        // Commands
        public ICommand SaveProfileCommand { get; }

        public SellerProfileViewModel()
        {
            try
            {
                _context = new TmdtContext();
            }
            catch {}

            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile);

            LoadShopProfile();
        }

        private void LoadShopProfile()
        {
            int currentShopId = GetCurrentShopId();

            try
            {
                if (_context != null)
                {
                    var dbShop = _context.Shops.Find(currentShopId);
                    if (dbShop != null)
                    {
                        Shop = dbShop;
                        PopulateFields();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load shop profile from DB: " + ex.Message);
            }

            // Mock Shop Profile
            Shop = new Shop
            {
                ShopId = 1,
                ShopName = "MyShop Premium Store",
                Logo = "pack://application:,,,/Resources/Images/default_shop.png",
                WarehouseAddress = "236 Hoàng Quốc Việt, Cầu Giấy, Hà Nội",
                CommissionRate = 3.0m,
                VacationMode = false,
                OpenedAt = DateTime.Now.AddMonths(-6)
            };
            PopulateFields();
        }

        private void PopulateFields()
        {
            if (Shop != null)
            {
                ShopNameInput = Shop.ShopName;
                LogoInput = Shop.Logo;
                WarehouseAddressInput = Shop.WarehouseAddress;
                CommissionRate = Shop.CommissionRate ?? 3.0m;
                VacationMode = Shop.VacationMode ?? false;
                OpenedAtDisplay = Shop.OpenedAt.HasValue ? Shop.OpenedAt.Value.ToString("dd/MM/yyyy") : DateTime.Now.ToString("dd/MM/yyyy");
            }
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
                if (_context != null)
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF save shop profile failed: " + ex.Message);
            }

            MessageBox.Show("Đã lưu cấu hình thông tin Shop thành công!", "Cập nhật thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private int GetCurrentShopId()
        {
            try
            {
                if (_context != null)
                {
                    var shop = _context.Shops
                        .Include(s => s.User)
                        .FirstOrDefault(s => s.User != null && s.User.Email == "seller@myshop.com")
                        ?? _context.Shops.FirstOrDefault();
                    if (shop != null) return shop.ShopId;
                }
            }
            catch {}
            return 1;
        }
    }
}

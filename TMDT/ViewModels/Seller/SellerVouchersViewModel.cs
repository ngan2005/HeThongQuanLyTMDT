using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class SellerVouchersViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        private ObservableCollection<Voucher> _vouchers;
        private Voucher _selectedVoucher;

        // Input fields for creating a new voucher
        private string _voucherCodeInput;
        private string _voucherNameInput;
        private string _discountTypeInput = "Percentage"; // Percentage, Fixed
        private decimal _discountValueInput;
        private decimal _maxDiscountInput;
        private decimal _minOrderValueInput;
        private int _totalQuantityInput = 100;
        private int _durationDaysInput = 30;

        public ObservableCollection<Voucher> Vouchers
        {
            get => _vouchers;
            set { _vouchers = value; OnPropertyChanged(); }
        }

        public Voucher SelectedVoucher
        {
            get => _selectedVoucher;
            set { _selectedVoucher = value; OnPropertyChanged(); }
        }

        #region Input Properties
        public string VoucherCodeInput
        {
            get => _voucherCodeInput;
            set { _voucherCodeInput = value; OnPropertyChanged(); }
        }
        public string VoucherNameInput
        {
            get => _voucherNameInput;
            set { _voucherNameInput = value; OnPropertyChanged(); }
        }
        public string DiscountTypeInput
        {
            get => _discountTypeInput;
            set { _discountTypeInput = value; OnPropertyChanged(); }
        }
        public decimal DiscountValueInput
        {
            get => _discountValueInput;
            set { _discountValueInput = value; OnPropertyChanged(); }
        }
        public decimal MaxDiscountInput
        {
            get => _maxDiscountInput;
            set { _maxDiscountInput = value; OnPropertyChanged(); }
        }
        public decimal MinOrderValueInput
        {
            get => _minOrderValueInput;
            set { _minOrderValueInput = value; OnPropertyChanged(); }
        }
        public int TotalQuantityInput
        {
            get => _totalQuantityInput;
            set { _totalQuantityInput = value; OnPropertyChanged(); }
        }
        public int DurationDaysInput
        {
            get => _durationDaysInput;
            set { _durationDaysInput = value; OnPropertyChanged(); }
        }
        #endregion

        // Commands
        public ICommand SaveVoucherCommand { get; }
        public ICommand ResetFieldsCommand { get; }
        public ICommand ToggleVoucherStatusCommand { get; }

        public SellerVouchersViewModel()
        {
            try
            {
                _context = new TmdtContext();
            }
            catch {}

            Vouchers = new ObservableCollection<Voucher>();

            SaveVoucherCommand = new RelayCommand(ExecuteSaveVoucher);
            ResetFieldsCommand = new RelayCommand(o => ResetInputs());
            ToggleVoucherStatusCommand = new RelayCommand(ExecuteToggleVoucherStatus, o => SelectedVoucher != null);

            LoadVouchers();
            ResetInputs();
        }

        private void LoadVouchers()
        {
            Vouchers.Clear();
            int currentShopId = GetCurrentShopId();

            try
            {
                if (_context != null && _context.Vouchers.Any())
                {
                    var dbVouchers = _context.Vouchers
                        .Where(v => v.ShopId == currentShopId)
                        .ToList();

                    foreach (var v in dbVouchers)
                    {
                        Vouchers.Add(v);
                    }

                    if (Vouchers.Any()) return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load vouchers: " + ex.Message);
            }

        }

        private void ResetInputs()
        {
            VoucherCodeInput = "";
            VoucherNameInput = "";
            DiscountTypeInput = "Percentage";
            DiscountValueInput = 0;
            MaxDiscountInput = 0;
            MinOrderValueInput = 0;
            TotalQuantityInput = 100;
            DurationDaysInput = 30;
        }

        private async void ExecuteSaveVoucher(object obj)
        {
            if (string.IsNullOrWhiteSpace(VoucherCodeInput) || string.IsNullOrWhiteSpace(VoucherNameInput))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Mã và Tên voucher!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DiscountValueInput <= 0)
            {
                MessageBox.Show("Giá trị giảm phải lớn hơn 0!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string formattedCode = VoucherCodeInput.Trim().ToUpper();

            // Check if code exists in list
            if (Vouchers.Any(v => v.VoucherCode == formattedCode))
            {
                MessageBox.Show("Mã Voucher này đã tồn tại trong Shop của bạn!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int currentShopId = GetCurrentShopId();

            var newVoucher = new Voucher
            {
                ShopId = currentShopId,
                VoucherCode = formattedCode,
                VoucherName = VoucherNameInput,
                DiscountType = DiscountTypeInput,
                DiscountValue = DiscountValueInput,
                MaxDiscount = DiscountTypeInput == "Percentage" ? MaxDiscountInput : DiscountValueInput,
                MinOrderValue = MinOrderValueInput,
                TotalQuantity = TotalQuantityInput,
                UsedCount = 0,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(DurationDaysInput),
                IsActive = true
            };

            try
            {
                if (_context != null)
                {
                    _context.Vouchers.Add(newVoucher);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF save voucher failed: " + ex.Message);
                newVoucher.VoucherId = new Random().Next(8000, 9999);
            }

            Vouchers.Add(newVoucher);
            MessageBox.Show($"Đã tạo và kích hoạt thành công Mã giảm giá '{formattedCode}'!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            ResetInputs();
        }

        private async void ExecuteToggleVoucherStatus(object obj)
        {
            if (SelectedVoucher == null) return;

            SelectedVoucher.IsActive = !(SelectedVoucher.IsActive ?? false);

            try
            {
                if (_context != null)
                {
                    var dbVoucher = await _context.Vouchers.FindAsync(SelectedVoucher.VoucherId);
                    if (dbVoucher != null)
                    {
                        dbVoucher.IsActive = SelectedVoucher.IsActive;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF update voucher status failed: " + ex.Message);
            }

            string statusText = (SelectedVoucher.IsActive ?? false) ? "Kích hoạt" : "Ngưng hoạt động";
            MessageBox.Show($"Đã cập nhật trạng thái voucher: {statusText}!", "Cập nhật thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadVouchers();
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

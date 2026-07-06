using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;
using TMDT.Views.Components;

namespace TMDT.ViewModels.Seller
{
    public class SellerWalletViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        private decimal _walletBalance;
        private decimal _pendingPayouts;
        private decimal _withdrawnYTD;
        private bool _isWithdrawDialogOpen;
        private bool _isVNPayMethod = true;
        private ObservableCollection<WithdrawRequest> _withdrawRequests;

        // Input fields for a new request
        private decimal _amountInput;
        private string _bankNameInput = "Vietcombank";
        private string _accountNumberInput;
        private string _vnPayAccountInput = "";

        public decimal WalletBalance
        {
            get => _walletBalance;
            set { _walletBalance = value; OnPropertyChanged(); }
        }

        public decimal PendingPayouts
        {
            get => _pendingPayouts;
            set { _pendingPayouts = value; OnPropertyChanged(); }
        }

        public decimal WithdrawnYTD
        {
            get => _withdrawnYTD;
            set { _withdrawnYTD = value; OnPropertyChanged(); }
        }

        public bool IsWithdrawDialogOpen
        {
            get => _isWithdrawDialogOpen;
            set { _isWithdrawDialogOpen = value; OnPropertyChanged(); }
        }

        public bool IsVNPayMethod
        {
            get => _isVNPayMethod;
            set { _isVNPayMethod = value; OnPropertyChanged(); }
        }

        public ObservableCollection<WithdrawRequest> WithdrawRequests
        {
            get => _withdrawRequests;
            set { _withdrawRequests = value; OnPropertyChanged(); }
        }

        #region Input Properties
        public decimal AmountInput
        {
            get => _amountInput;
            set { _amountInput = value; OnPropertyChanged(); }
        }
        public string BankNameInput
        {
            get => _bankNameInput;
            set { _bankNameInput = value; OnPropertyChanged(); }
        }
        public string AccountNumberInput
        {
            get => _accountNumberInput;
            set { _accountNumberInput = value; OnPropertyChanged(); }
        }
        public string VNPayAccountInput
        {
            get => _vnPayAccountInput;
            set { _vnPayAccountInput = value; OnPropertyChanged(); }
        }
        #endregion

        // Commands
        public ICommand SendWithdrawRequestCommand { get; }
        public ICommand ResetFieldsCommand { get; }
        public ICommand OpenDialogCommand { get; }
        public ICommand CloseDialogCommand { get; }

        public SellerWalletViewModel()
        {
            try
            {
                _context = new TmdtContext();
            }
            catch {}

            WithdrawRequests = new ObservableCollection<WithdrawRequest>();

            SendWithdrawRequestCommand = new RelayCommand(ExecuteSendWithdrawRequest);
            ResetFieldsCommand = new RelayCommand(o => ResetInputs());
            OpenDialogCommand = new RelayCommand(o => IsWithdrawDialogOpen = true);
            CloseDialogCommand = new RelayCommand(o => { IsWithdrawDialogOpen = false; ResetInputs(); });

            LoadWalletInfo();
            ResetInputs();
        }

        private void LoadWalletInfo()
        {
            int currentShopId = GetCurrentShopId();

            try
            {
                if (_context != null)
                {
                    var shop = _context.Shops.Find(currentShopId);
                    if (shop != null)
                    {
                        WalletBalance = shop.WalletBalance ?? 0;
                    }

                    if (_context.WithdrawRequests.Any())
                    {
                        var dbRequests = _context.WithdrawRequests
                            .Where(w => w.ShopId == currentShopId)
                            .OrderByDescending(w => w.RequestedAt)
                            .ToList();

                        WithdrawRequests.Clear();
                        PendingPayouts = 0;
                        WithdrawnYTD = 0;

                        foreach (var req in dbRequests)
                        {
                            WithdrawRequests.Add(req);
                            if (req.Status == "Pending") PendingPayouts += req.Amount ?? 0;
                            if (req.Status == "Approved") WithdrawnYTD += req.Amount ?? 0;
                        }

                        if (WithdrawRequests.Any()) return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load wallet from DB: " + ex.Message);
            }

        }

        private void ResetInputs()
        {
            AmountInput = 0;
            AccountNumberInput = "";
            VNPayAccountInput = "";
            IsVNPayMethod = true;
        }

        private async void ExecuteSendWithdrawRequest(object obj)
        {
            if (AmountInput <= 0)
            {
                MessageBox.Show("Vui lòng nhập số tiền hợp lệ để rút!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AmountInput > WalletBalance)
            {
                MessageBox.Show("Số dư ví không đủ để rút số tiền này!", "Lỗi số dư", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsVNPayMethod)
            {
                // --- Rút qua VNPay: tức thì ---
                if (string.IsNullOrWhiteSpace(VNPayAccountInput))
                {
                    MessageBox.Show("Vui lòng nhập số điện thoại / tài khoản VNPay!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Mở cửa sổ mock VNPay
                var vnpayWin = new VNPayWithdrawMockWindow(AmountInput, VNPayAccountInput);
                bool? result = vnpayWin.ShowDialog();

                if (result != true) return; // Người dùng hủy

                // Xác nhận thành công → trừ ví ngay, tạo lịch sử Approved
                int currentShopId = GetCurrentShopId();
                var newReq = new WithdrawRequest
                {
                    ShopId = currentShopId,
                    Amount = AmountInput,
                    BankName = "VNPay",
                    AccountNumber = VNPayAccountInput.Trim(),
                    Status = "Approved",  // Tức thì duyệt
                    RequestedAt = DateTime.Now
                };

                try
                {
                    if (_context != null)
                    {
                        var shop = await _context.Shops.FindAsync(currentShopId);
                        if (shop != null)
                        {
                            decimal realBalance = shop.WalletBalance ?? 0;
                            if (realBalance < AmountInput)
                            {
                                MessageBox.Show("Số dư ví thực tế không đủ.", "Lỗi số dư", MessageBoxButton.OK, MessageBoxImage.Warning);
                                WalletBalance = realBalance;
                                return;
                            }
                            shop.WalletBalance = realBalance - AmountInput;
                            WalletBalance = shop.WalletBalance.Value;
                        }
                        _context.WithdrawRequests.Add(newReq);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("VNPay withdraw failed: " + ex.Message);
                }

                WithdrawRequests.Insert(0, newReq);
                WithdrawnYTD += AmountInput;  // VNPay duyệt ngay
                IsWithdrawDialogOpen = false;
                MessageBox.Show($"Rút tiền qua VNPay thành công!\nSố tiền {AmountInput:N0} đ đã được chuyển vào {VNPayAccountInput}.\nMã GD: {vnpayWin.TransactionCode}",
                    "Rút tiền thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetInputs();
            }
            else
            {
                // --- Rút qua Ngân hàng: gửi yêu cầu chờ Admin duyệt ---
                if (string.IsNullOrWhiteSpace(AccountNumberInput))
                {
                    MessageBox.Show("Vui lòng nhập số tài khoản ngân hàng!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int currentShopId = GetCurrentShopId();
                var newReq = new WithdrawRequest
                {
                    ShopId = currentShopId,
                    Amount = AmountInput,
                    BankName = BankNameInput,
                    AccountNumber = AccountNumberInput.Trim(),
                    Status = "Pending",
                    RequestedAt = DateTime.Now
                };

                try
                {
                    if (_context != null)
                    {
                        var shop = await _context.Shops.FindAsync(currentShopId);
                        if (shop != null)
                        {
                            decimal realBalance = shop.WalletBalance ?? 0;
                            if (realBalance < AmountInput)
                            {
                                MessageBox.Show("Số dư ví thực tế không đủ để rút số tiền này.", "Lỗi số dư", MessageBoxButton.OK, MessageBoxImage.Warning);
                                WalletBalance = realBalance;
                                return;
                            }
                            shop.WalletBalance = realBalance - AmountInput;
                            WalletBalance = shop.WalletBalance.Value;
                        }
                        _context.WithdrawRequests.Add(newReq);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Bank withdraw request failed: " + ex.Message);
                }

                WithdrawRequests.Insert(0, newReq);
                PendingPayouts += AmountInput;
                IsWithdrawDialogOpen = false;
                MessageBox.Show("Yêu cầu rút tiền đã được gửi!\nSố dư ví tạm khấu trừ, đang chờ Admin phê duyệt giải ngân.",
                    "Gửi yêu cầu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetInputs();
            }
        }

        private int GetCurrentShopId()
        {
            try
            {
                if (SessionManager.CurrentUser == null) return 0;

                var shop = _context.Shops
                    .FirstOrDefault(s => s.UserId == SessionManager.CurrentUser.UserId);
                if (shop != null) return shop.ShopId;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("GetCurrentShopId failed: " + ex.Message); }
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}

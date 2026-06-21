using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerPromotionsViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;

        public ObservableCollection<Voucher> Vouchers { get; } = new();

        public ICommand CopyVoucherCommand { get; }

        public BuyerPromotionsViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;

            CopyVoucherCommand = new RelayCommand(ExecuteCopyVoucher);

            LoadVouchers();
        }

        private async void LoadVouchers()
        {
            try
            {
                using var context = new TmdtContext();
                var activeVouchers = await context.Vouchers.AsNoTracking()
                    .Include(v => v.Shop)
                    .Where(v => v.IsActive == true 
                             && (v.StartDate == null || v.StartDate <= DateTime.Now)
                             && (v.EndDate == null || v.EndDate >= DateTime.Now))
                    .OrderBy(v => v.EndDate)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Vouchers.Clear();
                    foreach (var v in activeVouchers)
                    {
                        Vouchers.Add(v);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load Vouchers failed: " + ex.Message);
            }
        }

        private void ExecuteCopyVoucher(object? parameter)
        {
            if (parameter is string voucherCode && !string.IsNullOrWhiteSpace(voucherCode))
            {
                try
                {
                    Clipboard.SetText(voucherCode);
                    MessageBox.Show($"Đã sao chép mã: {voucherCode}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Copy failed: " + ex.Message);
                }
            }
        }
    }
}

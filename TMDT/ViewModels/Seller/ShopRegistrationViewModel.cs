using System;
using System.Windows;
using System.Windows.Input;
using TMDT.Services;
using TMDT.Services.Interfaces;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class ShopRegistrationViewModel : ViewModelBase
    {
        private string _shopName = "";
        public string ShopName
        {
            get => _shopName;
            set => _shopName = value;
        }

        private string _warehouseAddress = "";
        public string WarehouseAddress
        {
            get => _warehouseAddress;
            set => _warehouseAddress = value;
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => _isLoading = value;
        }

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool> RequestClose;

        public ShopRegistrationViewModel()
        {
            SubmitCommand = new RelayCommand(async _ => await ExecuteSubmit());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }

        private async System.Threading.Tasks.Task ExecuteSubmit()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(ShopName))
            {
                ErrorMessage = "Vui lòng nhập tên cửa hàng.";
                return;
            }

            if (ShopName.Trim().Length < 4)
            {
                ErrorMessage = "Tên cửa hàng phải có ít nhất 4 ký tự.";
                return;
            }

            if (string.IsNullOrWhiteSpace(WarehouseAddress))
            {
                ErrorMessage = "Vui lòng nhập địa chỉ kho hàng.";
                return;
            }

            if (WarehouseAddress.Trim().Length < 10)
            {
                ErrorMessage = "Địa chỉ kho hàng phải có ít nhất 10 ký tự.";
                return;
            }

            IsLoading = true;

            try
            {
                var service = new ShopService(new Models.TmdtContext());
                var result = await service.RegisterShopAsync(new ShopRegisterRequest
                {
                    UserId = Utilities.SessionManager.CurrentUser.UserId,
                    ShopName = ShopName.Trim(),
                    WarehouseAddress = WarehouseAddress.Trim()
                });

                IsLoading = false;

                if (result == null)
                {
                    ErrorMessage = "Tên cửa hàng đã tồn tại hoặc bạn đã đăng ký shop rồi.";
                    return;
                }

                MessageBox.Show(
                    "Yêu cầu đăng ký shop của bạn đã được gửi!\n\nVui lòng chờ Admin phê duyệt.",
                    "Đăng ký thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                IsLoading = false;
                ErrorMessage = $"Lỗi: {ex.Message}";
            }
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class CartViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private ObservableCollection<TMDT.Services.CartItem> _items;
        private decimal _totalPrice;
        private decimal _shippingFee = 25000m;
        private decimal _totalPayment;
        private int _selectedPayment = 0;
        private Address? _selectedAddress;

        public ObservableCollection<TMDT.Services.CartItem> Items
        {
            get => _items;
            set { SetProperty(ref _items, value); }
        }

        public decimal TotalPrice
        {
            get => _totalPrice;
            set { SetProperty(ref _totalPrice, value); }
        }

        public decimal ShippingFee
        {
            get => _shippingFee;
            set { SetProperty(ref _shippingFee, value); }
        }

        public decimal TotalPayment
        {
            get => _totalPayment;
            set { SetProperty(ref _totalPayment, value); }
        }

        public int SelectedPayment
        {
            get => _selectedPayment;
            set { SetProperty(ref _selectedPayment, value); }
        }

        public Address? SelectedAddress
        {
            get => _selectedAddress;
            set { SetProperty(ref _selectedAddress, value); }
        }

        public bool IsEmpty => Items.Count == 0;

        public ICommand RemoveItemCommand { get; }
        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }
        public ICommand PlaceOrderCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ContinueShoppingCommand { get; }
        public ICommand ClearCartCommand { get; }

        public CartViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;
            _items = CartService.Instance.Items;

            RemoveItemCommand = new RelayCommand(p => ExecuteRemove(p as TMDT.Services.CartItem));
            IncreaseCommand = new RelayCommand(p => ExecuteIncrease(p as TMDT.Services.CartItem));
            DecreaseCommand = new RelayCommand(p => ExecuteDecrease(p as TMDT.Services.CartItem));
            PlaceOrderCommand = new RelayCommand(_ => ExecutePlaceOrder(), _ => !IsEmpty && SessionManager.IsLoggedIn);
            BackCommand = new RelayCommand(_ => _mainVm.NavigateHome());
            ContinueShoppingCommand = new RelayCommand(_ => _mainVm.NavigateHome());
            ClearCartCommand = new RelayCommand(_ => ExecuteClear());

            LoadAddress();
            Recalculate();
        }

        private void LoadAddress()
        {
            if (!SessionManager.IsLoggedIn) return;
            try
            {
                using var context = new TmdtContext();
                var address = context.Addresses
                    .FirstOrDefault(a => a.UserId == SessionManager.CurrentUser!.UserId && a.IsDefault == true)
                    ?? context.Addresses.FirstOrDefault(a => a.UserId == SessionManager.CurrentUser!.UserId);
                SelectedAddress = address;
            }
            catch { }
        }

        public void Recalculate()
        {
            TotalPrice = CartService.Instance.TotalPrice;
            TotalPayment = TotalPrice + ShippingFee;
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void ExecuteRemove(TMDT.Services.CartItem? item)
        {
            if (item == null) return;
            var result = MessageBox.Show($"Xóa '{item.ProductName}' khỏi giỏ hàng?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            CartService.Instance.RemoveProduct(item.ProductId);
            Recalculate();
        }

        private void ExecuteIncrease(TMDT.Services.CartItem? item)
        {
            if (item == null) return;
            CartService.Instance.UpdateQuantity(item.ProductId, item.Quantity + 1);
            Recalculate();
        }

        private void ExecuteDecrease(TMDT.Services.CartItem? item)
        {
            if (item == null || item.Quantity <= 1) return;
            CartService.Instance.UpdateQuantity(item.ProductId, item.Quantity - 1);
            Recalculate();
        }

        private async void ExecutePlaceOrder()
        {
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập để đặt hàng.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Items.Count == 0)
            {
                MessageBox.Show("Giỏ hàng trống.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedAddress == null)
            {
                MessageBox.Show("Bạn chưa có địa chỉ nhận hàng.\nVui lòng vào mục 'Tài Khoản' -> 'Địa Chỉ' để thêm và chọn địa chỉ mặc định trước khi đặt hàng.", "Thiếu địa chỉ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string paymentMethod = SelectedPayment switch
            {
                0 => "COD",
                1 => "VNPay",
                2 => "MoMo",
                _ => "COD"
            };

            var result = MessageBox.Show(
                $"Xác nhận đặt hàng?\n\nTổng tiền: {TotalPayment:N0} đ\nThanh toán: {paymentMethod}",
                "Xác nhận đặt hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                foreach (var group in Items.GroupBy(i => i.ShopId))
                {
                    var cartItems = group.Select(i => new DTOs.CartOrderItem
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.Price,
                        TotalPrice = i.LineTotal
                    }).ToList();

                    var order = await OrderService.Instance.CreateOrderFromCartAsync(
                        SessionManager.CurrentUser!.UserId,
                        group.Key,
                        SelectedAddress?.AddressId,
                        null,
                        paymentMethod,
                        ShippingFee,
                        cartItems);

                    if (paymentMethod == "VNPay" && order != null)
                    {
                        string vnpUrl = VNPayService.CreatePaymentUrl(order);
                        var vnPayWindow = new TMDT.Views.Components.VNPayWindow(vnpUrl);
                        bool? success = vnPayWindow.ShowDialog();

                        if (success == true)
                        {
                            await OrderService.Instance.UpdatePaymentSuccessAsync(order.OrderId, vnPayWindow.TransactionCode);
                        }
                        else
                        {
                            MessageBox.Show($"Thanh toán VNPay cho đơn hàng {order.OrderCode} thất bại hoặc đã bị hủy.\nĐơn hàng vẫn được tạo nhưng ở trạng thái Chưa thanh toán.", "Lưu ý", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }

                CartService.Instance.Clear();
                Recalculate();

                MessageBox.Show("Đặt hàng thành công! Cảm ơn bạn đã mua sắm.",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                _mainVm.NavigateOrders();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi đặt hàng",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đặt hàng: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteClear()
        {
            var result = MessageBox.Show("Xóa tất cả sản phẩm trong giỏ hàng?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            CartService.Instance.Clear();
            Recalculate();
        }
    }
}

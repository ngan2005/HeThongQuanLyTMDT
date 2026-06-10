using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class CartViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private ObservableCollection<CartItem> _items;
        private decimal _totalPrice;
        private decimal _shippingFee = 25000m;
        private decimal _totalPayment;
        private int _selectedPayment = 0;
        private Address? _selectedAddress;

        public ObservableCollection<CartItem> Items
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

            RemoveItemCommand = new RelayCommand(p => ExecuteRemove(p as CartItem));
            IncreaseCommand = new RelayCommand(p => ExecuteIncrease(p as CartItem));
            DecreaseCommand = new RelayCommand(p => ExecuteDecrease(p as CartItem));
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

        private void ExecuteRemove(CartItem? item)
        {
            if (item == null) return;
            var result = MessageBox.Show($"Xóa '{item.ProductName}' khỏi giỏ hàng?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            CartService.Instance.RemoveProduct(item.ProductId);
            Recalculate();
        }

        private void ExecuteIncrease(CartItem? item)
        {
            if (item == null) return;
            CartService.Instance.UpdateQuantity(item.ProductId, item.Quantity + 1);
            Recalculate();
        }

        private void ExecuteDecrease(CartItem? item)
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
                using var context = new TmdtContext();
                await using var transaction = await context.Database.BeginTransactionAsync();

                foreach (var group in Items.GroupBy(i => i.ShopId))
                {
                    var order = new Order
                    {
                        OrderCode = "ORD-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        BuyerId = SessionManager.CurrentUser!.UserId,
                        ShopId = group.Key,
                        SubTotal = group.Sum(i => i.LineTotal),
                        ShippingFee = ShippingFee,
                        Discount = 0,
                        TotalAmount = group.Sum(i => i.LineTotal) + ShippingFee,
                        PlatformFee = (group.Sum(i => i.LineTotal) + ShippingFee) * 0.05m,
                        PaymentMethod = paymentMethod,
                        OrderStatus = "Pending",
                        OrderDate = DateTime.Now,
                        AddressId = SelectedAddress?.AddressId
                    };

                    context.Orders.Add(order);
                    await context.SaveChangesAsync();

                    foreach (var item in group)
                    {
                        var detail = new OrderDetail
                        {
                            OrderId = order.OrderId,
                            ProductId = item.ProductId,
                            ProductNameSnapshot = item.ProductName,
                            Quantity = item.Quantity,
                            UnitPrice = item.Price,
                            TotalPrice = item.LineTotal
                        };
                        context.OrderDetails.Add(detail);

                        var product = await context.Products.FindAsync(item.ProductId);
                        if (product != null)
                            product.StockQuantity = (product.StockQuantity ?? 0) - item.Quantity;
                    }

                    await context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                CartService.Instance.Clear();
                Recalculate();

                MessageBox.Show("Đặt hàng thành công! Cảm ơn bạn đã mua sắm.",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                _mainVm.NavigateOrders();
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

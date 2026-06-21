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
    public class BuyerOrdersViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private ObservableCollection<Order> _orders = new();
        private Order? _selectedOrder;
        private string _statusFilter = "Tất cả";
        private bool _isLoading;

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set { SetProperty(ref _orders, value); }
        }

        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                SetProperty(ref _selectedOrder, value);
                if (value != null) _ = LoadOrderDetailsAsync(value);
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); _ = LoadOrdersAsync(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { SetProperty(ref _isLoading, value); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand ReceiveOrderCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand SetFilterCommand { get; }

        public BuyerOrdersViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;

            RefreshCommand = new RelayCommand(_ => _ = LoadOrdersAsync());
            CancelOrderCommand = new RelayCommand(o => ExecuteCancelOrder(o as Order), o => CanCancelOrder(o as Order));
            ReceiveOrderCommand = new RelayCommand(o => ExecuteReceiveOrder(o as Order), o => CanReceiveOrder(o as Order));
            BackCommand = new RelayCommand(_ => _mainVm.NavigateHome());
            SetFilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "Tất cả");

            _ = LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            if (!SessionManager.IsLoggedIn) return;

            IsLoading = true;
            try
            {
                var list = await OrderService.Instance.GetBuyerOrdersAsync(
                    SessionManager.CurrentUser!.UserId, StatusFilter);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Orders.Clear();
                    foreach (var order in list)
                        Orders.Add(order);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load buyer orders failed: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadOrderDetailsAsync(Order order)
        {
            if (order?.OrderId == null) return;
            try
            {
                var refreshed = await OrderService.Instance.GetOrderByIdAsync(order.OrderId);
                if (refreshed == null) return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    order.OrderDetails.Clear();
                    foreach (var d in refreshed.OrderDetails)
                        order.OrderDetails.Add(d);
                });
            }
            catch { }
        }

        private bool CanCancelOrder(Order? order)
        {
            return order != null &&
                   (order.OrderStatus == "Pending" || order.OrderStatus == "Chờ duyệt");
        }

        private async void ExecuteCancelOrder(Order? order)
        {
            if (order == null) return;

            var result = MessageBox.Show(
                $"Hủy đơn hàng '{order.OrderCode}'?\nSản phẩm sẽ được trả lại kho.",
                "Xác nhận hủy", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await OrderService.Instance.CancelOrderAsync(order.OrderId);
                order.OrderStatus = "Cancelled";
                OnPropertyChanged(nameof(Orders));

                MessageBox.Show("Đơn hàng đã được hủy.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Không thể hủy",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi hủy đơn: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanReceiveOrder(Order? order)
        {
            return order != null && order.OrderStatus == "Shipping";
        }

        private async void ExecuteReceiveOrder(Order? order)
        {
            if (order == null) return;

            var result = MessageBox.Show($"Xác nhận bạn đã nhận được đơn hàng '{order.OrderCode}' và hài lòng với sản phẩm?\nTiền sẽ được chuyển cho người bán.",
                                         "Xác nhận nhận hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await OrderService.Instance.ReceiveOrderAsync(order.OrderId);
                order.OrderStatus = "Completed";
                OnPropertyChanged(nameof(Orders));
                _ = LoadOrdersAsync();

                MessageBox.Show("Cảm ơn bạn đã mua sắm! Đơn hàng đã hoàn thành.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật đơn hàng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class SellerOrdersViewModel : ViewModelBase
    {
        // Removed long-lived _context for async safety
        private ObservableCollection<Order> _orders;
        private Order _selectedOrder;
        private string _statusFilter = "All"; // All, Pending, Shipping, Completed, Cancelled

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set { _orders = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> AvailableShippingProviders { get; } = new ObservableCollection<string>
        {
            "Shopee Express",
            "Giao Hàng Tiết Kiệm",
            "Viettel Post",
            "Giao Hàng Nhanh",
            "J&T Express"
        };

        private string _selectedShippingProvider = "Shopee Express";
        public string SelectedShippingProvider
        {
            get => _selectedShippingProvider;
            set { _selectedShippingProvider = value; OnPropertyChanged(); }
        }

        public Order SelectedOrder
        {
            get => _selectedOrder;
            set { _selectedOrder = value; OnPropertyChanged(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); _ = LoadOrdersAsync(); }
        }

        // Commands
        public ICommand ShipOrderCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand SetFilterCommand { get; }

        public SellerOrdersViewModel()
        {

            Orders = new ObservableCollection<Order>();

            ShipOrderCommand = new RelayCommand(ExecuteShipOrder, o => SelectedOrder != null && SelectedOrder.OrderStatus == "Pending");
            CancelOrderCommand = new RelayCommand(ExecuteCancelOrder, o => SelectedOrder != null && (SelectedOrder.OrderStatus == "Pending" || SelectedOrder.OrderStatus == "Shipping"));
            SetFilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");

            _ = LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            Orders.Clear();
            int currentShopId = await GetCurrentShopIdAsync();
            if (currentShopId == 0) return;

            try
            {
                var dbOrders = await OrderService.Instance.GetShopOrdersAsync(currentShopId, StatusFilter);
                foreach (var order in dbOrders)
                    Orders.Add(order);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load orders failed: " + ex.Message);
            }
        }

        private async void ExecuteShipOrder(object obj)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show($"Xác nhận chuẩn bị hàng và giao cho đơn vị vận chuyển [{SelectedShippingProvider}] cho đơn '{SelectedOrder.OrderCode}'?",
                                         "Xác nhận giao hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var success = await OrderService.Instance.ShipOrderAsync(SelectedOrder.OrderId, SelectedShippingProvider);
                if (!success)
                {
                    MessageBox.Show("Không thể xác nhận giao hàng. Đơn có thể đã bị hủy hoặc đang ở trạng thái không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show($"Đã xác nhận đơn hàng thành công! Mã vận đơn là: {SelectedOrder.TrackingCode}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật đơn hàng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteCancelOrder(object obj)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn HỦY đơn hàng '{SelectedOrder.OrderCode}'?",
                                         "Xác nhận hủy đơn", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await OrderService.Instance.CancelOrderAsync(SelectedOrder.OrderId);
                SelectedOrder.OrderStatus = "Cancelled";
                MessageBox.Show("Đơn hàng đã được hủy thành công.", "Đã hủy", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = LoadOrdersAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Không thể hủy", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi hủy đơn hàng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<int> GetCurrentShopIdAsync()
        {
            try
            {
                if (SessionManager.CurrentUser == null) return 0;

                using var ctx = new TmdtContext();
                var shop = await ctx.Shops
                    .FirstOrDefaultAsync(s => s.UserId == SessionManager.CurrentUser.UserId);
                if (shop != null) return shop.ShopId;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("GetCurrentShopId failed: " + ex.Message); }
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

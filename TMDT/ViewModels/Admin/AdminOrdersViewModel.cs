using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminOrdersViewModel : ViewModelBase
    {
        // Removed long-lived _context for async safety

        private ObservableCollection<Order> _filteredOrders;
        public ObservableCollection<Order> FilteredOrders
        {
            get => _filteredOrders;
            set { _filteredOrders = value; OnPropertyChanged(); }
        }

        private Order? _selectedOrder;
        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (_selectedOrder == value) return;
                _selectedOrder = value;
                OnPropertyChanged();
            }
        }

        private string _searchKeyword = "";
        public string SearchKeyword
        {
            get => _searchKeyword;
            set { _searchKeyword = value; OnPropertyChanged(); _ = FilterOrdersAsync(); }
        }

        private string _selectedStatus = "Tất cả";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); _ = FilterOrdersAsync(); }
        }

        public ObservableCollection<string> Statuses { get; } = new ObservableCollection<string>
        {
            "Tất cả", "Chờ xác nhận", "Đang xử lý", "Đang giao hàng", "Hoàn thành", "Đã hủy", "Hoàn tiền"
        };

        // Stats
        private int _totalOrders;
        private int _pendingOrders;
        private int _shippingOrders;
        private decimal _totalRevenue;

        public int TotalOrders { get => _totalOrders; set { _totalOrders = value; OnPropertyChanged(); } }
        public int PendingOrders { get => _pendingOrders; set { _pendingOrders = value; OnPropertyChanged(); } }
        public int ShippingOrders { get => _shippingOrders; set { _shippingOrders = value; OnPropertyChanged(); } }
        public decimal TotalRevenue { get => _totalRevenue; set { _totalRevenue = value; OnPropertyChanged(); } }

        // Events
        public event Action<Order?>? ShowDetailRequest;
        public event Action? HideDetailRequest;

        // Commands
        public ICommand CancelOrderCommand { get; }
        public ICommand RefundOrderCommand { get; }
        public ICommand ViewOrderCommand { get; }

        public AdminOrdersViewModel(string initialStatus = "Tất cả")
        {
            _selectedStatus = initialStatus;
            _filteredOrders = new ObservableCollection<Order>();

            CancelOrderCommand = new RelayCommand(o => _ = CancelOrderAsync(o), CanCancelOrder);
            RefundOrderCommand = new RelayCommand(o => _ = RefundOrderAsync(o), CanRefundOrder);
            ViewOrderCommand = new RelayCommand(o => { SelectedOrder = o as Order; ShowDetailRequest?.Invoke(SelectedOrder); });

            _ = LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                string dbStatus = SelectedStatus switch
                {
                    "Chờ xác nhận" => "Pending",
                    "Đang xử lý" => "Processing",
                    "Đang giao hàng" => "Shipping",
                    "Hoàn thành" => "Completed",
                    "Đã hủy" => "Cancelled",
                    _ => SelectedStatus
                };

                var allOrders = await OrderService.Instance.GetAllOrdersAsync(dbStatus, SearchKeyword);
                var stats = await OrderService.Instance.GetAdminStatsAsync();

                TotalOrders = stats.TotalOrders;
                PendingOrders = stats.PendingOrders;
                ShippingOrders = stats.ShippingOrders;
                TotalRevenue = stats.TotalRevenue;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredOrders = new ObservableCollection<Order>(allOrders);

                    if (SelectedOrder != null)
                    {
                        var updated = allOrders.FirstOrDefault(o => o.OrderId == SelectedOrder.OrderId);
                        if (updated != null)
                            SelectedOrder = updated;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải đơn hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task FilterOrdersAsync()
        {
            try
            {
                string dbStatus = SelectedStatus switch
                {
                    "Chờ xác nhận" => "Pending",
                    "Đang xử lý" => "Processing",
                    "Đang giao hàng" => "Shipping",
                    "Hoàn thành" => "Completed",
                    "Đã hủy" => "Cancelled",
                    _ => SelectedStatus
                };

                var list = await OrderService.Instance.GetAllOrdersAsync(dbStatus, SearchKeyword);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredOrders = new ObservableCollection<Order>(list);

                    if (SelectedOrder != null)
                    {
                        var updated = list.FirstOrDefault(o => o.OrderId == SelectedOrder.OrderId);
                        if (updated != null)
                            SelectedOrder = updated;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lọc đơn hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanCancelOrder(object _) =>
            SelectedOrder != null &&
            SelectedOrder.OrderStatus != "Đã hủy" &&
            SelectedOrder.OrderStatus != "Hoàn thành" &&
            SelectedOrder.OrderStatus != "Hoàn tiền";

        private async Task CancelOrderAsync(object _)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Hủy đơn {SelectedOrder.OrderCode}?\nHành động này không thể hoàn tác.",
                "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await OrderService.Instance.CancelOrderAsync(SelectedOrder.OrderId);
                SelectedOrder.OrderStatus = "Cancelled";
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Không thể hủy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hủy đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AuditLogHelper.Log("CANCEL_ORDER", $"Hủy '{SelectedOrder.OrderCode}' ({SelectedOrder.TotalAmount:N0} đ) — Shop: {SelectedOrder.Shop?.ShopName}", "Đơn hàng", "Critical");
            MessageBox.Show("Đã hủy đơn hàng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

            var selectedId = SelectedOrder?.OrderId;
            HideDetailRequest?.Invoke();
            _ = LoadOrdersAsync();
            if (selectedId.HasValue)
                SelectedOrder = FilteredOrders.FirstOrDefault(o => o.OrderId == selectedId.Value);
        }

        private bool CanRefundOrder(object _) =>
            SelectedOrder != null &&
            SelectedOrder.OrderStatus != "Hoàn tiền" &&
            (SelectedOrder.OrderStatus == "Đã hủy" || SelectedOrder.OrderStatus == "Hoàn thành" || SelectedOrder.OrderStatus == "Đang giao hàng");

        private async Task RefundOrderAsync(object _)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Hoàn tiền đơn {SelectedOrder.OrderCode}?\nTiền sẽ được cộng lại vào ví người mua.",
                "Xác nhận hoàn tiền", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await OrderService.Instance.RefundOrderAsync(SelectedOrder.OrderId);
                SelectedOrder.OrderStatus = "Hoàn tiền";
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Không thể hoàn tiền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hoàn tiền: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AuditLogHelper.Log("REFUND_ORDER", $"Hoàn tiền '{SelectedOrder.OrderCode}' ({SelectedOrder.TotalAmount:N0} đ) — Người mua: {SelectedOrder.Buyer?.FullName}", "Đơn hàng", "Critical");
            MessageBox.Show("Đã hoàn tiền cho người mua.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

            var selectedId = SelectedOrder?.OrderId;
            HideDetailRequest?.Invoke();
            _ = LoadOrdersAsync();
            if (selectedId.HasValue)
                SelectedOrder = FilteredOrders.FirstOrDefault(o => o.OrderId == selectedId.Value);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

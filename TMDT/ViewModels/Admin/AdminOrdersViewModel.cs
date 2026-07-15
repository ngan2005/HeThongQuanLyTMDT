using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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

        // 🟢 Dùng CancellationToken để hủy các query search cũ khi user gõ tiếp — tránh race condition (kết quả cũ ghi đè kết quả mới).
        private CancellationTokenSource? _searchCts;

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
                // 🟢 Bắt buộc WPF re-evaluate CanExecute cho Cancel/Refund command
                // vì CanCancelOrder/CanRefundOrder phụ thuộc SelectedOrder.OrderStatus.
                CommandManager.InvalidateRequerySuggested();
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
                    "Hoàn tiền" => "Hoàn tiền",
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
            // 🟢 Hủy query search trước (nếu có) để tránh race condition khi user gõ nhanh
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                string dbStatus = SelectedStatus switch
                {
                    "Chờ xác nhận" => "Pending",
                    "Đang xử lý" => "Processing",
                    "Đang giao hàng" => "Shipping",
                    "Hoàn thành" => "Completed",
                    "Đã hủy" => "Cancelled",
                    "Hoàn tiền" => "Hoàn tiền",
                    _ => SelectedStatus
                };

                var list = await OrderService.Instance.GetAllOrdersAsync(dbStatus, SearchKeyword);

                // 🟢 Nếu đã có query mới hơn → bỏ kết quả cũ
                token.ThrowIfCancellationRequested();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (token.IsCancellationRequested) return;
                    FilteredOrders = new ObservableCollection<Order>(list);

                    if (SelectedOrder != null)
                    {
                        var updated = list.FirstOrDefault(o => o.OrderId == SelectedOrder.OrderId);
                        if (updated != null)
                            SelectedOrder = updated;
                    }
                });
            }
            catch (OperationCanceledException) { /* query bị hủy bởi search mới — bình thường */ }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    MessageBox.Show($"Lỗi lọc đơn hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🟢 Chỉ cho phép hủy khi đơn còn ở trạng thái đầu (Pending/Processing) — Đã hủy/Hoàn thành/Đang giao/Hoàn tiền đều khóa để tránh sai workflow
        private bool CanCancelOrder(object? _)
        {
            return SelectedOrder != null && 
                   (SelectedOrder.OrderStatus == "Pending" || SelectedOrder.OrderStatus == "Shipping");
        }

        private async Task CancelOrderAsync(object? _)
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
            // 🟢 Đóng cửa sổ chi tiết NGAY (trước khi load) — tránh cửa sổ chi tiết hiển thị đơn đã đổi trạng thái.
            HideDetailRequest?.Invoke();
            _ = LoadOrdersAsync();
            // 🟢 Sau load: nếu đơn còn trong list (vd filter chưa loại) → chọn lại; nếu không còn (filter = Pending thì đơn vừa cancel biến mất) → để null.
            if (selectedId.HasValue && FilteredOrders != null)
                SelectedOrder = FilteredOrders.FirstOrDefault(o => o.OrderId == selectedId.Value);
        }

        // 🟢 Chỉ hoàn tiền khi đơn đã đối soát xong — Đã hủy (chưa chuyển tiền) hoặc Hoàn thành. Đang giao/Đã hủy-sau-ship cần workflow riêng.
        private bool CanRefundOrder(object? _)
        {
            return SelectedOrder != null && SelectedOrder.OrderStatus == "ReturnRequest";
        }

        private async Task RefundOrderAsync(object? _)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Hoàn tiền đơn {SelectedOrder.OrderCode}?\nTiền sẽ được cộng lại vào ví người mua.",
                "Xác nhận hoàn tiền", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await OrderService.Instance.AdminRefundOrderAsync(SelectedOrder.OrderId);
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
            // 🟢 Đóng cửa sổ chi tiết NGAY — tránh hiển thị đơn vừa chuyển sang "Hoàn tiền".
            HideDetailRequest?.Invoke();
            _ = LoadOrdersAsync();
            if (selectedId.HasValue && FilteredOrders != null)
                SelectedOrder = FilteredOrders.FirstOrDefault(o => o.OrderId == selectedId.Value);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

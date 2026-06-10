using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminOrdersViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;

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
            set { _searchKeyword = value; OnPropertyChanged(); FilterOrders(); }
        }

        private string _selectedStatus = "Tất cả";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); FilterOrders(); }
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

        public AdminOrdersViewModel()
        {
            _context = new TmdtContext();
            _filteredOrders = new ObservableCollection<Order>();

            CancelOrderCommand = new RelayCommand(CancelOrder, CanCancelOrder);
            RefundOrderCommand = new RelayCommand(RefundOrder, CanRefundOrder);
            ViewOrderCommand = new RelayCommand(o => { SelectedOrder = o as Order; ShowDetailRequest?.Invoke(SelectedOrder); });

            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                _context.ChangeTracker.Clear();
                var allOrders = _context.Orders
                    .Include(o => o.Shop)
                    .Include(o => o.Buyer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                // Stats
                TotalOrders = allOrders.Count;
                PendingOrders = allOrders.Count(o => o.OrderStatus == "Chờ xác nhận");
                ShippingOrders = allOrders.Count(o => o.OrderStatus == "Đang giao hàng");
                TotalRevenue = allOrders.Where(o => o.OrderStatus == "Hoàn thành").Sum(o => o.TotalAmount ?? 0);

                FilteredOrders = new ObservableCollection<Order>(allOrders);

                if (SelectedOrder != null)
                {
                    var updated = allOrders.FirstOrDefault(o => o.OrderId == SelectedOrder.OrderId);
                    if (updated != null)
                    {
                        SelectedOrder = updated;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải đơn hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterOrders()
        {
            if (_context == null) return;

            try
            {
                _context.ChangeTracker.Clear();
                var query = _context.Orders
                    .Include(o => o.Shop)
                    .Include(o => o.Buyer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    string keyword = SearchKeyword.ToLower();
                    query = query.Where(o =>
                        (o.OrderCode != null && o.OrderCode.ToLower().Contains(keyword)) ||
                        (o.Shop != null && o.Shop.ShopName != null && o.Shop.ShopName.ToLower().Contains(keyword)) ||
                        (o.Buyer != null && o.Buyer.FullName != null && o.Buyer.FullName.ToLower().Contains(keyword))
                    );
                }

                if (SelectedStatus != "Tất cả")
                    query = query.Where(o => o.OrderStatus == SelectedStatus);

                var list = query.OrderByDescending(o => o.OrderDate).ToList();
                FilteredOrders = new ObservableCollection<Order>(list);

                if (SelectedOrder != null)
                {
                    var updated = list.FirstOrDefault(o => o.OrderId == SelectedOrder.OrderId);
                    if (updated != null)
                    {
                        SelectedOrder = updated;
                    }
                }
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

        private void CancelOrder(object _)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Hủy đơn {SelectedOrder.OrderCode}?\nHành động này không thể hoàn tác.",
                "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var dbOrder = _context.Orders.Find(SelectedOrder.OrderId);
                    if (dbOrder == null)
                    {
                        MessageBox.Show("Không tìm thấy đơn hàng trong cơ sở dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    dbOrder.OrderStatus = "Đã hủy";
                    _context.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = SelectedOrder.OrderId,
                        NewStatus = "Đã hủy",
                        Note = "Hủy khẩn cấp bởi Admin",
                        ChangedAt = DateTime.Now
                    });

                    _context.SaveChanges();
                    transaction.Commit();

                    SelectedOrder.OrderStatus = "Đã hủy";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Lỗi hủy đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            AuditLogHelper.Log("CANCEL_ORDER", $"Hủy '{SelectedOrder.OrderCode}' ({SelectedOrder.TotalAmount:N0} đ) — Shop: {SelectedOrder.Shop?.ShopName}", "Đơn hàng", "Critical");
            MessageBox.Show("Đã hủy đơn hàng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            
            var selectedId = SelectedOrder?.OrderId;
            HideDetailRequest?.Invoke();
            LoadOrders();
            if (selectedId.HasValue)
            {
                SelectedOrder = FilteredOrders.FirstOrDefault(o => o.OrderId == selectedId.Value);
            }
        }

        private bool CanRefundOrder(object _) =>
            SelectedOrder != null &&
            SelectedOrder.OrderStatus != "Hoàn tiền" &&
            (SelectedOrder.OrderStatus == "Đã hủy" || SelectedOrder.OrderStatus == "Hoàn thành" || SelectedOrder.OrderStatus == "Đang giao hàng");

        private void RefundOrder(object _)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Hoàn tiền đơn {SelectedOrder.OrderCode}?\nTiền sẽ được cộng lại vào ví người mua.",
                "Xác nhận hoàn tiền", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var dbOrder = _context.Orders.Find(SelectedOrder.OrderId);
                    if (dbOrder == null)
                    {
                        MessageBox.Show("Không tìm thấy đơn hàng trong cơ sở dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Hoàn tiền vào ví người mua
                    if (dbOrder.BuyerId.HasValue)
                    {
                        var buyer = _context.Users.Find(dbOrder.BuyerId.Value);
                        if (buyer == null)
                        {
                            MessageBox.Show("Không tìm thấy thông tin người mua để hoàn tiền.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        buyer.WalletBalance = (buyer.WalletBalance ?? 0) + (dbOrder.TotalAmount ?? 0);
                    }
                    else
                    {
                        MessageBox.Show("Đơn hàng không có thông tin người mua.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    dbOrder.OrderStatus = "Hoàn tiền";
                    _context.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = SelectedOrder.OrderId,
                        NewStatus = "Hoàn tiền",
                        Note = "Hoàn tiền bởi Admin",
                        ChangedAt = DateTime.Now
                    });

                    _context.SaveChanges();
                    transaction.Commit();

                    SelectedOrder.OrderStatus = "Hoàn tiền";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Lỗi hoàn tiền: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            AuditLogHelper.Log("REFUND_ORDER", $"Hoàn tiền '{SelectedOrder.OrderCode}' ({SelectedOrder.TotalAmount:N0} đ) — Người mua: {SelectedOrder.Buyer?.FullName}", "Đơn hàng", "Critical");
            MessageBox.Show("Đã hoàn tiền cho người mua.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            
            var selectedId = SelectedOrder?.OrderId;
            HideDetailRequest?.Invoke();
            LoadOrders();
            if (selectedId.HasValue)
            {
                SelectedOrder = FilteredOrders.FirstOrDefault(o => o.OrderId == selectedId.Value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;
using System.Windows;

namespace TMDT.ViewModels.Admin
{
    public class AdminOrdersViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;

        private ObservableCollection<Order> _filteredOrders;
        public ObservableCollection<Order> FilteredOrders
        {
            get => _filteredOrders;
            set
            {
                _filteredOrders = value;
                OnPropertyChanged();
            }
        }

        private Order _selectedOrder;
        public Order SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                OnPropertyChanged();
                IsOrderSelected = value != null;
            }
        }

        private bool _isOrderSelected;
        public bool IsOrderSelected
        {
            get => _isOrderSelected;
            set
            {
                _isOrderSelected = value;
                OnPropertyChanged();
            }
        }

        private string _searchKeyword = "";
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                _searchKeyword = value;
                OnPropertyChanged();
                FilterOrders();
            }
        }

        private string _selectedStatus = "Tất cả";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged();
                FilterOrders();
            }
        }

        public ObservableCollection<string> Statuses { get; } = new ObservableCollection<string>
        {
            "Tất cả", "Chờ xác nhận", "Đang xử lý", "Đang giao hàng", "Hoàn thành", "Đã hủy", "Hoàn tiền"
        };

        public ICommand CancelOrderCommand { get; }
        public ICommand RefundOrderCommand { get; }

        public AdminOrdersViewModel()
        {
            _context = new TmdtContext();
            _filteredOrders = new ObservableCollection<Order>();

            CancelOrderCommand = new RelayCommand(CancelOrder, CanCancelOrder);
            RefundOrderCommand = new RelayCommand(RefundOrder, CanRefundOrder);

            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.Shop)
                    .Include(o => o.Buyer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                FilteredOrders = new ObservableCollection<Order>(query);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách đơn hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterOrders()
        {
            if (_context == null) return;

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
                    o.OrderCode.ToLower().Contains(keyword) ||
                    (o.Shop != null && o.Shop.ShopName.ToLower().Contains(keyword)) ||
                    (o.Buyer != null && o.Buyer.FullName.ToLower().Contains(keyword))
                );
            }

            if (SelectedStatus != "Tất cả")
            {
                query = query.Where(o => o.OrderStatus == SelectedStatus);
            }

            FilteredOrders = new ObservableCollection<Order>(query.OrderByDescending(o => o.OrderDate).ToList());
        }

        private bool CanCancelOrder(object param)
        {
            return SelectedOrder != null && 
                   SelectedOrder.OrderStatus != "Đã hủy" && 
                   SelectedOrder.OrderStatus != "Hoàn thành" &&
                   SelectedOrder.OrderStatus != "Hoàn tiền";
        }

        private void CancelOrder(object param)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn HỦY đơn hàng {SelectedOrder.OrderCode} do vấn đề khẩn cấp?\nHành động này không thể hoàn tác.", "Cảnh báo hệ thống", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    SelectedOrder.OrderStatus = "Đã hủy";
                    
                    var history = new OrderStatusHistory
                    {
                        OrderId = SelectedOrder.OrderId,
                        NewStatus = "Đã hủy",
                        Note = "Đơn hàng bị hủy khẩn cấp bởi Admin hệ thống",
                        ChangedAt = DateTime.Now
                    };
                    _context.OrderStatusHistories.Add(history);

                    _context.SaveChanges();
                    AuditLogHelper.Log("CANCEL_ORDER", $"Hủy khẩn cấp đơn hàng '{SelectedOrder.OrderCode}' (trị giá: {SelectedOrder.TotalAmount:N0} đ) — Shop: {SelectedOrder.Shop?.ShopName}", "Đơn hàng", "Critical");
                    MessageBox.Show("Đã hủy đơn hàng thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Trigger refresh details
                    OnPropertyChanged(nameof(SelectedOrder));
                    FilterOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi hủy đơn hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanRefundOrder(object param)
        {
            return SelectedOrder != null && 
                   (SelectedOrder.OrderStatus == "Đã hủy" || SelectedOrder.OrderStatus == "Hoàn thành" || SelectedOrder.OrderStatus == "Đang giao hàng") &&
                   SelectedOrder.OrderStatus != "Hoàn tiền";
        }

        private void RefundOrder(object param)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn HOÀN TIỀN cho đơn hàng {SelectedOrder.OrderCode}?\nTiền sẽ được cộng lại vào ví người mua.", "Xác nhận hoàn tiền", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    SelectedOrder.OrderStatus = "Hoàn tiền";
                    
                    var history = new OrderStatusHistory
                    {
                        OrderId = SelectedOrder.OrderId,
                        NewStatus = "Hoàn tiền",
                        Note = "Đơn hàng được hoàn tiền khẩn cấp bởi Admin",
                        ChangedAt = DateTime.Now
                    };
                    _context.OrderStatusHistories.Add(history);

                    _context.SaveChanges();
                    AuditLogHelper.Log("REFUND_ORDER", $"Hoàn tiền khẩn cấp đơn hàng '{SelectedOrder.OrderCode}' (trị giá: {SelectedOrder.TotalAmount:N0} đ) — Người mua: {SelectedOrder.Buyer?.FullName}", "Đơn hàng", "Critical");
                    MessageBox.Show("Đã xử lý hoàn tiền cho người mua.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    OnPropertyChanged(nameof(SelectedOrder));
                    FilterOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi hoàn tiền: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

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
                using var context = new TmdtContext();
                var allOrders = await context.Orders.AsNoTracking()
                    .Include(o => o.Shop)
                    .Include(o => o.Buyer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                // Stats
                TotalOrders = allOrders.Count;
                PendingOrders = allOrders.Count(o => o.OrderStatus == "Chờ xác nhận");
                ShippingOrders = allOrders.Count(o => o.OrderStatus == "Đang giao hàng");
                TotalRevenue = allOrders.Where(o => o.OrderStatus == "Hoàn thành" || o.OrderStatus == "Đã giao hàng").Sum(o => o.TotalAmount ?? 0);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredOrders = new ObservableCollection<Order>(allOrders);

                    if (SelectedOrder != null)
                    {
                        var updated = allOrders.FirstOrDefault(o => o.OrderId == SelectedOrder.OrderId);
                        if (updated != null)
                        {
                            SelectedOrder = updated;
                        }
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
                using var context = new TmdtContext();
                var query = context.Orders.AsNoTracking()
                    .Include(o => o.Shop)
                    .Include(o => o.Buyer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    string keyword = SearchKeyword.Trim();
                    query = query.Where(o =>
                        (o.OrderCode != null && EF.Functions.Like(o.OrderCode, $"%{keyword}%")) ||
                        (o.Shop != null && o.Shop.ShopName != null && EF.Functions.Like(o.Shop.ShopName, $"%{keyword}%")) ||
                        (o.Buyer != null && o.Buyer.FullName != null && EF.Functions.Like(o.Buyer.FullName, $"%{keyword}%"))
                    );
                }

                if (SelectedStatus != "Tất cả")
                    query = query.Where(o => o.OrderStatus == SelectedStatus);

                var list = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredOrders = new ObservableCollection<Order>(list);

                    if (SelectedOrder != null)
                    {
                        var updated = list.FirstOrDefault(o => o.OrderId == SelectedOrder.OrderId);
                        if (updated != null)
                        {
                            SelectedOrder = updated;
                        }
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

            using var context = new TmdtContext();
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var dbOrder = await context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == SelectedOrder.OrderId);
                if (dbOrder == null)
                {
                    MessageBox.Show("Không tìm thấy đơn hàng trong cơ sở dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Hoàn trả lại số lượng tồn kho
                foreach (var detail in dbOrder.OrderDetails)
                {
                    if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                    {
                        var product = await context.Products.FindAsync(detail.ProductId.Value);
                        if (product != null)
                            product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity.Value;
                    }
                }

                dbOrder.OrderStatus = "Đã hủy";
                context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = SelectedOrder.OrderId,
                    NewStatus = "Đã hủy",
                    Note = "Hủy khẩn cấp bởi Admin",
                    ChangedAt = DateTime.Now
                });

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                SelectedOrder.OrderStatus = "Đã hủy";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                MessageBox.Show($"Lỗi hủy đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AuditLogHelper.Log("CANCEL_ORDER", $"Hủy '{SelectedOrder.OrderCode}' ({SelectedOrder.TotalAmount:N0} đ) — Shop: {SelectedOrder.Shop?.ShopName}", "Đơn hàng", "Critical");
            MessageBox.Show("Đã hủy đơn hàng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            
            var selectedId = SelectedOrder?.OrderId;
            HideDetailRequest?.Invoke();
            _ = LoadOrdersAsync();
            if (selectedId.HasValue)
            {
                SelectedOrder = FilteredOrders.FirstOrDefault(o => o.OrderId == selectedId.Value);
            }
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

            using var context = new TmdtContext();
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var dbOrder = await context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == SelectedOrder.OrderId);
                if (dbOrder == null)
                    throw new Exception("Không tìm thấy đơn hàng trong cơ sở dữ liệu.");

                if (!dbOrder.BuyerId.HasValue)
                    throw new Exception("Đơn hàng không có thông tin người mua.");

                var buyer = await context.Users.FindAsync(dbOrder.BuyerId.Value);
                if (buyer == null)
                    throw new Exception("Không tìm thấy thông tin người mua để hoàn tiền.");

                // Trừ tiền của Shop nếu đơn hàng đã "Hoàn thành" (Shop đã nhận tiền)
                if (dbOrder.OrderStatus == "Hoàn thành" && dbOrder.ShopId.HasValue)
                {
                    var shop = await context.Shops.FindAsync(dbOrder.ShopId.Value);
                    if (shop != null)
                    {
                        var revenue = (dbOrder.TotalAmount ?? 0) - (dbOrder.PlatformFee ?? 0);
                        shop.WalletBalance = (shop.WalletBalance ?? 0) - revenue;
                        
                        // Trừ phí sàn khỏi ví tổng hệ thống
                        SystemSettingsHelper.AddSystemWalletBalance(-(dbOrder.PlatformFee ?? 0));
                    }
                }

                // Hoàn trả Tồn kho
                foreach (var detail in dbOrder.OrderDetails)
                {
                    if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                    {
                        var product = await context.Products.FindAsync(detail.ProductId.Value);
                        if (product != null)
                            product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity.Value;
                    }
                }

                // Hoàn tiền vào ví người mua
                buyer.WalletBalance = (buyer.WalletBalance ?? 0) + (dbOrder.TotalAmount ?? 0);
                dbOrder.OrderStatus = "Hoàn tiền";
                context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = SelectedOrder.OrderId,
                    NewStatus = "Hoàn tiền",
                    Note = "Hoàn tiền bởi Admin",
                    ChangedAt = DateTime.Now
                });

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                SelectedOrder.OrderStatus = "Hoàn tiền";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                MessageBox.Show($"Lỗi hoàn tiền: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AuditLogHelper.Log("REFUND_ORDER", $"Hoàn tiền '{SelectedOrder.OrderCode}' ({SelectedOrder.TotalAmount:N0} đ) — Người mua: {SelectedOrder.Buyer?.FullName}", "Đơn hàng", "Critical");
            MessageBox.Show("Đã hoàn tiền cho người mua.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            
            var selectedId = SelectedOrder?.OrderId;
            HideDetailRequest?.Invoke();
            _ = LoadOrdersAsync();
            if (selectedId.HasValue)
            {
                SelectedOrder = FilteredOrders.FirstOrDefault(o => o.OrderId == selectedId.Value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class SellerOrdersViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        private ObservableCollection<Order> _orders;
        private Order _selectedOrder;
        private string _statusFilter = "All"; // All, Pending, Shipping, Completed, Cancelled

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set { _orders = value; OnPropertyChanged(); }
        }

        public Order SelectedOrder
        {
            get => _selectedOrder;
            set { _selectedOrder = value; OnPropertyChanged(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); LoadOrders(); }
        }

        // Commands
        public ICommand ShipOrderCommand { get; }
        public ICommand CompleteOrderCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand SetFilterCommand { get; }

        public SellerOrdersViewModel()
        {
            try
            {
                _context = new TmdtContext();
            }
            catch {}

            Orders = new ObservableCollection<Order>();

            ShipOrderCommand = new RelayCommand(ExecuteShipOrder, o => SelectedOrder != null && SelectedOrder.OrderStatus == "Pending");
            CompleteOrderCommand = new RelayCommand(ExecuteCompleteOrder, o => SelectedOrder != null && SelectedOrder.OrderStatus == "Shipping");
            CancelOrderCommand = new RelayCommand(ExecuteCancelOrder, o => SelectedOrder != null && (SelectedOrder.OrderStatus == "Pending" || SelectedOrder.OrderStatus == "Shipping"));
            SetFilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");

            LoadOrders();
        }

        private void LoadOrders()
        {
            Orders.Clear();
            int currentShopId = GetCurrentShopId();

            try
            {
                if (_context != null && _context.Orders.Any())
                {
                    var query = _context.Orders
                        .Include(o => o.Buyer)
                        .Include(o => o.Address)
                        .Include(o => o.OrderDetails)
                        .Where(o => o.ShopId == currentShopId)
                        .AsQueryable();

                    if (StatusFilter != "All")
                    {
                        query = query.Where(o => o.OrderStatus == StatusFilter);
                    }

                    var dbOrders = query.OrderByDescending(o => o.OrderDate).ToList();
                    foreach (var order in dbOrders)
                    {
                        Orders.Add(order);
                    }

                    if (Orders.Any()) return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load orders from DB: " + ex.Message);
            }

        }

        private async void ExecuteShipOrder(object obj)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show($"Xác nhận chuẩn bị hàng và giao cho đơn vị vận chuyển cho đơn '{SelectedOrder.OrderCode}'?", 
                                         "Xác nhận giao hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            SelectedOrder.OrderStatus = "Shipping";
            SelectedOrder.TrackingCode = "SPX-" + new Random().Next(10000000, 99999999);

            try
            {
                if (_context != null)
                {
                    var dbOrder = await _context.Orders.FindAsync(SelectedOrder.OrderId);
                    if (dbOrder != null)
                    {
                        dbOrder.OrderStatus = "Shipping";
                        dbOrder.TrackingCode = SelectedOrder.TrackingCode;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF update order failed: " + ex.Message);
            }

            MessageBox.Show($"Đã xác nhận đơn hàng thành công! Mã vận đơn là: {SelectedOrder.TrackingCode}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadOrders();
        }

        private async void ExecuteCompleteOrder(object obj)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show($"Xác nhận đơn hàng '{SelectedOrder.OrderCode}' đã giao thành công tới người mua?", 
                                         "Xác nhận hoàn thành", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            SelectedOrder.OrderStatus = "Completed";
            SelectedOrder.CompletedAt = DateTime.Now;

            try
            {
                if (_context != null)
                {
                    var dbOrder = await _context.Orders.FindAsync(SelectedOrder.OrderId);
                    if (dbOrder != null)
                    {
                        dbOrder.OrderStatus = "Completed";
                        dbOrder.CompletedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF update order failed: " + ex.Message);
            }

            MessageBox.Show("Đã hoàn thành đơn đặt hàng! Số tiền doanh thu sẽ được cộng vào Ví của Shop.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadOrders();
        }

        private async void ExecuteCancelOrder(object obj)
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn HỦY đơn hàng '{SelectedOrder.OrderCode}'?", 
                                         "Xác nhận hủy đơn", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            SelectedOrder.OrderStatus = "Cancelled";

            try
            {
                if (_context != null)
                {
                    var dbOrder = await _context.Orders.FindAsync(SelectedOrder.OrderId);
                    if (dbOrder != null)
                    {
                        dbOrder.OrderStatus = "Cancelled";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF update order failed: " + ex.Message);
            }

            MessageBox.Show("Đơn hàng đã được hủy thành công.", "Đã hủy", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadOrders();
        }

        private int GetCurrentShopId()
        {
            try
            {
                if (_context != null)
                {
                    var shop = _context.Shops
                        .Include(s => s.User)
                        .FirstOrDefault(s => s.User != null && s.User.Email == "seller@myshop.com")
                        ?? _context.Shops.FirstOrDefault();
                    if (shop != null) return shop.ShopId;
                }
            }
            catch {}
            return 1;
        }
    }
}

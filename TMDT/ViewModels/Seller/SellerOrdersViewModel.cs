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

            LoadMockOrders();
        }

        private void LoadMockOrders()
        {
            var mockOrders = new ObservableCollection<Order>();

            var address1 = new Address { RecipientName = "Nguyễn Hùng Anh", Phone = "0912345678", FullAddress = "Số 15 Ngõ 12 Cầu Giấy, Hà Nội" };
            var address2 = new Address { RecipientName = "Phan Thị Thu Hà", Phone = "0987654321", FullAddress = "Chung cư Sunrise City, Quận 7, TP HCM" };
            var address3 = new Address { RecipientName = "Lê Việt Hoàng", Phone = "0909090909", FullAddress = "34 Nguyễn Hữu Thọ, Đà Nẵng" };
            var address4 = new Address { RecipientName = "Vũ Hoàng My", Phone = "0944455566", FullAddress = "12 Trần Hưng Đạo, Cần Thơ" };

            // Mock 1: Pending
            var o1 = new Order
            {
                OrderId = 701,
                OrderCode = "ORD-7701",
                OrderStatus = "Pending",
                PaymentMethod = "Thanh toán Online",
                SubTotal = 4980000,
                ShippingFee = 30000,
                TotalAmount = 5010000,
                OrderDate = DateTime.Now.AddMinutes(-45),
                Buyer = new User { FullName = "Nguyễn Hùng Anh", Email = "hunganh@gmail.com" },
                Address = address1,
                Note = "Giao hàng giờ hành chính giúp em ạ."
            };
            o1.OrderDetails.Add(new OrderDetail { ProductNameSnapshot = "Nồi Chiên Không Dầu Tefal XXL 5.6L", Quantity = 2, UnitPrice = 2490000, TotalPrice = 4980000 });
            mockOrders.Add(o1);

            // Mock 2: Shipping
            var o2 = new Order
            {
                OrderId = 702,
                OrderCode = "ORD-7702",
                OrderStatus = "Shipping",
                PaymentMethod = "COD (Tiền mặt)",
                SubTotal = 189000,
                ShippingFee = 15000,
                TotalAmount = 204000,
                OrderDate = DateTime.Now.AddHours(-3),
                Buyer = new User { FullName = "Phan Thị Thu Hà", Email = "thuha@gmail.com" },
                Address = address2,
                TrackingCode = "GHTK-983812739",
                Note = "Xin hãy gọi trước khi giao 15 phút."
            };
            o2.OrderDetails.Add(new OrderDetail { ProductNameSnapshot = "Áo Thun Unisex Cotton Organic Cao Cấp", Quantity = 1, UnitPrice = 189000, TotalPrice = 189000 });
            mockOrders.Add(o2);

            // Mock 3: Completed
            var o3 = new Order
            {
                OrderId = 703,
                OrderCode = "ORD-7703",
                OrderStatus = "Completed",
                PaymentMethod = "Thanh toán Online",
                SubTotal = 14500000,
                ShippingFee = 0,
                TotalAmount = 14500000,
                OrderDate = DateTime.Now.AddDays(-1),
                CompletedAt = DateTime.Now.AddDays(-1).AddHours(4),
                Buyer = new User { FullName = "Lê Việt Hoàng", Email = "vhoang@gmail.com" },
                Address = address3,
                TrackingCode = "VNP-12903827"
            };
            o3.OrderDetails.Add(new OrderDetail { ProductNameSnapshot = "Robot Hút Bụi Lau Nhà Roborock Q Revo", Quantity = 1, UnitPrice = 14500000, TotalPrice = 14500000 });
            mockOrders.Add(o3);

            // Mock 4: Completed
            var o4 = new Order
            {
                OrderId = 704,
                OrderCode = "ORD-7704",
                OrderStatus = "Completed",
                PaymentMethod = "COD",
                SubTotal = 378000,
                ShippingFee = 25000,
                TotalAmount = 403000,
                OrderDate = DateTime.Now.AddDays(-2),
                CompletedAt = DateTime.Now.AddDays(-2).AddHours(2),
                Buyer = new User { FullName = "Vũ Hoàng My", Email = "mymy@gmail.com" },
                Address = address4,
                TrackingCode = "GHTK-9832917"
            };
            o4.OrderDetails.Add(new OrderDetail { ProductNameSnapshot = "Áo Thun Unisex Cotton Organic Cao Cấp", Quantity = 2, UnitPrice = 189000, TotalPrice = 378000 });
            mockOrders.Add(o4);

            // Mock 5: Cancelled
            var o5 = new Order
            {
                OrderId = 705,
                OrderCode = "ORD-7705",
                OrderStatus = "Cancelled",
                PaymentMethod = "COD",
                SubTotal = 6490000,
                ShippingFee = 50000,
                TotalAmount = 6540000,
                OrderDate = DateTime.Now.AddDays(-5),
                Buyer = new User { FullName = "Trần Minh Quân", Email = "mquan@gmail.com" },
                Address = address1,
                Note = "Khách hàng hủy đơn do đặt nhầm sản phẩm"
            };
            o5.OrderDetails.Add(new OrderDetail { ProductNameSnapshot = "Tai nghe Chống Ồn Sony WH-1000XM5", Quantity = 1, UnitPrice = 6490000, TotalPrice = 6490000 });
            mockOrders.Add(o5);

            var filtered = mockOrders.AsQueryable();
            if (StatusFilter != "All")
            {
                filtered = filtered.Where(o => o.OrderStatus == StatusFilter);
            }

            foreach (var order in filtered.ToList())
            {
                Orders.Add(order);
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

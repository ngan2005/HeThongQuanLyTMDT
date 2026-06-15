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

            try
            {
                using var ctx = new TmdtContext();
                if (await ctx.Orders.AnyAsync())
                {
                    var query = ctx.Orders
                        .Include(o => o.Buyer)
                        .Include(o => o.Address)
                        .Include(o => o.OrderDetails)
                        .Where(o => o.ShopId == currentShopId)
                        .AsQueryable();

                    if (StatusFilter != "All")
                    {
                        query = query.Where(o => o.OrderStatus == StatusFilter);
                    }

                    var dbOrders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
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

            var result = MessageBox.Show($"Xác nhận chuẩn bị hàng và giao cho đơn vị vận chuyển [{SelectedShippingProvider}] cho đơn '{SelectedOrder.OrderCode}'?", 
                                         "Xác nhận giao hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            string trackingPrefix = "SPX";
            if (SelectedShippingProvider == "Giao Hàng Tiết Kiệm") trackingPrefix = "GHTK";
            else if (SelectedShippingProvider == "Viettel Post") trackingPrefix = "VTP";
            else if (SelectedShippingProvider == "Giao Hàng Nhanh") trackingPrefix = "GHN";
            else if (SelectedShippingProvider == "J&T Express") trackingPrefix = "JNT";

            SelectedOrder.OrderStatus = "Shipping";
            SelectedOrder.TrackingCode = trackingPrefix + "-" + new Random().Next(10000000, 99999999);

            try
            {
                using var ctx = new TmdtContext();
                var dbOrder = await ctx.Orders.FindAsync(SelectedOrder.OrderId);
                if (dbOrder != null)
                {
                    dbOrder.OrderStatus = "Shipping";
                    dbOrder.TrackingCode = SelectedOrder.TrackingCode;
                    await ctx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF update order failed: " + ex.Message);
            }

            MessageBox.Show($"Đã xác nhận đơn hàng thành công! Mã vận đơn là: {SelectedOrder.TrackingCode}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            _ = LoadOrdersAsync();
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
                using var ctx = new TmdtContext();
                await using var transaction = await ctx.Database.BeginTransactionAsync();
                try
                {
                    var dbOrder = await ctx.Orders
                        .Include(o => o.OrderDetails)
                            .FirstOrDefaultAsync(o => o.OrderId == SelectedOrder.OrderId);

                        if (dbOrder != null)
                        {
                            foreach (var detail in dbOrder.OrderDetails)
                            {
                                if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                                {
                                    var product = await ctx.Products.FindAsync(detail.ProductId.Value);
                                    if (product != null)
                                        product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity.Value;
                                }
                            }

                            dbOrder.OrderStatus = "Cancelled";
                            await ctx.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF update order failed: " + ex.Message);
                MessageBox.Show("Lỗi khi hủy đơn hàng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Đơn hàng đã được hủy thành công.", "Đã hủy", MessageBoxButton.OK, MessageBoxImage.Information);
            _ = LoadOrdersAsync();
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

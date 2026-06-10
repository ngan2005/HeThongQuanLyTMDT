using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
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
                if (value != null) LoadOrderDetails(value);
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); LoadOrders(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { SetProperty(ref _isLoading, value); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand SetFilterCommand { get; }

        public BuyerOrdersViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;

            RefreshCommand = new RelayCommand(_ => LoadOrders());
            CancelOrderCommand = new RelayCommand(o => ExecuteCancelOrder(o as Order), o => CanCancelOrder(o as Order));
            BackCommand = new RelayCommand(_ => _mainVm.NavigateHome());
            SetFilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "Tất cả");

            LoadOrders();
        }

        private void LoadOrders()
        {
            if (!SessionManager.IsLoggedIn) return;

            IsLoading = true;
            try
            {
                using var context = new TmdtContext();
                var userId = SessionManager.CurrentUser!.UserId;

                var query = context.Orders
                    .Include(o => o.Shop)
                    .Include(o => o.Address)
                    .Where(o => o.BuyerId == userId)
                    .AsQueryable();

                if (StatusFilter != "Tất cả")
                    query = query.Where(o => o.OrderStatus == StatusFilter);

                var list = query.OrderByDescending(o => o.OrderDate).ToList();

                Orders.Clear();
                foreach (var order in list)
                    Orders.Add(order);
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

        private void LoadOrderDetails(Order order)
        {
            if (order?.OrderId == null) return;
            try
            {
                using var context = new TmdtContext();
                var details = context.OrderDetails
                    .Where(d => d.OrderId == order.OrderId)
                    .ToList();

                order.OrderDetails.Clear();
                foreach (var d in details)
                    order.OrderDetails.Add(d);
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
                using var context = new TmdtContext();
                await using var transaction = await context.Database.BeginTransactionAsync();

                var dbOrder = await context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                if (dbOrder == null) return;

                dbOrder.OrderStatus = "Cancelled";

                foreach (var detail in dbOrder.OrderDetails)
                {
                    if (detail.ProductId.HasValue && detail.Quantity.HasValue)
                    {
                        var product = await context.Products.FindAsync(detail.ProductId.Value);
                        if (product != null)
                            product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity.Value;
                    }
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                order.OrderStatus = "Cancelled";
                OnPropertyChanged(nameof(Orders));

                MessageBox.Show("Đơn hàng đã được hủy.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi hủy đơn: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

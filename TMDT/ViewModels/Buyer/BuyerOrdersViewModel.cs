using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;
using Microsoft.EntityFrameworkCore;

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
                if (value != null) _ = LoadOrderDetailsAsync(value);
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); _ = LoadOrdersAsync(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { SetProperty(ref _isLoading, value); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand ReceiveOrderCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand SetFilterCommand { get; }

        public BuyerOrdersViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;

            RefreshCommand = new RelayCommand(_ => _ = LoadOrdersAsync());
            CancelOrderCommand = new RelayCommand(o => ExecuteCancelOrder(o as Order), o => CanCancelOrder(o as Order));
            ReceiveOrderCommand = new RelayCommand(o => ExecuteReceiveOrder(o as Order), o => CanReceiveOrder(o as Order));
            BackCommand = new RelayCommand(_ => _mainVm.NavigateHome());
            SetFilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "Tất cả");
            ReviewProductCommand = new RelayCommand(o => ExecuteReviewProduct(o as OrderDetail));

            _ = LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            if (!SessionManager.IsLoggedIn) return;

            IsLoading = true;
            try
            {
                var list = await OrderService.Instance.GetBuyerOrdersAsync(
                    SessionManager.CurrentUser!.UserId, StatusFilter);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Orders.Clear();
                    foreach (var order in list)
                        Orders.Add(order);
                });
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

        private async Task LoadOrderDetailsAsync(Order order)
        {
            if (order?.OrderId == null) return;
            try
            {
                var refreshed = await OrderService.Instance.GetOrderByIdAsync(order.OrderId);
                if (refreshed == null) return;

                using var ctx = new TmdtContext();
                var reviewedIds = ctx.Reviews
                    .Where(r => r.UserId == SessionManager.CurrentUser!.UserId && r.OrderDetailId != null)
                    .Select(r => r.OrderDetailId)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    order.OrderDetails.Clear();
                    foreach (var d in refreshed.OrderDetails)
                    {
                        d.IsReviewed = reviewedIds.Contains(d.OrderDetailId);
                        order.OrderDetails.Add(d);
                    }
                });
            }
            catch { }
        }

        public ICommand ReviewProductCommand { get; }

        private void ExecuteReviewProduct(OrderDetail? detail)
        {
            if (detail == null || detail.IsReviewed) return;

            var dialog = new TMDT.Views.Buyer.ReviewDialog(detail.ProductNameSnapshot ?? "Sản phẩm");
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var ctx = new TmdtContext();
                    var newReview = new Review
                    {
                        OrderDetailId = detail.OrderDetailId,
                        ProductId = detail.ProductId,
                        UserId = SessionManager.CurrentUser!.UserId,
                        StarRating = (byte)dialog.StarRating,
                        Content = dialog.ReviewContent,
                        ReviewedAt = DateTime.Now
                    };
                    ctx.Reviews.Add(newReview);
                    ctx.SaveChanges();

                    // Cập nhật lại Rating trung bình của Product và Shop
                    if (detail.ProductId.HasValue)
                    {
                        var product = ctx.Products.Find(detail.ProductId.Value);
                        if (product != null)
                        {
                            var allReviews = ctx.Reviews.Where(r => r.ProductId == product.ProductId).ToList();
                            if (allReviews.Any())
                            {
                                product.Rating = (decimal)allReviews.Average(r => r.StarRating ?? 0);
                            }

                            if (product.ShopId.HasValue)
                            {
                                var shop = ctx.Shops.Find(product.ShopId.Value);
                                if (shop != null)
                                {
                                    var shopReviews = ctx.Reviews
                                        .Include(r => r.Product)
                                        .Where(r => r.Product != null && r.Product.ShopId == shop.ShopId)
                                        .ToList();
                                    if (shopReviews.Any())
                                    {
                                        shop.Rating = (decimal)shopReviews.Average(r => r.StarRating ?? 0);
                                    }
                                }
                            }
                        }
                        ctx.SaveChanges();
                    }

                    detail.IsReviewed = true;
                    // Refresh UI
                    var order = Orders.FirstOrDefault(o => o.OrderId == detail.OrderId);
                    if (order != null)
                    {
                        var list = order.OrderDetails as IList<OrderDetail>;
                        if (list != null)
                        {
                            var index = list.IndexOf(detail);
                            if (index >= 0)
                            {
                                list.RemoveAt(index);
                                list.Insert(index, detail);
                            }
                        }
                        
                        var orderIndex = Orders.IndexOf(order);
                        if (orderIndex >= 0)
                        {
                            Orders.RemoveAt(orderIndex);
                            Orders.Insert(orderIndex, order);
                        }
                    }

                    MessageBox.Show("Cảm ơn bạn đã đánh giá sản phẩm!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi gửi đánh giá: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
                await OrderService.Instance.CancelOrderAsync(order.OrderId);
                order.OrderStatus = "Cancelled";
                OnPropertyChanged(nameof(Orders));

                MessageBox.Show("Đơn hàng đã được hủy.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Không thể hủy",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi hủy đơn: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanReceiveOrder(Order? order)
        {
            return order != null && order.OrderStatus == "Shipping";
        }

        private async void ExecuteReceiveOrder(Order? order)
        {
            if (order == null) return;

            var result = MessageBox.Show($"Xác nhận bạn đã nhận được đơn hàng '{order.OrderCode}' và hài lòng với sản phẩm?\nTiền sẽ được chuyển cho người bán.",
                                         "Xác nhận nhận hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await OrderService.Instance.ReceiveOrderAsync(order.OrderId);
                order.OrderStatus = "Completed";
                OnPropertyChanged(nameof(Orders));
                _ = LoadOrdersAsync();

                MessageBox.Show("Cảm ơn bạn đã mua sắm! Đơn hàng đã hoàn thành.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật đơn hàng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

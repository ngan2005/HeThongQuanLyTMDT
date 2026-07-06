using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using Microsoft.EntityFrameworkCore;

namespace TMDT.Views.Seller
{
    public partial class PosRefundWindow : Window
    {
        private readonly int _shopId;
        private Order? _foundOrder;

        public PosRefundWindow(int shopId)
        {
            InitializeComponent();
            _shopId = shopId;
            txtOrderCode.Focus();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void txtOrderCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnSearch_Click(sender, e);
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string code = txtOrderCode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;

            pnlOrderInfo.Tag = null;
            pnlOrderInfo.Visibility = Visibility.Collapsed;
            pnlWarning.Visibility = Visibility.Collapsed;
            btnRefund.IsEnabled = false;
            _foundOrder = null;

            try
            {
                using var ctx = new TmdtContext();
                var order = await ctx.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(o => o.OrderCode == code && o.ShopId == _shopId);

                if (order == null)
                {
                    txtWarning.Text = "Không tìm thấy đơn hàng với mã này trong cửa hàng của bạn.";
                    pnlWarning.Visibility = Visibility.Visible;
                    return;
                }

                if (order.OrderStatus == "Cancelled" || order.OrderStatus == "Refunded")
                {
                    txtWarning.Text = $"Đơn hàng này đã ở trạng thái '{order.OrderStatus}', không thể hoàn trả.";
                    pnlWarning.Visibility = Visibility.Visible;
                    return;
                }

                _foundOrder = order;

                // Populate UI
                txtOrderCodeDisplay.Text = order.OrderCode;
                txtStatus.Text = order.OrderStatus ?? "N/A";
                txtRefundAmount.Text = $"{order.TotalAmount ?? 0:N0} đ";

                lstDetails.ItemsSource = order.OrderDetails.Select(d => new
                {
                    DisplayName = $"{d.ProductNameSnapshot ?? d.Product?.ProductName} x{d.Quantity}",
                    TotalPrice = d.TotalPrice ?? 0
                }).ToList();

                pnlOrderInfo.Tag = order; // Trigger visibility
                pnlOrderInfo.Visibility = Visibility.Visible;
                btnRefund.IsEnabled = true;
            }
            catch (Exception ex)
            {
                txtWarning.Text = $"Lỗi tìm kiếm: {ex.Message}";
                pnlWarning.Visibility = Visibility.Visible;
            }
        }

        private async void btnRefund_Click(object sender, RoutedEventArgs e)
        {
            if (_foundOrder == null) return;

            var confirm = MessageBox.Show(
                $"Xác nhận hoàn trả đơn {_foundOrder.OrderCode}?\n\nSố tiền hoàn lại cho khách: {_foundOrder.TotalAmount ?? 0:N0} đ\n\nHành động này sẽ nhập lại hàng vào kho và không thể hoàn tác.",
                "Xác Nhận Hoàn Trả", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                btnRefund.IsEnabled = false;
                await OrderService.Instance.RefundOrderAsync(_foundOrder.OrderId);

                MessageBox.Show(
                    $"✅ Hoàn trả thành công!\n\nĐơn {_foundOrder.OrderCode} đã được hủy.\nTồn kho đã được cập nhật.\nVui lòng hoàn trả {_foundOrder.TotalAmount ?? 0:N0} đ cho khách.",
                    "Hoàn Trả Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hoàn trả: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                btnRefund.IsEnabled = true;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

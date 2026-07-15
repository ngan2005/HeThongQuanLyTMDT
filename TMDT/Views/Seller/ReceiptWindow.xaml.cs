using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TMDT.Models;
using QuestPDF.Fluent;

namespace TMDT.Views.Seller
{
    public partial class ReceiptWindow : Window
    {
        private readonly Order _originalOrder;

        public ReceiptWindow(Order order, decimal givenAmount, decimal changeAmount)
        {
            InitializeComponent();
            _originalOrder = order;

            var vm = new ReceiptViewModel
            {
                OrderCode = order.OrderCode,
                OrderDate = order.OrderDate?.ToString("dd/MM/yyyy HH:mm") ?? "",
                OrderDetails = order.OrderDetails.ToList(),
                SubTotal = order.SubTotal ?? order.OrderDetails.Sum(d => d.TotalPrice ?? 0),
                Discount = order.Discount ?? 0,
                TotalAmount = order.TotalAmount ?? order.SubTotal ?? 0,
                PaymentMethod = order.PaymentMethod switch
                {
                    "POS_Cash" => "Tiền mặt",
                    "Cash" => "Tiền mặt",
                    "MoMo" => "MoMo",
                    "VNPay" => "VNPay",
                    _ => order.PaymentMethod ?? ""
                },
                GivenAmount = givenAmount,
                ChangeAmount = changeAmount,
                StaffName = TMDT.Utilities.SessionManager.CurrentUser?.FullName ?? "Thu ngân",
                // Tính điểm tích lũy: 1 điểm/10,000đ, chỉ hiển khi có BuyerId thật (không phải khách vãng lai)
                EarnedPoints = (order.BuyerId.HasValue && order.Buyer?.Email != "guest@pos.local")
                    ? (int)((order.TotalAmount ?? 0) / 10000)
                    : 0
            };

            DataContext = vm;
        }

        private void Header_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // In phần khung ReceiptPrintArea
                printDialog.PrintVisual(ReceiptPrintArea, "Hóa đơn POS - " + ((ReceiptViewModel)DataContext).OrderCode);
                MessageBox.Show("Đã gửi lệnh in hóa đơn thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var vm = (ReceiptViewModel)DataContext;
            
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "PDF Document (*.pdf)|*.pdf";
            saveFileDialog.FileName = $"HoaDon_{vm.OrderCode}_{System.DateTime.Now:yyyyMMdd}.pdf";
            saveFileDialog.Title = "Lưu Hóa Đơn PDF";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // QuestPDF Generate
                    var document = new TMDT.Documents.PosReceiptDocument(_originalOrder, vm.GivenAmount, vm.ChangeAmount, TMDT.Utilities.SessionManager.CurrentUser?.FullName ?? "Thu ngân");
                    document.GeneratePdf(saveFileDialog.FileName);

                    MessageBox.Show("Đã xuất hóa đơn PDF thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Mở file PDF lên
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                    
                    this.Close();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Lỗi khi xuất PDF: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class ReceiptViewModel
    {
        public string OrderCode { get; set; } = "";
        public string OrderDate { get; set; } = "";
        public System.Collections.Generic.List<OrderDetail> OrderDetails { get; set; } = new();
        
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
        
        public string PaymentMethod { get; set; } = "";
        public decimal GivenAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public string StaffName { get; set; } = "";
        public int EarnedPoints { get; set; }
        
        public Visibility DiscountVisibility => Discount > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CashVisibility => PaymentMethod == "Tiền mặt" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EarnedPointsVisibility => EarnedPoints > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}

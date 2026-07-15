using System.Windows;

namespace TMDT.Views.Components
{
    public partial class VNPayMockWindow : Window
    {
        public string TransactionCode { get; private set; } = string.Empty;

        /// <summary>True khi người dùng nhấn "Sửa đơn" — POS sẽ load đơn về tab để chỉnh sửa.</summary>
        public bool UserChoseToEdit { get; private set; }

        /// <summary>True khi cashier xác nhận offline (mạng lỗi) — POS sẽ lưu queue để sync sau.</summary>
        public bool UserChoseOffline { get; private set; }

        public VNPayMockWindow(decimal amount, string? orderCode = null) : this(amount)
        {
            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                txtOrderCode.Text = $"Mã đơn: {orderCode}";
                txtOrderCode.Visibility = Visibility.Visible;
            }
        }

        public VNPayMockWindow(decimal amount)
        {
            InitializeComponent();
            txtAmount.Text = $"{amount:N0} đ";
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            TransactionCode = $"VNP_{DateTime.Now.Ticks}";
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnEditOrder_Click(object sender, RoutedEventArgs e)
        {
            UserChoseToEdit = true;
            DialogResult = false;
            Close();
        }

        private void BtnOfflineConfirm_Click(object sender, RoutedEventArgs e)
        {
            UserChoseOffline = true;
            DialogResult = false;
            Close();
        }
    }
}

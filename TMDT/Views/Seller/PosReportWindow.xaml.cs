using System.Windows;

namespace TMDT.Views.Seller
{
    public partial class PosReportWindow : Window
    {
        public PosReportWindow(object dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(PrintArea, "Báo Cáo Kết Ca Z-Report");
                MessageBox.Show("Đã gửi lệnh in Z-Report thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }
    }
}

using System.Windows;
using TMDT.Utilities;

namespace TMDT.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Chỉ Buyer mới được vào trang này
            if (!SessionManager.IsBuyer)
            {
                MessageBox.Show("Bạn không có quyền truy cập trang này.", "Cảnh báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }
        }
    }
}

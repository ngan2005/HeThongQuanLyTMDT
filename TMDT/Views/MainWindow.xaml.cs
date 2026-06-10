using System.Windows;
using TMDT.ViewModels.Buyer;

namespace TMDT.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is BuyerMainViewModel vm)
                vm.Dispose();
            base.OnClosed(e);
        }
    }
}

using System.Windows.Controls;
using System.Windows.Input;

namespace TMDT.Views.Seller
{
    public partial class SellerWalletView : UserControl
    {
        public SellerWalletView()
        {
            InitializeComponent();
        }

        private void LightboxOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.Seller.SellerWalletViewModel vm)
            {
                vm.CloseDialogCommand.Execute(null);
            }
        }

        private void VNPayTab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.Seller.SellerWalletViewModel vm)
                vm.IsVNPayMethod = true;
        }

        private void BankTab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.Seller.SellerWalletViewModel vm)
                vm.IsVNPayMethod = false;
        }
    }
}

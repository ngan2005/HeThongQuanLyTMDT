using System.Windows.Controls;

namespace TMDT.Views.Seller
{
    public partial class SellerWalletView : UserControl
    {
        public SellerWalletView()
        {
            InitializeComponent();
        }

        private void LightboxOverlay_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.Seller.SellerWalletViewModel vm)
            {
                vm.CloseDialogCommand.Execute(null);
            }
        }
    }
}

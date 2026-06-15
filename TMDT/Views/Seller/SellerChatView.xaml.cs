using System.Windows.Controls;
using TMDT.ViewModels.Seller;

namespace TMDT.Views.Seller
{
    public partial class SellerChatView : UserControl
    {
        public SellerChatView()
        {
            InitializeComponent();
            DataContext = new SellerChatViewModel();
        }
    }
}

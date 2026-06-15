using System.Windows.Controls;
using TMDT.ViewModels.Seller;

namespace TMDT.Views.Seller
{
    public partial class SellerReturnRequestsView : UserControl
    {
        public SellerReturnRequestsView()
        {
            InitializeComponent();
            DataContext = new SellerReturnRequestsViewModel();
        }
    }
}

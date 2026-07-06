using System.Windows.Controls;
using TMDT.ViewModels.Seller;

namespace TMDT.Views.Seller
{
    public partial class SellerSalesHistoryView : UserControl
    {
        public SellerSalesHistoryView()
        {
            InitializeComponent();
            DataContext = new SellerSalesHistoryViewModel();
        }
    }
}

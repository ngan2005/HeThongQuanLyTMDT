using System.Windows.Controls;
using TMDT.ViewModels.Seller;

namespace TMDT.Views.Seller
{
    public partial class SellerReviewsView : UserControl
    {
        public SellerReviewsView()
        {
            InitializeComponent();
            DataContext = new SellerReviewsViewModel();
        }
    }
}

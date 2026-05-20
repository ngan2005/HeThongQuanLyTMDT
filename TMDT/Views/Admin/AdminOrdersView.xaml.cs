using System.Windows.Controls;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminOrdersView : UserControl
    {
        public AdminOrdersView()
        {
            InitializeComponent();
            DataContext = new AdminOrdersViewModel();
        }
    }
}

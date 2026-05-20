using System.Windows.Controls;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminProfileView : UserControl
    {
        public AdminProfileView()
        {
            InitializeComponent();
            DataContext = new AdminProfileViewModel();
        }
    }
}

using System.Windows.Controls;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminAuditLogsView : UserControl
    {
        public AdminAuditLogsView()
        {
            InitializeComponent();
            DataContext = new AdminAuditLogsViewModel();
        }
    }
}

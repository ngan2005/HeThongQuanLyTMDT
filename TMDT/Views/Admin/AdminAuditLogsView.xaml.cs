using System.Windows;
using System.Windows.Controls;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    /// <summary>
    /// Interaction logic for AdminAuditLogsView.xaml.
    /// DataContext is set by the parent AdminMainView's DataTemplate (via navigation),
    /// NOT here — this avoids creating a duplicate ViewModel instance.
    /// </summary>
    public partial class AdminAuditLogsView : UserControl
    {
        public AdminAuditLogsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Wire up any ViewModel events here if needed in the future
        }
    }
}

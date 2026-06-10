using System.Windows.Controls;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    /// <summary>
    /// Interaction logic for AdminSettingsView.xaml.
    /// DataContext is set by the parent AdminMainView's DataTemplate (via navigation),
    /// NOT here — this avoids creating a duplicate ViewModel instance.
    /// </summary>
    public partial class AdminSettingsView : UserControl
    {
        public AdminSettingsView()
        {
            InitializeComponent();
        }
    }
}

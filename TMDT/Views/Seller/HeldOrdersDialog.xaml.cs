using System.Collections.ObjectModel;
using System.Windows;
using TMDT.ViewModels.Seller;

namespace TMDT.Views.Seller
{
    public partial class HeldOrdersDialog : Window
    {
        public HeldOrderSnapshot? SelectedSnapshot { get; private set; }

        public HeldOrdersDialog(ObservableCollection<HeldOrderSnapshot> snapshots)
        {
            InitializeComponent();
            DataContext = snapshots;
        }

        private void Resume_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is HeldOrderSnapshot snap)
            {
                SelectedSnapshot = snap;
                DialogResult = true;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
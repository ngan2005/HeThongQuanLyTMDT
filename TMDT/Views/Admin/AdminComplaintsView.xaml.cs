using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TMDT.Models;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminComplaintsView : UserControl
    {
        private bool _isHiding = false; // Guard flag to prevent re-entrant HideLightbox calls

        public AdminComplaintsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminComplaintsViewModel vm)
            {
                vm.ShowDetailRequest += ShowLightbox;
                vm.HideDetailRequest += HideLightbox;
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Complaint complaint && DataContext is AdminComplaintsViewModel vm)
            {
                vm.SelectedComplaint = complaint;
                ShowLightbox();
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only update the ViewModel selection; do NOT open the lightbox here.
            // Opening is done via double-click or the "Chi tiết" button.
            if (sender is DataGrid dg && dg.SelectedItem is Complaint complaint && DataContext is AdminComplaintsViewModel vm)
            {
                // Suppress HideDetailRequest side-effect when just changing selection
                _isHiding = true;
                vm.SelectedComplaint = complaint;
                _isHiding = false;
            }
        }

        private void ViewComplaint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Complaint complaint && DataContext is AdminComplaintsViewModel vm)
            {
                vm.SelectedComplaint = complaint;
                ShowLightbox();
            }
        }

        private void ShowLightbox()
        {
            LightboxOverlay.Visibility = Visibility.Visible;
        }

        private void HideLightbox()
        {
            if (_isHiding) return; // Prevent infinite recursion
            _isHiding = true;
            try
            {
                LightboxOverlay.Visibility = Visibility.Collapsed;
                if (DataContext is AdminComplaintsViewModel vm)
                {
                    vm.SelectedComplaint = null;
                }
            }
            finally
            {
                _isHiding = false;
            }
        }

        private void CloseLightbox(object sender, RoutedEventArgs e)
        {
            HideLightbox();
        }

        private void LightboxOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == LightboxOverlay)
                HideLightbox();
        }

        private void FilterPill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && DataContext is AdminComplaintsViewModel vm)
            {
                vm.StatusFilter = rb.Tag?.ToString() ?? "All";
            }
        }
    }
}

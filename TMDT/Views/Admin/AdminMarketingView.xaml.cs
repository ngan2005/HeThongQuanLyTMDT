using System.Windows;
using System.Windows.Controls;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    /// <summary>
    /// Interaction logic for AdminMarketingView.xaml
    /// </summary>
    public partial class AdminMarketingView : UserControl
    {
        private AdminMarketingViewModel _viewModel;

        public AdminMarketingView()
        {
            InitializeComponent();
            DataContextChanged += AdminMarketingView_DataContextChanged;
        }

        private void AdminMarketingView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ShowDetailRequest -= OnShowDetail;
                _viewModel.HideDetailRequest -= OnHideDetail;
            }

            _viewModel = e.NewValue as AdminMarketingViewModel;

            if (_viewModel != null)
            {
                _viewModel.ShowDetailRequest += OnShowDetail;
                _viewModel.HideDetailRequest += OnHideDetail;
            }
        }

        private void OnShowDetail()
        {
            LightboxOverlay.Visibility = Visibility.Visible;
        }

        private void OnHideDetail()
        {
            LightboxOverlay.Visibility = Visibility.Collapsed;
        }

        private void OnRowDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LightboxOverlay.Visibility != Visibility.Visible)
            {
                LightboxOverlay.Visibility = Visibility.Visible;
            }
        }
    }
}

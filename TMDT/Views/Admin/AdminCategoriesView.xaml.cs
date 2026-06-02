using System.Windows;
using System.Windows.Controls;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    /// <summary>
    /// Interaction logic for AdminCategoriesView.xaml
    /// </summary>
    public partial class AdminCategoriesView : UserControl
    {
        private AdminCategoriesViewModel _viewModel;

        public AdminCategoriesView()
        {
            InitializeComponent();
            DataContextChanged += AdminCategoriesView_DataContextChanged;
        }

        private void AdminCategoriesView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ShowDetailRequest -= OnShowDetail;
                _viewModel.HideDetailRequest -= OnHideDetail;
            }

            _viewModel = e.NewValue as AdminCategoriesViewModel;

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

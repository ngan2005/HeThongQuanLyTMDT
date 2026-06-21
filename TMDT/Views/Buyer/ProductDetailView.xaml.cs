using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TMDT.Messages;
using TMDT.ViewModels.Buyer;

namespace TMDT.Views.Buyer
{
    public partial class ProductDetailView : UserControl
    {
        private ProductDetailViewModel _viewModel;

        public ProductDetailView()
        {
            InitializeComponent();
            this.DataContextChanged += ProductDetailView_DataContextChanged;
        }

        private void ProductDetailView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.AddedToCart -= ViewModel_AddedToCart;
            }

            if (this.DataContext is ProductDetailViewModel vm)
            {
                _viewModel = vm;
                _viewModel.AddedToCart += ViewModel_AddedToCart;
            }
        }

        private void ViewModel_AddedToCart()
        {
            // Execute on UI thread
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.MainImageUrl))
                {
                    Point absolutePos = MainProductImage.PointToScreen(new Point(0, 0));
                    Rect sourceRect = new Rect(absolutePos.X, absolutePos.Y, MainProductImage.ActualWidth, MainProductImage.ActualHeight);

                    MessageBus.SendFlyToCart(new FlyToCartMessage(_viewModel.MainImageUrl, sourceRect));
                }
            });
        }

        private void MainProductImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LightboxOverlay.Visibility = Visibility.Visible;
        }

        private void LightboxOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LightboxOverlay.Visibility = Visibility.Collapsed;
        }

        private void CloseLightbox_Click(object sender, RoutedEventArgs e)
        {
            LightboxOverlay.Visibility = Visibility.Collapsed;
        }
    }
}

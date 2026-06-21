using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TMDT.Messages;

namespace TMDT.Views.Buyer
{
    public partial class BuyerMainView : UserControl
    {
        public BuyerMainView()
        {
            InitializeComponent();
            this.Loaded += BuyerMainView_Loaded;
            this.Unloaded += BuyerMainView_Unloaded;
        }

        private void BuyerMainView_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainSnackbar.MessageQueue == null)
            {
                MainSnackbar.MessageQueue = new MaterialDesignThemes.Wpf.SnackbarMessageQueue(TimeSpan.FromSeconds(3));
            }
            MessageBus.OnFlyToCart += MessageBus_OnFlyToCart;
            MessageBus.OnToastMessage += MessageBus_OnToastMessage;
        }

        private void BuyerMainView_Unloaded(object sender, RoutedEventArgs e)
        {
            MessageBus.OnFlyToCart -= MessageBus_OnFlyToCart;
            MessageBus.OnToastMessage -= MessageBus_OnToastMessage;
        }

        private void MessageBus_OnFlyToCart(FlyToCartMessage m)
        {
            Application.Current.Dispatcher.InvokeAsync(() => HandleFlyToCart(m));
        }

        private void MessageBus_OnToastMessage(ToastMessage m)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MainSnackbar.MessageQueue?.Enqueue(m.Message);
            });
        }

        private void HandleFlyToCart(FlyToCartMessage message)
        {
            try
            {
                if (string.IsNullOrEmpty(message.SourceImageUrl)) return;

                // Center of the cart icon
                Point cartCenterScreen = CartIconArea.PointToScreen(new Point(CartIconArea.ActualWidth / 2, CartIconArea.ActualHeight / 2));
                Point cartCenterCanvas = AnimationOverlay.PointFromScreen(cartCenterScreen);
                
                // Target coordinates for Canvas.Left and Top so that the center of the image lands here
                double targetLeft = cartCenterCanvas.X - (message.SourceRect.Width / 2);
                double targetTop = cartCenterCanvas.Y - (message.SourceRect.Height / 2);

                // Get the source point relative to the screen, then map to our Canvas
                Point sourcePointScreen = message.SourceRect.TopLeft;
                Point sourcePointCanvas = AnimationOverlay.PointFromScreen(sourcePointScreen);
                
                string uriString = message.SourceImageUrl;
                if (uriString.StartsWith("/"))
                {
                    uriString = "pack://application:,,," + uriString;
                }

                // Create image control
                Image animatedImage = new Image
                {
                    Source = new BitmapImage(new Uri(uriString, UriKind.RelativeOrAbsolute)),
                    Width = message.SourceRect.Width,
                    Height = message.SourceRect.Height,
                    Stretch = Stretch.Uniform
                };

                Canvas.SetLeft(animatedImage, sourcePointCanvas.X);
                Canvas.SetTop(animatedImage, sourcePointCanvas.Y);

                AnimationOverlay.Children.Add(animatedImage);

                // Setup animation storyboard
                Storyboard storyboard = new Storyboard();

                // Move X
                DoubleAnimation moveX = new DoubleAnimation
                {
                    To = targetLeft,
                    Duration = TimeSpan.FromSeconds(0.7),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(moveX, animatedImage);
                Storyboard.SetTargetProperty(moveX, new PropertyPath(Canvas.LeftProperty));
                storyboard.Children.Add(moveX);

                // Move Y
                DoubleAnimation moveY = new DoubleAnimation
                {
                    To = targetTop,
                    Duration = TimeSpan.FromSeconds(0.7),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(moveY, animatedImage);
                Storyboard.SetTargetProperty(moveY, new PropertyPath(Canvas.TopProperty));
                storyboard.Children.Add(moveY);

                // Scale X & Y (Shrink)
                ScaleTransform scaleTransform = new ScaleTransform(1, 1);
                animatedImage.RenderTransform = scaleTransform;
                animatedImage.RenderTransformOrigin = new Point(0.5, 0.5);

                DoubleAnimation scaleAnim = new DoubleAnimation
                {
                    To = 0.05,
                    Duration = TimeSpan.FromSeconds(0.7),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(scaleAnim, animatedImage);
                Storyboard.SetTargetProperty(scaleAnim, new PropertyPath("RenderTransform.ScaleX"));
                storyboard.Children.Add(scaleAnim);

                DoubleAnimation scaleYAnim = new DoubleAnimation
                {
                    To = 0.05,
                    Duration = TimeSpan.FromSeconds(0.7),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(scaleYAnim, animatedImage);
                Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("RenderTransform.ScaleY"));
                storyboard.Children.Add(scaleYAnim);

                // Fade out at the end
                DoubleAnimation opacityAnim = new DoubleAnimation
                {
                    To = 0,
                    BeginTime = TimeSpan.FromSeconds(0.5),
                    Duration = TimeSpan.FromSeconds(0.2)
                };
                Storyboard.SetTarget(opacityAnim, animatedImage);
                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(opacityAnim);

                // Cleanup after animation completes
                storyboard.Completed += (s, e) =>
                {
                    AnimationOverlay.Children.Remove(animatedImage);
                };

                storyboard.Begin();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Animation error: " + ex.Message);
            }
        }
    }
}

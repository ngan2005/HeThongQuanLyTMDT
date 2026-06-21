using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TMDT.Utilities;

namespace TMDT.Views.Components
{
    public partial class ProductCard : UserControl
    {
        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.Register(nameof(ClickCommand), typeof(ICommand), typeof(ProductCard));

        public static readonly DependencyProperty ClickCommandParameterProperty =
            DependencyProperty.Register(nameof(ClickCommandParameter), typeof(object), typeof(ProductCard));

        public static readonly DependencyProperty WishlistCommandProperty =
            DependencyProperty.Register(nameof(WishlistCommand), typeof(ICommand), typeof(ProductCard));

        public static readonly DependencyProperty WishlistCommandParameterProperty =
            DependencyProperty.Register(nameof(WishlistCommandParameter), typeof(object), typeof(ProductCard));

        public static readonly DependencyProperty IsWishlistedProperty =
            DependencyProperty.Register(nameof(IsWishlisted), typeof(bool), typeof(ProductCard),
                new PropertyMetadata(false));

        public static readonly RoutedEvent WishlistToggledEvent =
            EventManager.RegisterRoutedEvent(nameof(WishlistToggled), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(ProductCard));

        public ICommand ClickCommand
        {
            get => (ICommand)GetValue(ClickCommandProperty);
            set => SetValue(ClickCommandProperty, value);
        }

        public object ClickCommandParameter
        {
            get => GetValue(ClickCommandParameterProperty);
            set => SetValue(ClickCommandParameterProperty, value);
        }

        public ICommand WishlistCommand
        {
            get => (ICommand)GetValue(WishlistCommandProperty);
            set => SetValue(WishlistCommandProperty, value);
        }

        public object WishlistCommandParameter
        {
            get => GetValue(WishlistCommandParameterProperty);
            set => SetValue(WishlistCommandParameterProperty, value);
        }

        public bool IsWishlisted
        {
            get => (bool)GetValue(IsWishlistedProperty);
            set => SetValue(IsWishlistedProperty, value);
        }

        public event RoutedEventHandler WishlistToggled
        {
            add => AddHandler(WishlistToggledEvent, value);
            remove => RemoveHandler(WishlistToggledEvent, value);
        }

        public ProductCard()
        {
            InitializeComponent();
        }

        private void CardBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ClickCommand?.CanExecute(ClickCommandParameter) == true)
                ClickCommand.Execute(ClickCommandParameter);
        }

        private void HeartToggle_Click(object sender, RoutedEventArgs e)
        {
            if (WishlistCommand?.CanExecute(WishlistCommandParameter) == true)
                WishlistCommand.Execute(WishlistCommandParameter);
            else if (!SessionManager.IsLoggedIn)
                MessageBox.Show("Vui lòng đăng nhập để thêm sản phẩm yêu thích.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

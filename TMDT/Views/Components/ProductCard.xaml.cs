using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TMDT.Views.Components
{
    public partial class ProductCard : UserControl
    {
        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.Register(nameof(ClickCommand), typeof(ICommand), typeof(ProductCard));

        public static readonly DependencyProperty ClickCommandParameterProperty =
            DependencyProperty.Register(nameof(ClickCommandParameter), typeof(object), typeof(ProductCard));

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

        public ProductCard()
        {
            InitializeComponent();
        }

        private void CardBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ClickCommand?.CanExecute(ClickCommandParameter) == true)
                ClickCommand.Execute(ClickCommandParameter);
        }
    }
}

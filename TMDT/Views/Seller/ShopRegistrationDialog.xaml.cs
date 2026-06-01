using System.Windows;
using System.Windows.Input;

namespace TMDT.Views.Seller
{
    public partial class ShopRegistrationDialog : Window
    {
        public bool RegistrationSucceeded { get; private set; }

        public ShopRegistrationDialog()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (DataContext is ViewModels.Seller.ShopRegistrationViewModel vm)
                {
                    vm.RequestClose += success =>
                    {
                        RegistrationSucceeded = success;
                        Close();
                    };
                }
            };
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

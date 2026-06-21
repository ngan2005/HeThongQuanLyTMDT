using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TMDT.ViewModels.Buyer;

namespace TMDT.Views.Buyer
{
    public partial class WishlistView : UserControl
    {
        public WishlistView()
        {
            InitializeComponent();
        }

        private void CardBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is WishlistItem item)
            {
                if (item.Product != null && DataContext is WishlistViewModel vm)
                {
                    vm.OpenProductCommand.Execute(item.Product);
                }
            }
        }
    }
}

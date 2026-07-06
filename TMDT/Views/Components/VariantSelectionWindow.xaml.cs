using System.Collections.Generic;
using System.Windows;
using TMDT.Models;
using System.Linq;

namespace TMDT.Views.Components
{
    public partial class VariantSelectionWindow : Window
    {
        public ProductVariant? SelectedVariant { get; private set; }

        public VariantSelectionWindow(IEnumerable<ProductVariant> variants)
        {
            InitializeComponent();
            DataContext = variants.ToList();
            btnConfirm.IsEnabled = false;
        }

        private void lstVariants_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lstVariants.SelectedItem is ProductVariant variant)
            {
                SelectedVariant = variant;
                btnConfirm.IsEnabled = (variant.Quantity ?? 0) > 0;
            }
            else
            {
                SelectedVariant = null;
                btnConfirm.IsEnabled = false;
            }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedVariant != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

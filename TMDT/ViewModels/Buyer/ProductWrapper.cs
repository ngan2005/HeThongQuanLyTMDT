using TMDT.Models;

namespace TMDT.ViewModels.Buyer
{
    public class ProductWrapper : ViewModelBase
    {
        private bool _isWishlisted;

        public Product Product { get; }

        public bool IsWishlisted
        {
            get => _isWishlisted;
            set => SetProperty(ref _isWishlisted, value);
        }

        public ProductWrapper(Product product, bool isWishlisted = false)
        {
            Product = product;
            IsWishlisted = isWishlisted;
        }
    }
}

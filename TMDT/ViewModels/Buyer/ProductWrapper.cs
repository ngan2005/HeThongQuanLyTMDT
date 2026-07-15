using TMDT.Models;

namespace TMDT.ViewModels.Buyer
{
    public class ProductWrapper : ViewModelBase
    {
        private bool _isWishlisted;
        private bool _isCompared;

        public Product Product { get; }

        public bool IsWishlisted
        {
            get => _isWishlisted;
            set => SetProperty(ref _isWishlisted, value);
        }

        public bool IsCompared
        {
            get => _isCompared;
            set => SetProperty(ref _isCompared, value);
        }

        public ProductWrapper(Product product, bool isWishlisted = false, bool isCompared = false)
        {
            Product = product;
            IsWishlisted = isWishlisted;
            IsCompared = isCompared;
        }
    }
}

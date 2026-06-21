using TMDT.Models;

namespace TMDT.ViewModels.Buyer
{
    public class ProductWrapper
    {
        public Product Product { get; }
        public bool IsWishlisted { get; set; }

        public ProductWrapper(Product product, bool isWishlisted = false)
        {
            Product = product;
            IsWishlisted = isWishlisted;
        }
    }
}

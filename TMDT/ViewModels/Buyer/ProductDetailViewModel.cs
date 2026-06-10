using System;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class ProductDetailViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private readonly Product _product;

        private int _quantity = 1;
        private string _selectedImageUrl = "";

        public Product Product => _product;
        public string ProductName => _product.ProductName;
        public decimal Price => _product.Price;
        public decimal? OriginalPrice => _product.OriginalPrice;
        public string? Description => _product.Description;
        public int StockQuantity => _product.StockQuantity ?? 0;
        public int SoldCount => _product.SoldCount ?? 0;
        public decimal? Rating => _product.Rating;
        public string? CategoryName => _product.Category?.CategoryName;
        public string? ShopName => _product.Shop?.ShopName;
        public int ShopId => _product.ShopId ?? 0;

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value < 1) value = 1;
                if (value > StockQuantity) value = StockQuantity;
                SetProperty(ref _quantity, value);
            }
        }

        public int DiscountPercent => OriginalPrice.HasValue && OriginalPrice.Value > 0
            ? (int)Math.Round((1 - Price / OriginalPrice.Value) * 100)
            : 0;

        public bool IsInStock => StockQuantity > 0;

        public ICommand AddToCartCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }

        public event Action? AddedToCart;

        public ProductDetailViewModel(Product product, BuyerMainViewModel mainVm)
        {
            _product = product;
            _mainVm = mainVm;

            AddToCartCommand = new RelayCommand(_ => ExecuteAddToCart());
            BackCommand = new RelayCommand(_ => ExecuteBack());
            IncreaseCommand = new RelayCommand(_ => Quantity++);
            DecreaseCommand = new RelayCommand(_ => Quantity--, _ => Quantity > 1);
        }

        private void ExecuteAddToCart()
        {
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (StockQuantity <= 0)
            {
                MessageBox.Show("Sản phẩm đã hết hàng.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CartService.Instance.AddProduct(_product, Quantity);
            MessageBox.Show($"Đã thêm {Quantity} sản phẩm '{ProductName}' vào giỏ hàng!",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            AddedToCart?.Invoke();
        }

        private void ExecuteBack()
        {
            _mainVm.NavigateHome();
        }
    }
}

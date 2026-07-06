using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;
using TMDT.Messages;
using Microsoft.EntityFrameworkCore;

namespace TMDT.ViewModels.Buyer
{
    public class ProductDetailViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private readonly Product _product;

        private int _quantity = 1;
        private string? _mainImageUrl;
        private bool _isWishlisted;

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

        public string? MainImageUrl 
        {
            get => _mainImageUrl;
            set => SetProperty(ref _mainImageUrl, value);
        }

        public ObservableCollection<string> Thumbnails { get; } = new();
        public ObservableCollection<TMDT.ViewModels.Seller.ReviewItem> Reviews { get; } = new();

        public bool IsWishlisted
        {
            get => _isWishlisted;
            set => SetProperty(ref _isWishlisted, value);
        }

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

        public ICommand AddToCartCommand { get; } = null!;
        public ICommand BackCommand { get; } = null!;
        public ICommand IncreaseCommand { get; } = null!;
        public ICommand DecreaseCommand { get; } = null!;
        public ICommand SelectImageCommand { get; } = null!;
        public ICommand ViewShopCommand { get; } = null!;
        public ICommand ToggleWishlistCommand { get; } = null!;
        public ICommand ShareCommand { get; } = null!;
        public ICommand ShowBarcodeCommand { get; } = null!;

        public event Action? AddedToCart;

        public ProductDetailViewModel(Product product, BuyerMainViewModel mainVm)
        {
            _product = product;
            _mainVm = mainVm;

            AddToCartCommand = new RelayCommand(_ => ExecuteAddToCart());
            BackCommand = new RelayCommand(_ => ExecuteBack());
            IncreaseCommand = new RelayCommand(_ => Quantity++);
            DecreaseCommand = new RelayCommand(_ => Quantity--, _ => Quantity > 1);
            SelectImageCommand = new RelayCommand(url => MainImageUrl = url as string);
            ViewShopCommand = new RelayCommand(_ => {
                if (ShopId > 0)
                    _mainVm.NavigateShop(ShopId);
            });
            ToggleWishlistCommand = new RelayCommand(_ => ExecuteToggleWishlist());
            ShareCommand = new RelayCommand(_ => ExecuteShare());
            ShowBarcodeCommand = new RelayCommand(_ => ExecuteShowBarcode());

            var mainImg = _product.ProductImages?.FirstOrDefault(i => i.IsMain == true) 
                       ?? _product.ProductImages?.FirstOrDefault();
            _mainImageUrl = mainImg?.ImageUrl;

            if (_product.ProductImages != null)
            {
                foreach (var img in _product.ProductImages.OrderBy(i => i.SortOrder))
                {
                    if (!string.IsNullOrEmpty(img.ImageUrl))
                    {
                        Thumbnails.Add(img.ImageUrl);
                    }
                }
            }

            CheckWishlistStatus();
            LoadReviews();
        }

        private void ExecuteShowBarcode()
        {
            if (_product == null) return;
            var dlg = new TMDT.Views.Seller.BarcodeDialog
            {
                DataContext = new TMDT.ViewModels.Seller.BarcodeViewModel(_product),
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();
        }

        private void CheckWishlistStatus()
        {
            if (!SessionManager.IsLoggedIn) return;
            try
            {
                using var ctx = new TmdtContext();
                IsWishlisted = ctx.Wishlists.Any(w => w.UserId == SessionManager.CurrentUser!.UserId && w.ProductId == _product.ProductId);
            }
            catch { }
        }

        private void ExecuteToggleWishlist()
        {
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập để thêm sản phẩm yêu thích.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var ctx = new TmdtContext();
                var existing = ctx.Wishlists.FirstOrDefault(w => w.UserId == SessionManager.CurrentUser!.UserId && w.ProductId == _product.ProductId);
                if (existing != null)
                {
                    ctx.Wishlists.Remove(existing);
                    ctx.SaveChanges();
                    IsWishlisted = false;
                }
                else
                {
                    ctx.Wishlists.Add(new Wishlist
                    {
                        UserId = SessionManager.CurrentUser!.UserId,
                        ProductId = _product.ProductId,
                        AddedAt = DateTime.Now
                    });
                    ctx.SaveChanges();
                    IsWishlisted = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Toggle wishlist failed: " + ex.Message);
            }
        }

        private void ExecuteShare()
        {
            try
            {
                var shareText = $"Xem ngay '{ProductName}' giá chỉ {Price:N0}đ trên Volox!\n";
                Clipboard.SetText(shareText);
                MessageBox.Show("Đã lưu nội dung chia sẻ vào bộ nhớ tạm (Clipboard)!\nBạn có thể dán (Ctrl+V) vào Facebook/Zalo để gửi cho bạn bè.", 
                                "Chia sẻ thành công", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Information);
            }
            catch { }
        }

        private void LoadReviews()
        {
            try
            {
                using var ctx = new TmdtContext();
                var list = ctx.Reviews
                    .Include(r => r.User)
                    .Include(r => r.ReviewReplies)
                    .Where(r => r.ProductId == _product.ProductId && r.IsHidden != true)
                    .OrderByDescending(r => r.ReviewedAt)
                    .ToList();

                Reviews.Clear();
                foreach (var r in list)
                {
                    Reviews.Add(new TMDT.ViewModels.Seller.ReviewItem { ReviewData = r });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load reviews failed: " + ex.Message);
            }
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

            CartService.Instance.AddProduct(_product, null, Quantity);
            MessageBus.SendToast($"Đã thêm {Quantity} sản phẩm '{ProductName}' vào giỏ hàng!");
            AddedToCart?.Invoke();
        }

        private void ExecuteBack()
        {
            _mainVm.NavigateHome();
        }
    }
}

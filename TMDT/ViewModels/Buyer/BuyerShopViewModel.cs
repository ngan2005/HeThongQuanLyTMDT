using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerShopViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private readonly int _shopId;

        private Shop? _shop;
        private string? _shopName;
        private string? _shopLogoChar;
        private decimal? _rating;
        private DateTime? _openedAt;
        private int _totalProducts;
        private string? _warehouseAddress;
        private bool _isFollowed;

        public string? ShopName
        {
            get => _shopName;
            set => SetProperty(ref _shopName, value);
        }

        public string? ShopLogoChar
        {
            get => _shopLogoChar;
            set => SetProperty(ref _shopLogoChar, value);
        }

        public decimal? Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        public DateTime? OpenedAt
        {
            get => _openedAt;
            set => SetProperty(ref _openedAt, value);
        }

        public int TotalProducts
        {
            get => _totalProducts;
            set => SetProperty(ref _totalProducts, value);
        }

        public string? WarehouseAddress
        {
            get => _warehouseAddress;
            set => SetProperty(ref _warehouseAddress, value);
        }

        public bool IsFollowed
        {
            get => _isFollowed;
            set => SetProperty(ref _isFollowed, value);
        }

        public ObservableCollection<ProductWrapper> Products { get; } = new();

        public ICommand OpenProductCommand { get; }
        public ICommand ToggleWishlistCommand { get; }
        public ICommand ChatCommand { get; }
        public ICommand ToggleFollowCommand { get; }

        public BuyerShopViewModel(int shopId, BuyerMainViewModel mainVm)
        {
            _shopId = shopId;
            _mainVm = mainVm;

            OpenProductCommand = new RelayCommand(p => _mainVm.NavigateProductDetail(p is ProductWrapper w ? w.Product : p as Product));
            ToggleWishlistCommand = new RelayCommand(p => ExecuteToggleWishlist(p as ProductWrapper));
            ChatCommand = new RelayCommand(_ => ExecuteChat());
            ToggleFollowCommand = new RelayCommand(_ => IsFollowed = !IsFollowed); // Dummy for now

            LoadShopData();
        }

        private async void LoadShopData()
        {
            try
            {
                using var ctx = new TmdtContext();
                _shop = await ctx.Shops
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ShopId == _shopId);

                if (_shop != null)
                {
                    ShopName = _shop.ShopName;
                    ShopLogoChar = !string.IsNullOrEmpty(_shop.ShopName) ? _shop.ShopName.Substring(0, 1).ToUpper() : "S";
                    Rating = _shop.Rating ?? 0;
                    OpenedAt = _shop.OpenedAt;
                    WarehouseAddress = _shop.WarehouseAddress ?? "Chưa cập nhật";
                    
                    var products = await ctx.Products
                        .AsNoTracking()
                        .Include(p => p.ProductImages)
                        .Where(p => p.ShopId == _shopId && p.Status == "Approved")
                        .OrderByDescending(p => p.SoldCount)
                        .ToListAsync();

                    TotalProducts = products.Count;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Products.Clear();
                        foreach (var p in products)
                        {
                            bool inWishlist = SessionManager.IsLoggedIn
                                && ctx.Wishlists.Any(w => w.UserId == SessionManager.CurrentUser!.UserId && w.ProductId == p.ProductId);
                            Products.Add(new ProductWrapper(p, inWishlist));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load shop data: {ex.Message}");
            }
        }

        private void ExecuteToggleWishlist(ProductWrapper? wrapper)
        {
            if (wrapper == null) return;
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập để thêm sản phẩm yêu thích.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var ctx = new TmdtContext();
                var existing = ctx.Wishlists.FirstOrDefault(w => w.UserId == SessionManager.CurrentUser!.UserId && w.ProductId == wrapper.Product.ProductId);
                if (existing != null)
                {
                    ctx.Wishlists.Remove(existing);
                    ctx.SaveChanges();
                    wrapper.IsWishlisted = false;
                }
                else
                {
                    ctx.Wishlists.Add(new Wishlist
                    {
                        UserId = SessionManager.CurrentUser!.UserId,
                        ProductId = wrapper.Product.ProductId,
                        AddedAt = DateTime.Now
                    });
                    ctx.SaveChanges();
                    wrapper.IsWishlisted = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Toggle wishlist failed: " + ex.Message);
            }
        }

        private void ExecuteChat()
        {
            if (_shop != null)
            {
                _ = _mainVm.ChatViewModel.OpenChatWithShopAsync(_shop.ShopId);
            }
        }
    }
}

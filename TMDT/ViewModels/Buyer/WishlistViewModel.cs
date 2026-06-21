using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class WishlistViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private bool _isLoading;

        public ObservableCollection<WishlistItem> Items { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set { SetProperty(ref _isLoading, value); }
        }

        public bool IsEmpty => Items.Count == 0;

        public ICommand RefreshCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand AddToCartCommand { get; }
        public ICommand OpenProductCommand { get; }
        public ICommand BackCommand { get; }

        public WishlistViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;

            RefreshCommand = new RelayCommand(_ => _ = LoadWishlistAsync());
            RemoveCommand = new RelayCommand(w => ExecuteRemove(w as WishlistItem));
            AddToCartCommand = new RelayCommand(w => ExecuteAddToCart(w as WishlistItem));
            OpenProductCommand = new RelayCommand(p => _mainVm.NavigateProductDetail(p as Product));
            BackCommand = new RelayCommand(_ => _mainVm.NavigateHome());

            _ = LoadWishlistAsync();
        }

        private async Task LoadWishlistAsync()
        {
            if (!SessionManager.IsLoggedIn) return;

            IsLoading = true;
            try
            {
                using var ctx = new TmdtContext();
                var list = await ctx.Wishlists
                    .AsNoTracking()
                    .Include(w => w.Product).ThenInclude(p => p!.Shop)
                    .Include(w => w.Product).ThenInclude(p => p!.ProductImages)
                    .Where(w => w.UserId == SessionManager.CurrentUser!.UserId)
                    .OrderByDescending(w => w.AddedAt)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    foreach (var w in list)
                    {
                        if (w.Product != null)
                            Items.Add(new WishlistItem
                            {
                                WishlistId = w.WishlistId,
                                Product = w.Product,
                                AddedAt = w.AddedAt ?? DateTime.Now
                            });
                    }
                    OnPropertyChanged(nameof(IsEmpty));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load wishlist failed: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteRemove(WishlistItem? item)
        {
            if (item == null) return;

            var result = MessageBox.Show(
                $"Xóa '{item.Product?.ProductName}' khỏi danh sách yêu thích?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new TmdtContext();
                var dbItem = ctx.Wishlists.Find(item.WishlistId);
                if (dbItem != null)
                {
                    ctx.Wishlists.Remove(dbItem);
                    ctx.SaveChanges();
                }

                Items.Remove(item);
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch { }
        }

        private void ExecuteAddToCart(WishlistItem? item)
        {
            if (item?.Product == null) return;

            var product = item.Product;

            if (product.Status != "Approved")
            {
                MessageBox.Show("Sản phẩm này hiện không khả dụng.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (product.Shop != null && product.Shop.IsActive == false)
            {
                MessageBox.Show("Cửa hàng đang bị tạm khóa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if ((product.StockQuantity ?? 0) <= 0)
            {
                MessageBox.Show("Sản phẩm đã hết hàng.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CartService.Instance.AddProduct(product, 1);

            var result = MessageBox.Show(
                $"Đã thêm '{product.ProductName}' vào giỏ hàng.\n\nBạn có muốn xóa khỏi danh sách yêu thích?",
                "Thành công", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                ExecuteRemove(item);
        }
    }

    public class WishlistItem
    {
        public int WishlistId { get; set; }
        public Product? Product { get; set; }
        public DateTime AddedAt { get; set; }

        public string AddedAtText => AddedAt.ToString("dd/MM/yyyy");

        public string? ImageUrl =>
            Product?.ProductImages?.FirstOrDefault(i => i.IsMain == true)?.ImageUrl
            ?? Product?.ProductImages?.FirstOrDefault()?.ImageUrl;

        public decimal DiscountPercent
        {
            get
            {
                if (!Product?.OriginalPrice.HasValue ?? true || Product.OriginalPrice.Value <= 0)
                    return 0;
                if (Product.Price >= Product.OriginalPrice.Value)
                    return 0;
                return (int)Math.Round((1 - Product.Price / Product.OriginalPrice.Value) * 100);
            }
        }
    }
}

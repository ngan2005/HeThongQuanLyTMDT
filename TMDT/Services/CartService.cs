using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TMDT.Models;
using TMDT.Utilities;
using Microsoft.EntityFrameworkCore;

namespace TMDT.Services
{
    public class CartService
    {
        private static CartService? _instance;
        private static readonly object _lock = new();

        public static CartService Instance
        {
            get
            {
                lock (_lock)
                {
                    _instance ??= new CartService();
                    return _instance;
                }
            }
        }

        public ObservableCollection<CartItem> Items { get; } = new();

        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal TotalPrice => Items.Sum(i => i.LineTotal);

        public event Action? CartChanged;

        private CartService() { }

        public void AddProduct(Product product, ProductVariant? variant = null, int quantity = 1)
        {
            if (product == null) return;

            lock (_lock)
            {
                var existing = Items.FirstOrDefault(i => i.ProductId == product.ProductId && i.VariantId == variant?.VariantId);
                if (quantity <= 0) return;

                int maxStock = variant != null ? (variant.Quantity ?? 0) : (product.StockQuantity ?? 0);
                int finalQuantity = Math.Min(quantity, maxStock);

                if (existing != null)
                {
                    int newQuantity = existing.Quantity + finalQuantity;
                    if (newQuantity > maxStock)
                        newQuantity = maxStock;
                    
                    existing.Quantity = newQuantity;
                }
                else
                {
                    decimal price = product.Price + (variant?.ExtraPrice ?? 0);
                    Items.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        VariantId = variant?.VariantId,
                        VariantName = variant?.VariantName,
                        Price = price,
                        OriginalPrice = product.OriginalPrice,
                        ImageUrl = product.MainImageUrl,
                        StockQuantity = maxStock,
                        Quantity = finalQuantity,
                        ShopId = product.ShopId ?? 0
                    });
                }
            }
            SaveToDatabase();
            OnCartChanged();
        }

        public void RemoveProduct(int productId, int? variantId = null)
        {
            lock (_lock)
            {
                var item = Items.FirstOrDefault(i => i.ProductId == productId && i.VariantId == variantId);
                if (item != null)
                    Items.Remove(item);
            }
            SaveToDatabase();
            OnCartChanged();
        }

        public void UpdateQuantity(int productId, int? variantId, int quantity)
        {
            lock (_lock)
            {
                var item = Items.FirstOrDefault(i => i.ProductId == productId && i.VariantId == variantId);
                if (item == null) return;
                if (quantity <= 0)
                    Items.Remove(item);
                else
                {
                    if (quantity > item.StockQuantity)
                        quantity = item.StockQuantity;
                    item.Quantity = quantity;
                }
            }
            SaveToDatabase();
            OnCartChanged();
        }

        public void Clear()
        {
            lock (_lock)
            {
                Items.Clear();
            }
            SaveToDatabase();
            OnCartChanged();
        }

        private void OnCartChanged() => CartChanged?.Invoke();

        public void LoadFromDatabase(int userId)
        {
            lock (_lock)
            {
                Items.Clear();
                using (var context = new TmdtContext())
                {
                    var cart = context.Carts
                        .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.ProductImages)
                        .FirstOrDefault(c => c.UserId == userId);

                    if (cart != null)
                    {
                        foreach (var ci in cart.CartItems)
                        {
                            if (ci.Product != null)
                            {
                                Items.Add(new CartItem
                                {
                                    ProductId = ci.Product.ProductId,
                                    ProductName = ci.Product.ProductName ?? "",
                                    VariantId = ci.VariantId,
                                    VariantName = ci.Variant?.VariantName,
                                    Price = ci.Product.Price + (ci.Variant?.ExtraPrice ?? 0),
                                    OriginalPrice = ci.Product.OriginalPrice,
                                    ImageUrl = ci.Product.MainImageUrl,
                                    StockQuantity = ci.VariantId.HasValue ? (ci.Variant?.Quantity ?? 0) : (ci.Product.StockQuantity ?? 0),
                                    Quantity = ci.Quantity ?? 1,
                                    ShopId = ci.Product.ShopId ?? 0
                                });
                            }
                        }
                    }
                }
            }
            OnCartChanged();
        }

        private void SaveToDatabase()
        {
            if (!SessionManager.IsLoggedIn) return;
            int userId = SessionManager.CurrentUser.UserId;
            
            System.Threading.Tasks.Task.Run(() =>
            {
                using (var context = new TmdtContext())
                {
                    var cart = context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefault(c => c.UserId == userId);

                    if (cart == null)
                    {
                        cart = new Cart { UserId = userId, CreatedAt = DateTime.Now };
                        context.Carts.Add(cart);
                    }
                    else
                    {
                        context.CartItems.RemoveRange(cart.CartItems);
                    }

                    List<CartItem> snapshot;
                    lock (_lock)
                    {
                        snapshot = Items.ToList();
                    }

                    foreach (var item in snapshot)
                    {
                        cart.CartItems.Add(new TMDT.Models.CartItem
                        {
                            ProductId = item.ProductId,
                            VariantId = item.VariantId,
                            Quantity = item.Quantity,
                            AddedAt = DateTime.Now
                        });
                    }
                    context.SaveChanges();
                }
            });
        }
    }

    public class CartItem : System.ComponentModel.INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int? VariantId { get; set; }
        public string? VariantName { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? ImageUrl { get; set; }
        public int StockQuantity { get; set; }
        public int ShopId { get; set; }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(LineTotal)); OnPropertyChanged(nameof(DiscountPercent)); }
        }

        public decimal LineTotal => Price * Quantity;

        public int DiscountPercent => OriginalPrice.HasValue && OriginalPrice.Value > 0
            ? (int)Math.Round((1 - Price / OriginalPrice.Value) * 100)
            : 0;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}

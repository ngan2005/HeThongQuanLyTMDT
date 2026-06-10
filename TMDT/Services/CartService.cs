using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TMDT.Models;

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

        public void AddProduct(Product product, int quantity = 1)
        {
            if (product == null) return;

            lock (_lock)
            {
                var existing = Items.FirstOrDefault(i => i.ProductId == product.ProductId);
                if (existing != null)
                {
                    existing.Quantity += quantity;
                }
                else
                {
                    Items.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        Price = product.Price,
                        OriginalPrice = product.OriginalPrice,
                        ImageUrl = null,
                        StockQuantity = product.StockQuantity ?? 0,
                        Quantity = quantity,
                        ShopId = product.ShopId ?? 0
                    });
                }
            }
            OnCartChanged();
        }

        public void RemoveProduct(int productId)
        {
            lock (_lock)
            {
                var item = Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                    Items.Remove(item);
            }
            OnCartChanged();
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            lock (_lock)
            {
                var item = Items.FirstOrDefault(i => i.ProductId == productId);
                if (item == null) return;
                if (quantity <= 0)
                    Items.Remove(item);
                else
                    item.Quantity = quantity;
            }
            OnCartChanged();
        }

        public void Clear()
        {
            lock (_lock)
            {
                Items.Clear();
            }
            OnCartChanged();
        }

        private void OnCartChanged() => CartChanged?.Invoke();
    }

    public class CartItem : System.ComponentModel.INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;
using TMDT.Messages;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerComparisonViewModel : ViewModelBase, IDisposable
    {
        private readonly BuyerMainViewModel _mainVm;
        private bool _isLoading;

        public ObservableCollection<Product> ComparedProducts { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RemoveProductCommand { get; }
        public ICommand AddToCartCommand { get; }
        public ICommand ViewProductCommand { get; }

        public BuyerComparisonViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;

            RemoveProductCommand = new RelayCommand(ExecuteRemoveProduct);
            AddToCartCommand = new RelayCommand(ExecuteAddToCart);
            ViewProductCommand = new RelayCommand(ExecuteViewProduct);

            ComparisonService.Instance.ComparisonChanged += OnComparisonChanged;
            _ = LoadComparisonDataAsync();
        }

        private void OnComparisonChanged()
        {
            _ = LoadComparisonDataAsync();
        }

        private async Task LoadComparisonDataAsync()
        {
            IsLoading = true;
            try
            {
                var productIds = ComparisonService.Instance.ComparedProductIds.ToList();

                using var ctx = new TmdtContext();
                var products = await ctx.Products.AsNoTracking()
                    .Include(p => p.Shop)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ComparedProducts.Clear();
                    // Keep the original order as in productIds
                    foreach (var id in productIds)
                    {
                        var p = products.FirstOrDefault(x => x.ProductId == id);
                        if (p != null)
                        {
                            ComparedProducts.Add(p);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading comparison data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteRemoveProduct(object? parameter)
        {
            if (parameter is int productId)
            {
                ComparisonService.Instance.RemoveFromComparison(productId);
            }
            else if (parameter is Product p)
            {
                ComparisonService.Instance.RemoveFromComparison(p.ProductId);
            }
        }

        private void ExecuteAddToCart(object? parameter)
        {
            if (parameter is Product p)
            {
                if (p.StockQuantity <= 0)
                {
                    MessageBox.Show("Sản phẩm đã hết hàng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                CartService.Instance.AddProduct(p, null, 1);
                MessageBus.SendToast($"Đã thêm '{p.ProductName}' vào giỏ hàng.");
            }
        }

        private void ExecuteViewProduct(object? parameter)
        {
            if (parameter is Product p)
            {
                _mainVm.NavigateProductDetail(p);
            }
        }

        public void Dispose()
        {
            ComparisonService.Instance.ComparisonChanged -= OnComparisonChanged;
        }
    }
}

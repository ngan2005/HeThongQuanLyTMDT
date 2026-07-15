using System;
using System.Collections.Generic;
using System.Linq;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.Services
{
    public class ComparisonService
    {
        private static ComparisonService? _instance;
        private static readonly object _lock = new();

        public static ComparisonService Instance
        {
            get
            {
                lock (_lock)
                {
                    _instance ??= new ComparisonService();
                    return _instance;
                }
            }
        }

        public event Action? ComparisonChanged;

        private readonly List<int> _comparedProductIds = new();
        public IReadOnlyList<int> ComparedProductIds => _comparedProductIds;

        public int? CurrentCategoryId { get; private set; }

        private ComparisonService()
        {
        }

        public void LoadFromDb()
        {
            if (!SessionManager.IsLoggedIn || SessionManager.CurrentUser == null) return;

            try
            {
                using var ctx = new TmdtContext();
                var items = ctx.ProductComparisons
                    .Where(pc => pc.UserId == SessionManager.CurrentUser.UserId)
                    .Select(pc => new { pc.ProductId, CategoryId = pc.Product.CategoryId })
                    .ToList();

                _comparedProductIds.Clear();
                if (items.Any())
                {
                    CurrentCategoryId = items.First().CategoryId;
                    foreach (var item in items)
                    {
                        if (item.ProductId.HasValue)
                            _comparedProductIds.Add(item.ProductId.Value);
                    }
                }
                else
                {
                    CurrentCategoryId = null;
                }

                ComparisonChanged?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading comparison: {ex.Message}");
            }
        }

        public (bool Success, string Message) ToggleComparison(Product product)
        {
            if (product == null) return (false, "Sản phẩm không hợp lệ.");

            if (_comparedProductIds.Contains(product.ProductId))
            {
                RemoveFromComparison(product.ProductId);
                return (true, "Đã xóa khỏi danh sách so sánh.");
            }
            else
            {
                return AddToComparison(product);
            }
        }

        private (bool Success, string Message) AddToComparison(Product product)
        {
            if (_comparedProductIds.Count >= 3)
            {
                return (false, "Chỉ được so sánh tối đa 3 sản phẩm cùng lúc.");
            }

            if (_comparedProductIds.Count > 0 && CurrentCategoryId != product.CategoryId)
            {
                return (false, "Chỉ có thể so sánh các sản phẩm trong cùng một danh mục.");
            }

            if (SessionManager.IsLoggedIn)
            {
                try
                {
                    using var ctx = new TmdtContext();
                    var newPc = new ProductComparison
                    {
                        UserId = SessionManager.CurrentUser!.UserId,
                        ProductId = product.ProductId,
                        AddedAt = DateTime.Now
                    };
                    ctx.ProductComparisons.Add(newPc);
                    ctx.SaveChanges();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Add comparison error: {ex.Message}");
                    return (false, "Lỗi kết nối cơ sở dữ liệu.");
                }
            }

            _comparedProductIds.Add(product.ProductId);
            CurrentCategoryId = product.CategoryId;
            ComparisonChanged?.Invoke();

            return (true, "Đã thêm vào danh sách so sánh.");
        }

        public void RemoveFromComparison(int productId)
        {
            if (SessionManager.IsLoggedIn)
            {
                try
                {
                    using var ctx = new TmdtContext();
                    var item = ctx.ProductComparisons.FirstOrDefault(pc => pc.UserId == SessionManager.CurrentUser!.UserId && pc.ProductId == productId);
                    if (item != null)
                    {
                        ctx.ProductComparisons.Remove(item);
                        ctx.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Remove comparison error: {ex.Message}");
                }
            }

            _comparedProductIds.Remove(productId);
            if (_comparedProductIds.Count == 0)
            {
                CurrentCategoryId = null;
            }

            ComparisonChanged?.Invoke();
        }

        public void ClearLocal()
        {
            _comparedProductIds.Clear();
            CurrentCategoryId = null;
            ComparisonChanged?.Invoke();
        }
    }
}

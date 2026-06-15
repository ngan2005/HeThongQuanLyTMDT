using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class ReviewItem : ViewModelBase
    {
        public Review ReviewData { get; set; } = null!;

        // Properties for UI binding
        public string BuyerName => ReviewData.User?.FullName ?? "Khách hàng";
        public string BuyerAvatar => ReviewData.User?.Avatar ?? "/Assets/default_avatar.png";
        public string ProductName => ReviewData.Product?.ProductName ?? "Sản phẩm không xác định";
        public string ProductImage => ReviewData.Product?.ProductImages?.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl ?? "/Assets/placeholder.png";

        public int StarRating => ReviewData.StarRating ?? 5;
        public string Content => string.IsNullOrWhiteSpace(ReviewData.Content) ? "Không có nội dung đánh giá." : ReviewData.Content;
        public string ReviewImageUrl => ReviewData.ImageUrl ?? "";
        public bool HasImage => !string.IsNullOrEmpty(ReviewImageUrl);
        public string ReviewDate => ReviewData.ReviewedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";

        // Reply Data
        public ReviewReply? SellerReply => ReviewData.ReviewReplies?.OrderByDescending(r => r.RepliedAt).FirstOrDefault();
        public bool HasReply => SellerReply != null;
        public string ReplyContent => SellerReply?.Content ?? "";
        public string ReplyDate => SellerReply?.RepliedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";

        // UI State
        private bool _isReplying;
        public bool IsReplying
        {
            get => _isReplying;
            set { _isReplying = value; OnPropertyChanged(); }
        }

        private string _replyInputText = "";
        public string ReplyInputText
        {
            get => _replyInputText;
            set { _replyInputText = value; OnPropertyChanged(); }
        }
    }

    public class SellerReviewsViewModel : ViewModelBase
    {
        private readonly TmdtContext _context = null!;
        private ObservableCollection<ReviewItem> _reviews = new();
        
        // Filters
        private string _starFilter = "All";
        private string _statusFilter = "All";
        
        // Stats
        private double _averageRating = 0.0;
        private int _totalReviews = 0;
        private int _fiveStarCount = 0;
        private int _fourStarCount = 0;
        private int _threeStarCount = 0;
        private int _twoStarCount = 0;
        private int _oneStarCount = 0;

        public ObservableCollection<ReviewItem> Reviews
        {
            get => _reviews;
            set { _reviews = value; OnPropertyChanged(); }
        }

        public string StarFilter
        {
            get => _starFilter;
            set { _starFilter = value; OnPropertyChanged(); LoadReviews(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); LoadReviews(); }
        }

        public double AverageRating { get => _averageRating; set { _averageRating = value; OnPropertyChanged(); } }
        public int TotalReviews { get => _totalReviews; set { _totalReviews = value; OnPropertyChanged(); } }
        public int FiveStarCount { get => _fiveStarCount; set { _fiveStarCount = value; OnPropertyChanged(); } }
        public int FourStarCount { get => _fourStarCount; set { _fourStarCount = value; OnPropertyChanged(); } }
        public int ThreeStarCount { get => _threeStarCount; set { _threeStarCount = value; OnPropertyChanged(); } }
        public int TwoStarCount { get => _twoStarCount; set { _twoStarCount = value; OnPropertyChanged(); } }
        public int OneStarCount { get => _oneStarCount; set { _oneStarCount = value; OnPropertyChanged(); } }

        public ICommand SetStarFilterCommand { get; } = null!;
        public ICommand SetStatusFilterCommand { get; } = null!;
        public ICommand ToggleReplyCommand { get; } = null!;
        public ICommand SubmitReplyCommand { get; } = null!;

        public SellerReviewsViewModel()
        {
            try { _context = new TmdtContext(); } catch { }

            SetStarFilterCommand = new RelayCommand(o => StarFilter = o?.ToString() ?? "All");
            SetStatusFilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");
            
            ToggleReplyCommand = new RelayCommand(o => {
                if (o is ReviewItem item)
                {
                    item.IsReplying = !item.IsReplying;
                    if (item.IsReplying && item.HasReply)
                    {
                        item.ReplyInputText = item.ReplyContent; // Load existing reply for editing
                    }
                }
            });

            SubmitReplyCommand = new RelayCommand(ExecuteSubmitReply);

            CalculateStats();
            LoadReviews();
        }

        private int GetCurrentShopId()
        {
            if (SessionManager.IsSeller && SessionManager.CurrentUser?.ShopId != null)
            {
                return SessionManager.CurrentUser.ShopId.Value;
            }
            return 0;
        }

        private void CalculateStats()
        {
            int shopId = GetCurrentShopId();
            if (shopId <= 0 || _context == null) return;

            var allShopReviews = _context.Reviews
                .Include(r => r.Product)
                .Where(r => r.Product != null && r.Product.ShopId == shopId && r.IsHidden != true)
                .ToList();

            TotalReviews = allShopReviews.Count;
            if (TotalReviews > 0)
            {
                AverageRating = Math.Round(allShopReviews.Average(r => r.StarRating ?? 0.0), 1);
                FiveStarCount = allShopReviews.Count(r => r.StarRating == 5);
                FourStarCount = allShopReviews.Count(r => r.StarRating == 4);
                ThreeStarCount = allShopReviews.Count(r => r.StarRating == 3);
                TwoStarCount = allShopReviews.Count(r => r.StarRating == 2);
                OneStarCount = allShopReviews.Count(r => r.StarRating == 1);
            }
        }

        private void LoadReviews()
        {
            Reviews.Clear();
            int shopId = GetCurrentShopId();
            if (shopId <= 0 || _context == null) return;

            try
            {
                var query = _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                        .ThenInclude(p => p.ProductImages)
                    .Include(r => r.ReviewReplies)
                    .Where(r => r.Product != null && r.Product.ShopId == shopId && r.IsHidden != true)
                    .AsQueryable();

                // Apply Star Filter
                if (StarFilter != "All" && int.TryParse(StarFilter, out int starVal))
                {
                    query = query.Where(r => r.StarRating == starVal);
                }

                // Apply Status Filter
                if (StatusFilter == "Replied")
                {
                    query = query.Where(r => r.ReviewReplies.Any());
                }
                else if (StatusFilter == "NotReplied")
                {
                    query = query.Where(r => !r.ReviewReplies.Any());
                }

                var results = query.OrderByDescending(r => r.ReviewedAt).ToList();

                foreach (var review in results)
                {
                    Reviews.Add(new ReviewItem { ReviewData = review });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadReviews Error: " + ex.Message);
            }
        }

        private void ExecuteSubmitReply(object? parameter)
        {
            if (parameter is ReviewItem item)
            {
                if (string.IsNullOrWhiteSpace(item.ReplyInputText))
                {
                    MessageBox.Show("Vui lòng nhập nội dung phản hồi.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int sellerId = SessionManager.CurrentUser?.UserId ?? 0;
                if (sellerId <= 0) return;

                try
                {
                    // Check if already replied, update it
                    var existingReply = _context.ReviewReplies.FirstOrDefault(r => r.ReviewId == item.ReviewData.ReviewId);
                    if (existingReply != null)
                    {
                        existingReply.Content = item.ReplyInputText.Trim();
                        existingReply.RepliedAt = DateTime.Now;
                    }
                    else
                    {
                        var newReply = new ReviewReply
                        {
                            ReviewId = item.ReviewData.ReviewId,
                            UserId = sellerId,
                            Content = item.ReplyInputText.Trim(),
                            RepliedAt = DateTime.Now
                        };
                        _context.ReviewReplies.Add(newReply);
                    }

                    _context.SaveChanges();

                    item.IsReplying = false;
                    item.ReplyInputText = "";
                    LoadReviews(); // Reload to refresh data
                    MessageBox.Show("Đã lưu phản hồi thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi gửi phản hồi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

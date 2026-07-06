using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Views.Seller;
using System.Windows;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class SellerCustomerDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        public int OrderCount { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class SellerCustomersViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        private readonly int _shopId;

        public ObservableCollection<SellerCustomerDto> Customers { get; } = new ObservableCollection<SellerCustomerDto>();

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                _searchKeyword = value;
                OnPropertyChanged();
                // Tự động tìm kiếm sau khi gõ (có thể thêm delay nếu muốn, nhưng đơn giản thì cứ load trực tiếp)
                _ = LoadCustomersAsync();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ICommand AddCustomerCommand { get; }
        public ICommand LoadCustomersCommand { get; }

        public SellerCustomersViewModel()
        {
            _context = new TmdtContext();
            
            // Lấy ShopId của phiên đăng nhập hiện tại
            var currentUser = SessionManager.CurrentUser;
            if (currentUser != null)
            {
                var shop = _context.Shops.FirstOrDefault(s => s.UserId == currentUser.UserId);
                _shopId = shop?.ShopId ?? 0;
            }

            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            LoadCustomersCommand = new RelayCommand(async _ => await LoadCustomersAsync());

            // Tự động load danh sách lần đầu
            _ = LoadCustomersAsync();
        }

        public async Task LoadCustomersAsync()
        {
            if (_shopId == 0) return;

            IsLoading = true;
            try
            {
                var query = _context.Users
                    .Where(u => u.RoleId == 2); // Chỉ lấy Buyer

                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    string kw = SearchKeyword.ToLower();
                    query = query.Where(u => u.FullName.ToLower().Contains(kw) || 
                                             (u.Phone != null && u.Phone.Contains(kw)));
                }

                // Lấy tất cả user (dùng ToListAsync trước để dễ xử lý logic vì truy vấn phức tạp của EF Core đôi khi lỗi nếu GroupBy/Count không tương thích)
                // Tuy nhiên, để tối ưu, ta truy vấn trực tiếp SQL
                // Khách hàng của shop: 
                // 1. Email có đuôi @pos.local (đăng ký nhanh)
                // 2. HOẶC từng mua hàng ở shop này
                var users = await query.ToListAsync();
                
                var orderCounts = await _context.Orders
                    .Where(o => o.ShopId == _shopId)
                    .GroupBy(o => o.BuyerId)
                    .Select(g => new { BuyerId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.BuyerId, x => x.Count);

                var customerList = users
                    .Where(u => u.Email.EndsWith("@pos.local") || orderCounts.ContainsKey(u.UserId))
                    .Select(u => new SellerCustomerDto
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Phone = u.Phone ?? string.Empty,
                        Email = u.Email,
                        LoyaltyPoints = u.LoyaltyPoints ?? 0,
                        OrderCount = orderCounts.ContainsKey(u.UserId) ? orderCounts[u.UserId] : 0,
                        CreatedAt = u.CreatedAt
                    })
                    .OrderByDescending(c => c.OrderCount)
                    .ThenByDescending(c => c.CreatedAt)
                    .ToList();

                Customers.Clear();
                foreach (var c in customerList)
                {
                    Customers.Add(c);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách khách hàng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteAddCustomer(object? obj)
        {
            var addCustomerWindow = new AddCustomerWindow();
            // Lấy reference cửa sổ cha
            addCustomerWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            
            // Mở popup dạng dialog
            if (addCustomerWindow.ShowDialog() == true)
            {
                // Nếu đăng ký thành công (dialog result = true), tải lại danh sách khách hàng
                _ = LoadCustomersAsync();
            }
        }
    }
}

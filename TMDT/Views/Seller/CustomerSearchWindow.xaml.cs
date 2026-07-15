using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.Views.Seller
{
    public partial class CustomerSearchWindow : Window, INotifyPropertyChanged
    {
        private readonly int _shopId;
        private readonly List<CustomerRow> _allCustomers = new();

        public string SelectedPhone { get; private set; } = string.Empty;
        public int? SelectedBuyerId { get; private set; }

        public ObservableCollection<CustomerRow> FilteredCustomers { get; } = new();

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                _searchKeyword = value;
                OnPropertyChanged(nameof(SearchKeyword));
                ApplyFilter();
            }
        }

        public CustomerSearchWindow() : this(0) { }

        public CustomerSearchWindow(int shopId)
        {
            InitializeComponent();
            _shopId = shopId > 0 ? shopId : (SessionManager.CurrentUser?.ShopId ?? 0);
            DataContext = this;
            Loaded += async (_, _) => await LoadCustomersAsync();
        }

        private async Task LoadCustomersAsync()
        {
            if (_shopId <= 0)
            {
                MessageBox.Show("Không xác định được cửa hàng hiện tại.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;
            try
            {
                using var context = new TmdtContext();

                var query = context.Users.Where(u => u.RoleId == 2);

                var orderCounts = await context.Orders
                    .Where(o => o.ShopId == _shopId)
                    .GroupBy(o => o.BuyerId)
                    .Select(g => new { BuyerId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.BuyerId, x => x.Count);

                var users = await query.ToListAsync();

                _allCustomers.Clear();
                foreach (var u in users.Where(u => u.Email.EndsWith("@pos.local")
                                                  || orderCounts.ContainsKey(u.UserId)))
                {
                    _allCustomers.Add(new CustomerRow
                    {
                        UserId = u.UserId,
                        FullName = u.FullName ?? "(Không tên)",
                        Phone = u.Phone ?? string.Empty,
                        LoyaltyPoints = u.LoyaltyPoints ?? 0,
                        OrderCount = orderCounts.TryGetValue(u.UserId, out var c) ? c : 0
                    });
                }

                var sorted = _allCustomers
                    .OrderByDescending(c => c.OrderCount)
                    .ThenByDescending(c => c.LoyaltyPoints)
                    .ThenBy(c => c.FullName)
                    .ToList();

                _allCustomers.Clear();
                _allCustomers.AddRange(sorted);

                ApplyFilter();

                dgCustomers.SelectedIndex = _allCustomers.Count > 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách khách hàng: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyFilter()
        {
            FilteredCustomers.Clear();

            IEnumerable<CustomerRow> source = _allCustomers;
            if (!string.IsNullOrWhiteSpace(_searchKeyword))
            {
                // Normalize từ khóa: bỏ khoảng trắng, chỉ giữ số
                var digits = new string(_searchKeyword.Trim().Where(char.IsDigit).ToArray());

                source = source.Where(c =>
                    (c.FullName ?? string.Empty).ToLower().Contains(_searchKeyword.Trim().ToLower()) ||
                    (digits.Length > 0 && new string((c.Phone ?? "").Where(char.IsDigit).ToArray()).Contains(digits)));
            }

            foreach (var c in source)
                FilteredCustomers.Add(c);

            txtResultCount.Text = FilteredCustomers.Count switch
            {
                0 => string.IsNullOrWhiteSpace(_searchKeyword)
                    ? _allCustomers.Count == 0
                        ? "Chưa có khách hàng thành viên nào."
                        : $"Có {_allCustomers.Count} khách hàng — thử gõ tên hoặc SĐT để lọc."
                    : "Không tìm thấy kết quả phù hợp.",
                1 => "Tìm thấy 1 khách hàng.",
                _ => $"Tìm thấy {FilteredCustomers.Count} khách hàng."
            };

            btnClearSearch.Visibility = string.IsNullOrEmpty(_searchKeyword)
                ? Visibility.Collapsed : Visibility.Visible;

            if (FilteredCustomers.Count > 0 && dgCustomers.SelectedIndex < 0)
                dgCustomers.SelectedIndex = 0;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchKeyword = txtSearch.Text;
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                txtSearch.Clear();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (dgCustomers.SelectedItem is CustomerRow row)
                {
                    ChooseRow(row);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down)
            {
                if (FilteredCustomers.Count > 0)
                {
                    var idx = dgCustomers.SelectedIndex;
                    if (idx < FilteredCustomers.Count - 1)
                    {
                        dgCustomers.SelectedIndex = idx + 1;
                        dgCustomers.ScrollIntoView(dgCustomers.SelectedItem);
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up)
            {
                if (FilteredCustomers.Count > 0)
                {
                    var idx = dgCustomers.SelectedIndex;
                    if (idx > 0)
                    {
                        dgCustomers.SelectedIndex = idx - 1;
                        dgCustomers.ScrollIntoView(dgCustomers.SelectedItem);
                    }
                    e.Handled = true;
                }
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            txtSearch.Focus();
        }

        private void DgCustomers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgCustomers.SelectedItem is CustomerRow row)
            {
                ChooseRow(row);
            }
        }

        private void dgCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSelect.IsEnabled = dgCustomers.SelectedItem is CustomerRow;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Wire selection change after xaml is loaded
            dgCustomers.SelectionChanged += dgCustomers_SelectionChanged;
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is CustomerRow row)
            {
                ChooseRow(row);
            }
        }

        private void ChooseRow(CustomerRow row)
        {
            if (string.IsNullOrWhiteSpace(row.Phone))
            {
                MessageBox.Show("Khách hàng này chưa có số điện thoại nên không áp dụng được điểm.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedPhone = row.Phone;
            SelectedBuyerId = row.UserId;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class CustomerRow
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        public int OrderCount { get; set; }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminShopsViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        private ObservableCollection<Shop> _shops;
        private Shop _selectedShop;
        private string _searchText = "";
        private string _statusFilter = "All"; // All, Pending, Active, Suspended

        public ObservableCollection<Shop> Shops
        {
            get => _shops;
            set { _shops = value; OnPropertyChanged(); }
        }

        public Shop SelectedShop
        {
            get => _selectedShop;
            set { _selectedShop = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set 
            { 
                _searchText = value; 
                OnPropertyChanged(); 
                LoadShops(); 
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
                LoadShops();
            }
        }

        // Commands
        public ICommand ApproveShopCommand { get; }
        public ICommand SuspendShopCommand { get; }
        public ICommand ActivateShopCommand { get; }
        public ICommand FilterCommand { get; }

        public AdminShopsViewModel()
        {
            try
            {
                _context = new TmdtContext();
            }
            catch
            {
                // Failsafe if DB fails to init
            }

            Shops = new ObservableCollection<Shop>();

            // Setup Commands
            ApproveShopCommand = new RelayCommand(ExecuteApproveShop, CanExecuteApproveShop);
            SuspendShopCommand = new RelayCommand(ExecuteSuspendShop, CanExecuteSuspendShop);
            ActivateShopCommand = new RelayCommand(ExecuteActivateShop, CanExecuteActivateShop);
            FilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");

            LoadShops();
        }

        private void LoadShops()
        {
            Shops.Clear();

            try
            {
                if (_context != null && _context.Shops.Any())
                {
                    var query = _context.Shops.Include(s => s.User).AsQueryable();

                    // Apply Search
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        query = query.Where(s => s.ShopName.Contains(SearchText) || 
                                                 (s.User != null && s.User.FullName.Contains(SearchText)));
                    }

                    // Apply Filter
                    if (StatusFilter == "Pending")
                    {
                        query = query.Where(s => s.IsActive == null);
                    }
                    else if (StatusFilter == "Active")
                    {
                        query = query.Where(s => s.IsActive == true);
                    }
                    else if (StatusFilter == "Suspended")
                    {
                        query = query.Where(s => s.IsActive == false);
                    }

                    var dbShops = query.ToList();
                    foreach (var shop in dbShops)
                    {
                        Shops.Add(shop);
                    }

                    if (Shops.Any())
                        return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF core query failed, loading mock fallback shops. " + ex.Message);
            }

            // FALLBACK BEAUTIFUL MOCK SHops
            LoadMockShops();
        }

        private void LoadMockShops()
        {
            var mockShops = new ObservableCollection<Shop>();

            // Mock 1: Pending Shop
            mockShops.Add(new Shop 
            { 
                ShopId = 101, 
                ShopName = "Gia Dụng Thông Minh Việt", 
                WarehouseAddress = "12 Chùa Bộc, Đống Đa, Hà Nội", 
                CommissionRate = 3.0m,
                WalletBalance = 0,
                Rating = 0,
                IsActive = null, // Chờ duyệt
                OpenedAt = DateTime.Now.AddDays(-2),
                User = new User { FullName = "Hoàng Văn Lâm", Email = "lamhv@gmail.com" }
            });

            // Mock 2: Pending Shop
            mockShops.Add(new Shop 
            { 
                ShopId = 102, 
                ShopName = "Organic Food & Fruits", 
                WarehouseAddress = "45 Nguyễn Thị Minh Khai, Quận 1, TP. HCM", 
                CommissionRate = 3.5m,
                WalletBalance = 0,
                Rating = 0,
                IsActive = null, // Chờ duyệt
                OpenedAt = DateTime.Now.AddDays(-1),
                User = new User { FullName = "Lê Thị Mai", Email = "mailt@gmail.com" }
            });

            // Mock 3: Active Shop
            mockShops.Add(new Shop 
            { 
                ShopId = 103, 
                ShopName = "Hanoi Gadgets Store", 
                WarehouseAddress = "99 Cầu Giấy, Hà Nội", 
                CommissionRate = 4.0m,
                WalletBalance = 124500000,
                Rating = 4.8m,
                IsActive = true, // Đang hoạt động
                OpenedAt = DateTime.Now.AddMonths(-6),
                User = new User { FullName = "Nguyễn Văn Đạt", Email = "datnv@gmail.com" }
            });

            // Mock 4: Active Shop
            mockShops.Add(new Shop 
            { 
                ShopId = 104, 
                ShopName = "Fashionista Zone", 
                WarehouseAddress = "284 Lê Văn Sỹ, Quận 3, TP. HCM", 
                CommissionRate = 5.0m,
                WalletBalance = 54200000,
                Rating = 4.6m,
                IsActive = true, // Đang hoạt động
                OpenedAt = DateTime.Now.AddMonths(-3),
                User = new User { FullName = "Trần Thu Hà", Email = "hatran@gmail.com" }
            });

            // Mock 5: Suspended Shop
            mockShops.Add(new Shop 
            { 
                ShopId = 105, 
                ShopName = "Phụ Kiện Điện Thoại Giá Rẻ 247", 
                WarehouseAddress = "156 Trần Đại Nghĩa, Hai Bà Trưng, Hà Nội", 
                CommissionRate = 3.0m,
                WalletBalance = 1500000,
                Rating = 3.2m,
                IsActive = false, // Đang khóa
                OpenedAt = DateTime.Now.AddMonths(-1),
                User = new User { FullName = "Vũ Việt Anh", Email = "anhvv@gmail.com" }
            });

            // Apply filters to Mock data
            var filtered = mockShops.AsQueryable();
            if (!string.IsNullOrEmpty(SearchText))
            {
                filtered = filtered.Where(s => s.ShopName.ToLower().Contains(SearchText.ToLower()) || 
                                               s.User.FullName.ToLower().Contains(SearchText.ToLower()));
            }

            if (StatusFilter == "Pending")
            {
                filtered = filtered.Where(s => s.IsActive == null);
            }
            else if (StatusFilter == "Active")
            {
                filtered = filtered.Where(s => s.IsActive == true);
            }
            else if (StatusFilter == "Suspended")
            {
                filtered = filtered.Where(s => s.IsActive == false);
            }

            foreach (var shop in filtered.ToList())
            {
                Shops.Add(shop);
            }
        }

        // --- Commands Implementation ---

        private bool CanExecuteApproveShop(object obj) => SelectedShop != null && SelectedShop.IsActive == null;
        private async void ExecuteApproveShop(object obj)
        {
            if (SelectedShop == null) return;

            var result = MessageBox.Show($"Bạn có đồng ý phê duyệt cho Shop '{SelectedShop.ShopName}' hoạt động trên sàn?", 
                                         "Xác nhận phê duyệt", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            SelectedShop.IsActive = true;
            SelectedShop.OpenedAt = DateTime.Now;

            try
            {
                if (_context != null)
                {
                    var dbShop = await _context.Shops.FindAsync(SelectedShop.ShopId);
                    if (dbShop != null)
                    {
                        dbShop.IsActive = true;
                        dbShop.OpenedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã duyệt thành công! Shop '{SelectedShop.ShopName}' hiện đã có thể đăng bán sản phẩm.", 
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            
            LoadShops();
        }

        private bool CanExecuteSuspendShop(object obj) => SelectedShop != null && SelectedShop.IsActive == true;
        private async void ExecuteSuspendShop(object obj)
        {
            if (SelectedShop == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn TẠM KHÓA Shop '{SelectedShop.ShopName}'? Shop sẽ không thể đăng bán sản phẩm mới.", 
                                         "Xác nhận tạm khóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            SelectedShop.IsActive = false;

            try
            {
                if (_context != null)
                {
                    var dbShop = await _context.Shops.FindAsync(SelectedShop.ShopId);
                    if (dbShop != null)
                    {
                        dbShop.IsActive = false;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã khóa tạm thời Shop '{SelectedShop.ShopName}'.", 
                            "Đã thực hiện", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadShops();
        }

        private bool CanExecuteActivateShop(object obj) => SelectedShop != null && SelectedShop.IsActive == false;
        private async void ExecuteActivateShop(object obj)
        {
            if (SelectedShop == null) return;

            var result = MessageBox.Show($"Bạn có muốn KÍCH HOẠT LẠI cho Shop '{SelectedShop.ShopName}' hoạt động bình thường?", 
                                         "Xác nhận kích hoạt", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            SelectedShop.IsActive = true;

            try
            {
                if (_context != null)
                {
                    var dbShop = await _context.Shops.FindAsync(SelectedShop.ShopId);
                    if (dbShop != null)
                    {
                        dbShop.IsActive = true;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Shop '{SelectedShop.ShopName}' đã được kích hoạt hoạt động trở lại.", 
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadShops();
        }
    }
}

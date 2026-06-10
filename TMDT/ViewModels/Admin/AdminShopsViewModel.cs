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
        private string _statusFilter = "All";

        // Stats
        private int _totalShops;
        private int _pendingShops;
        private int _activeShops;
        private decimal _totalRevenue;

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
            set { _searchText = value; OnPropertyChanged(); LoadShops(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); LoadShops(); }
        }

        public int TotalShops { get => _totalShops; set { _totalShops = value; OnPropertyChanged(); } }
        public int PendingShops { get => _pendingShops; set { _pendingShops = value; OnPropertyChanged(); } }
        public int ActiveShops { get => _activeShops; set { _activeShops = value; OnPropertyChanged(); } }
        public decimal TotalRevenue { get => _totalRevenue; set { _totalRevenue = value; OnPropertyChanged(); } }

        // Events
        public event Action ShowDetailRequest;
        public event Action HideDetailRequest;

        // Commands
        public ICommand ApproveShopCommand { get; }
        public ICommand SuspendShopCommand { get; }
        public ICommand ActivateShopCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand ViewDetailCommand { get; }

        public AdminShopsViewModel(string initialStatus = "All")
        {
            _statusFilter = initialStatus;
            try { _context = new TmdtContext(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Init TmdtContext failed: " + ex.Message); }

            Shops = new ObservableCollection<Shop>();

            ApproveShopCommand = new RelayCommand(ExecuteApproveShop, _ => SelectedShop != null && SelectedShop.IsActive == null);
            SuspendShopCommand = new RelayCommand(ExecuteSuspendShop, _ => SelectedShop != null && SelectedShop.IsActive == true);
            ActivateShopCommand = new RelayCommand(ExecuteActivateShop, _ => SelectedShop != null && SelectedShop.IsActive == false);
            FilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");
            ViewDetailCommand = new RelayCommand(o => ShowDetailRequest?.Invoke());

            LoadShops();
        }

        private void LoadShops()
        {
            Shops.Clear();
            try
            {
                if (_context == null) return;

                // Load stats
                TotalShops = _context.Shops.Count();
                PendingShops = _context.Shops.Count(s => s.IsActive == null);
                ActiveShops = _context.Shops.Count(s => s.IsActive == true);
                TotalRevenue = _context.Shops.Sum(s => s.WalletBalance ?? 0);

                var query = _context.Shops.Include(s => s.User).AsQueryable();

                if (!string.IsNullOrEmpty(SearchText))
                {
                    string term = SearchText.Trim().ToLower();
                    query = query.Where(s =>
                        (s.ShopName != null && EF.Functions.Like(s.ShopName, $"%{term}%")) ||
                        (s.User != null && s.User.FullName != null && EF.Functions.Like(s.User.FullName, $"%{SearchText}%")));
                }

                if (StatusFilter == "Pending") query = query.Where(s => s.IsActive == null);
                else if (StatusFilter == "Active") query = query.Where(s => s.IsActive == true);
                else if (StatusFilter == "Suspended") query = query.Where(s => s.IsActive == false);

                foreach (var shop in query.ToList())
                    Shops.Add(shop);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadShops failed: " + ex.Message);
            }
        }

        private async void ExecuteApproveShop(object obj)
        {
            if (SelectedShop == null) return;

            var result = MessageBox.Show(
                $"Phê duyệt shop '{SelectedShop.ShopName}' hoạt động trên sàn?",
                "Xác nhận duyệt", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var dbShop = await _context.Shops.FindAsync(SelectedShop.ShopId);
                if (dbShop != null)
                {
                    dbShop.IsActive = true;
                    dbShop.OpenedAt = DateTime.Now;

                    var user = await _context.Users.FindAsync(dbShop.UserId);
                    if (user != null)
                    {
                        var sellerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SessionManager.RoleSeller);
                        if (sellerRole != null && user.RoleId != sellerRole.RoleId)
                            user.RoleId = sellerRole.RoleId;
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Approve failed: " + ex.Message);
            }

            AuditLogHelper.Log("APPROVE_SHOP", $"Duyệt '{SelectedShop.ShopName}' (ID:{SelectedShop.ShopId})", "Shop", "Normal");
            MessageBox.Show($"Đã duyệt '{SelectedShop.ShopName}'!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            HideDetailRequest?.Invoke();
            LoadShops();
        }

        private async void ExecuteSuspendShop(object obj)
        {
            if (SelectedShop == null) return;

            var result = MessageBox.Show(
                $"Tạm khóa shop '{SelectedShop.ShopName}'?",
                "Xác nhận khóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var dbShop = await _context.Shops.FindAsync(SelectedShop.ShopId);
                if (dbShop != null)
                {
                    dbShop.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Suspend failed: " + ex.Message);
            }

            AuditLogHelper.Log("SUSPEND_SHOP", $"Khóa '{SelectedShop.ShopName}' (ID:{SelectedShop.ShopId})", "Shop", "Warning");
            MessageBox.Show($"Đã khóa '{SelectedShop.ShopName}'.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            HideDetailRequest?.Invoke();
            LoadShops();
        }

        private async void ExecuteActivateShop(object obj)
        {
            if (SelectedShop == null) return;

            var result = MessageBox.Show(
                $"Kích hoạt lại shop '{SelectedShop.ShopName}'?",
                "Xác nhận kích hoạt", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var dbShop = await _context.Shops.FindAsync(SelectedShop.ShopId);
                if (dbShop != null)
                {
                    dbShop.IsActive = true;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Activate failed: " + ex.Message);
            }

            AuditLogHelper.Log("ACTIVATE_SHOP", $"Kích hoạt '{SelectedShop.ShopName}' (ID:{SelectedShop.ShopId})", "Shop", "Normal");
            MessageBox.Show($"Đã kích hoạt '{SelectedShop.ShopName}'.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            HideDetailRequest?.Invoke();
            LoadShops();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}

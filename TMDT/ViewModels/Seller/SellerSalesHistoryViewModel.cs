using System;
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
    public class SellerSalesHistoryViewModel : ViewModelBase
    {
        private readonly int _shopId;
        private bool _showOnline = true;
        private bool _isLoading;
        private string _searchText = "";
        private ObservableCollection<Order> _orders = new();
        private Order? _selectedOrder;

        public bool ShowOnline
        {
            get => _showOnline;
            set { _showOnline = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowPOS)); LoadOrders(); }
        }
        public bool ShowPOS => !_showOnline;

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); LoadOrders(); }
        }

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set { _orders = value; OnPropertyChanged(); }
        }

        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set { _selectedOrder = value; OnPropertyChanged(); }
        }

        public int TotalOrders => Orders.Count;
        public decimal TotalRevenue => Orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount ?? 0);

        public ICommand SwitchOnlineCommand { get; }
        public ICommand SwitchPOSCommand { get; }
        public ICommand RefreshCommand { get; }

        public SellerSalesHistoryViewModel()
        {
            // Lấy shopId của seller hiện tại
            _shopId = GetCurrentShopId();

            SwitchOnlineCommand = new RelayCommand(_ => ShowOnline = true);
            SwitchPOSCommand = new RelayCommand(_ => ShowOnline = false);
            RefreshCommand = new RelayCommand(_ => LoadOrders());

            LoadOrders();
        }

        private void LoadOrders()
        {
            _ = LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            if (_shopId <= 0) return;
            IsLoading = true;

            try
            {
                await Task.Run(() =>
                {
                    using var ctx = new TmdtContext();
                    IQueryable<Order> query = ctx.Orders
                        .Include(o => o.OrderDetails)
                        .Include(o => o.Payments)
                        .Include(o => o.Buyer)
                        .Where(o => o.ShopId == _shopId);

                    if (ShowOnline)
                        // Đơn online: buyer email khác guest@pos.local
                        query = query.Where(o => o.Buyer == null || o.Buyer.Email != "guest@pos.local");
                    else
                        // Đơn POS: buyer là tài khoản guest@pos.local (khách vãng lai tại quầy)
                        query = query.Where(o => o.Buyer != null && o.Buyer.Email == "guest@pos.local");

                    if (!string.IsNullOrWhiteSpace(_searchText))
                        query = query.Where(o => o.OrderCode!.Contains(_searchText));

                    var result = query.OrderByDescending(o => o.OrderDate).ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Orders = new ObservableCollection<Order>(result);
                        OnPropertyChanged(nameof(TotalOrders));
                        OnPropertyChanged(nameof(TotalRevenue));
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadOrders failed: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private int GetCurrentShopId()
        {
            try
            {
                if (SessionManager.CurrentUser == null) return 0;
                using var ctx = new TmdtContext();
                var shop = ctx.Shops.FirstOrDefault(s => s.UserId == SessionManager.CurrentUser.UserId);
                return shop?.ShopId ?? 0;
            }
            catch { return 0; }
        }
    }
}

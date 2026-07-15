using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.DTOs;
using TMDT.Utilities;
using TMDT.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TMDT.ViewModels.Seller
{
    public enum CustomerLookupStatus
    {
        None,
        Searching,
        Found,
        NotFound
    }

    public class PosCartItem : ViewModelBase
    {
        private int _quantity;
        
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int? VariantId { get; set; }
        public string? VariantName { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
        
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }
        
        public decimal LineTotal => Quantity * UnitPrice;
    }

    public class PosOrderContext : ViewModelBase
    {
        private string _tabTitle = "Đơn mới";
        public string TabTitle { get => _tabTitle; set => SetProperty(ref _tabTitle, value); }

        // 🟢 Tên hiển thị trên tab bill (auto đổi khi khách thay đổi)
        private string _billLabel = "Khách vãng lai";
        public string BillLabel
        {
            get => _billLabel;
            set
            {
                if (SetProperty(ref _billLabel, value))
                    OnPropertyChanged(nameof(BillDisplayText));
            }
        }

        public string BillDisplayText => string.IsNullOrWhiteSpace(_billLabel) ? "Khách vãng lai" : _billLabel;

        // 🟢 Lưu OrderId của đơn POS đang chờ thanh toán (AwaitingPayment).
        // Null = đơn mới (chưa có trong DB). Có giá trị = cho phép sửa rồi update lại DB.
        private int? _pendingPosOrderId;
        public int? PendingPosOrderId { get => _pendingPosOrderId; set => SetProperty(ref _pendingPosOrderId, value); }

        private ObservableCollection<PosCartItem> _cartItems = new();
        public ObservableCollection<PosCartItem> CartItems { get => _cartItems; set => SetProperty(ref _cartItems, value); }

        private decimal _totalPayment;
        public decimal TotalPayment { get => _totalPayment; set => SetProperty(ref _totalPayment, value); }

        private int _selectedPayment = 0;
        public int SelectedPayment 
        { 
            get => _selectedPayment; 
            set 
            { 
                if (SetProperty(ref _selectedPayment, value)) 
                { 
                    OnPropertyChanged(nameof(IsCashPayment));
                } 
            } 
        }

        public bool IsCashPayment => SelectedPayment == 0;

        private string _customerPhone = "";
        public string CustomerPhone
        {
            get => _customerPhone;
            set
            {
                if (SetProperty(ref _customerPhone, value))
                {
                    CustomerLookupStatus = CustomerLookupStatus.Searching;
                    OnPropertyChanged(nameof(CustomerLookupStatus));
                }
            }
        }

        private string _customerName = "Khách vãng lai";
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (SetProperty(ref _customerName, value))
                {
                    // 🟢 Auto-sync BillLabel khi tìm thấy / reset khách — cashier nhìn tab biết ngay đang phục vụ ai
                    var displayName = string.IsNullOrWhiteSpace(value) ? "Khách vãng lai" : value;
                    if (string.IsNullOrWhiteSpace(BillLabel)
                        || BillLabel.StartsWith("Khách ")
                        || BillLabel == _customerName)
                    {
                        BillLabel = displayName;
                    }
                    OnPropertyChanged(nameof(CustomerDisplayText));
                }
            }
        }

        public int? BuyerId { get; set; }

        private CustomerLookupStatus _customerLookupStatus = CustomerLookupStatus.None;
        public CustomerLookupStatus CustomerLookupStatus
        {
            get => _customerLookupStatus;
            set
            {
                if (SetProperty(ref _customerLookupStatus, value))
                {
                    OnPropertyChanged(nameof(CustomerLookupStatusText));
                    OnPropertyChanged(nameof(CustomerDisplayText));
                    OnPropertyChanged(nameof(IsCustomerFound));
                    OnPropertyChanged(nameof(IsCustomerNotFound));
                    OnPropertyChanged(nameof(IsCustomerSearching));
                }
            }
        }

        public string CustomerLookupStatusText => _customerLookupStatus switch
        {
            CustomerLookupStatus.Searching => "Đang tìm...",
            CustomerLookupStatus.Found => "Đã tìm thấy",
            CustomerLookupStatus.NotFound => "Chưa đăng ký thành viên",
            _ => ""
        };

        public string CustomerDisplayText => _customerLookupStatus switch
        {
            CustomerLookupStatus.Searching => "Đang tìm khách...",
            CustomerLookupStatus.Found => _customerName,
            CustomerLookupStatus.NotFound => "Khách vãng lai (chưa đăng ký)",
            _ => "Khách vãng lai"
        };

        public bool IsCustomerSearching => _customerLookupStatus == CustomerLookupStatus.Searching;
        public bool IsCustomerFound => _customerLookupStatus == CustomerLookupStatus.Found;
        public bool IsCustomerNotFound => _customerLookupStatus == CustomerLookupStatus.NotFound;

        private int _loyaltyPoints;
        public int LoyaltyPoints { get => _loyaltyPoints; set => SetProperty(ref _loyaltyPoints, value); }

        private int _orderCount;
        public int OrderCount { get => _orderCount; set => SetProperty(ref _orderCount, value); }

        private bool _useLoyaltyPoints;
        public bool UseLoyaltyPoints { get => _useLoyaltyPoints; set => SetProperty(ref _useLoyaltyPoints, value); }

        private string _voucherCode = "";
        public string VoucherCode { get => _voucherCode; set => SetProperty(ref _voucherCode, value); }

        public Voucher? AppliedVoucher { get; set; }

        private decimal _discountAmount;
        public decimal DiscountAmount { get => _discountAmount; set => SetProperty(ref _discountAmount, value); }

        private decimal _manualDiscount;
        public decimal ManualDiscount
        {
            get => _manualDiscount;
            set
            {
                if (value < 0) value = 0;
                if (SetProperty(ref _manualDiscount, value))
                {
                    _manualDiscountInput = value.ToString();
                    OnPropertyChanged(nameof(ManualDiscountInput));
                    OnPropertyChanged(nameof(IsManualDiscountExceeded));
                    OnPropertyChanged(nameof(ManualDiscountWarningText));
                    OnPropertyChanged(nameof(MaxManualDiscount));
                }
            }
        }

        private string _manualDiscountInput = "0";
        public string ManualDiscountInput
        {
            get => _manualDiscountInput;
            set
            {
                if (SetProperty(ref _manualDiscountInput, value))
                {
                    if (decimal.TryParse(value, out decimal result))
                    {
                        var max = MaxManualDiscount;
                        if (result < 0) result = 0;
                        if (result > max) result = max;
                        _manualDiscount = result;
                        OnPropertyChanged(nameof(ManualDiscount));
                        OnPropertyChanged(nameof(IsManualDiscountExceeded));
                        OnPropertyChanged(nameof(ManualDiscountWarningText));
                    }
                    else if (string.IsNullOrEmpty(value))
                    {
                        _manualDiscount = 0;
                        OnPropertyChanged(nameof(ManualDiscount));
                        OnPropertyChanged(nameof(IsManualDiscountExceeded));
                        OnPropertyChanged(nameof(ManualDiscountWarningText));
                    }
                }
            }
        }

        public decimal MaxManualDiscount
        {
            get
            {
                var max = TotalPayment;
                if (AppliedVoucher != null)
                {
                    if (AppliedVoucher.DiscountType == "Percentage")
                        max -= TotalPayment * ((AppliedVoucher.DiscountValue ?? 0) / 100m);
                    else
                        max -= AppliedVoucher.DiscountValue ?? 0;

                    if (AppliedVoucher.MaxDiscount.HasValue)
                        max += Math.Min(
                            TotalPayment * ((AppliedVoucher.DiscountValue ?? 0) / 100m),
                            AppliedVoucher.MaxDiscount.Value);
                }
                if (UseLoyaltyPoints)
                    max -= LoyaltyPoints * 1000m;
                if (max < 0) max = 0;
                return max;
            }
        }

        public bool IsManualDiscountExceeded => _manualDiscount > MaxManualDiscount && MaxManualDiscount >= 0;

        public string ManualDiscountWarningText => IsManualDiscountExceeded
            ? $"Vượt giới hạn! Tối đa: {MaxManualDiscount:N0} đ"
            : "";

        private decimal _netPayment;
        public decimal NetPayment { get => _netPayment; set => SetProperty(ref _netPayment, value); }

        private decimal _customerGivenAmount;
        public decimal CustomerGivenAmount 
        { 
            get => _customerGivenAmount; 
            set 
            { 
                if (SetProperty(ref _customerGivenAmount, value)) 
                {
                    ChangeAmount = value - NetPayment;
                }
            } 
        }

        private string _customerGivenAmountInput = "0";
        public string CustomerGivenAmountInput
        {
            get => _customerGivenAmountInput;
            set
            {
                if (SetProperty(ref _customerGivenAmountInput, value))
                {
                    if (decimal.TryParse(value, out decimal result))
                    {
                        _customerGivenAmount = result >= 0 ? result : 0;
                        OnPropertyChanged(nameof(CustomerGivenAmount));
                        ChangeAmount = _customerGivenAmount - NetPayment;
                    }
                    else if (string.IsNullOrEmpty(value))
                    {
                        _customerGivenAmount = 0;
                        OnPropertyChanged(nameof(CustomerGivenAmount));
                        ChangeAmount = 0 - NetPayment;
                    }
                }
            }
        }

        private decimal _changeAmount;
        public decimal ChangeAmount { get => _changeAmount; set => SetProperty(ref _changeAmount, value); }
    }

    public class SellerPosViewModel : ViewModelBase
    {
        private readonly int _shopId;
        private string _searchKeyword = "";
        private ObservableCollection<Product> _products = new();
        private string _barcodeInput = "";
        private System.Threading.CancellationTokenSource? _searchCustomerCts;

        private Order? _lastOrder;
        private decimal _lastGivenAmount;
        private decimal _lastChangeAmount;
        public decimal OpeningFloat { get; private set; } = 0;

        private readonly IOrderService _orderService;

        // Tab management
        private ObservableCollection<PosOrderContext> _tabs = new();
        public ObservableCollection<PosOrderContext> Tabs { get => _tabs; set => SetProperty(ref _tabs, value); }

        private PosOrderContext _selectedTab;
        public PosOrderContext SelectedTab 
        { 
            get => _selectedTab; 
            set 
            { 
                if (SetProperty(ref _selectedTab, value))
                {
                    // Hook property changed to recalculate when tab properties change
                    if (_selectedTab != null)
                    {
                        _selectedTab.PropertyChanged -= SelectedTab_PropertyChanged;
                        _selectedTab.PropertyChanged += SelectedTab_PropertyChanged;
                    }
                }
            } 
        }

        private void SelectedTab_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PosOrderContext.SelectedPayment) ||
                e.PropertyName == nameof(PosOrderContext.UseLoyaltyPoints) ||
                e.PropertyName == nameof(PosOrderContext.ManualDiscount) ||
                e.PropertyName == nameof(PosOrderContext.ManualDiscountInput))
            {
                Recalculate();
            }
            else if (e.PropertyName == nameof(PosOrderContext.CustomerPhone))
            {
                _searchCustomerCts?.Cancel();
                _searchCustomerCts = new System.Threading.CancellationTokenSource();
                var token = _searchCustomerCts.Token;
                Task.Delay(350, token).ContinueWith(t =>
                {
                    if (!t.IsCanceled && !token.IsCancellationRequested)
                        System.Windows.Application.Current.Dispatcher.Invoke(() => _ = SearchCustomerAsync());
                }, TaskScheduler.Default);
            }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set 
            { 
                if (SetProperty(ref _searchKeyword, value))
                {
                    LoadProducts();
                }
            }
        }

        public string BarcodeInput
        {
            get => _barcodeInput;
            set 
            { 
                if (SetProperty(ref _barcodeInput, value))
                {
                    if (value.EndsWith("\n") || value.EndsWith("\r"))
                    {
                        ProcessBarcodeScan();
                    }
                }
            }
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        private bool _hasProducts;
        public bool HasProducts
        {
            get => _hasProducts;
            private set => SetProperty(ref _hasProducts, value);
        }

        public ICommand AddToCartCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand ApplyVoucherCommand { get; }
        
        public ICommand AddTabCommand { get; }
        public ICommand CloseTabCommand { get; }

        public ICommand EndShiftCommand { get; }
        public ICommand RefundCommand { get; }
        public ICommand ReprintReceiptCommand { get; }

        // ============ NEW: F-key commands & helpers ============
        public ICommand SelectPaymentCashCommand { get; }
        public ICommand SelectPaymentVNPayCommand { get; }
        public ICommand SelectPaymentMoMoCommand { get; }
        public ICommand HoldOrderCommand { get; }
        public ICommand ShowHeldOrdersCommand { get; }
        public ICommand LoadLastReceiptCommand { get; }

        // ============ NEW: State for hold orders ============
        public ObservableCollection<HeldOrderSnapshot> HeldOrders { get; } = new();

        // ============ NEW: Recent customers cache (5 last) ============
        public ObservableCollection<RecentCustomer> RecentCustomers { get; } = new();

        // ============ NEW: Quick cash amounts ============
        public decimal[] QuickCashAmounts { get; } = { 50000, 100000, 200000, 500000, 1000000 };

        public SellerPosViewModel()
        {
            _shopId = SessionManager.CurrentUser?.ShopId ?? 0;
            _orderService = OrderService.Instance;
            
            AddToCartCommand = new RelayCommand(o => ExecuteAddToCart(o as Product));
            RemoveItemCommand = new RelayCommand(o => ExecuteRemoveItem(o as PosCartItem));
            IncreaseQuantityCommand = new RelayCommand(o => ExecuteIncrease(o as PosCartItem));
            DecreaseQuantityCommand = new RelayCommand(o => ExecuteDecrease(o as PosCartItem));
            ClearCartCommand = new RelayCommand(_ => ExecuteClearCart());
            CheckoutCommand = new RelayCommand(_ => ExecuteCheckout());
            ApplyVoucherCommand = new RelayCommand(_ => ExecuteApplyVoucher());
            
            AddTabCommand = new RelayCommand(_ => ExecuteAddTab());
            CloseTabCommand = new RelayCommand(o => ExecuteCloseTab(o as PosOrderContext));
            EndShiftCommand = new RelayCommand(_ => ExecuteEndShift());
            RefundCommand = new RelayCommand(_ => ExecuteRefund());
            ReprintReceiptCommand = new RelayCommand(_ => ExecuteReprintReceipt(), _ => _lastOrder != null);

            // ============ NEW: F-key commands ============
            SelectPaymentCashCommand = new RelayCommand(_ => { if (SelectedTab != null) SelectedTab.SelectedPayment = 0; },
                _ => SelectedTab != null);
            SelectPaymentVNPayCommand = new RelayCommand(_ => { if (SelectedTab != null) SelectedTab.SelectedPayment = 1; },
                _ => SelectedTab != null);
            SelectPaymentMoMoCommand = new RelayCommand(_ => { if (SelectedTab != null) SelectedTab.SelectedPayment = 2; },
                _ => SelectedTab != null);
            HoldOrderCommand = new RelayCommand(_ => ExecuteHoldOrder(),
                _ => SelectedTab != null && SelectedTab.CartItems.Count > 0);
            ShowHeldOrdersCommand = new RelayCommand(_ => ExecuteShowHeldOrders());
            LoadLastReceiptCommand = new RelayCommand(_ => ExecuteReprintReceipt(), _ => _lastOrder != null);

            // Show Opening Float dialog
            var shiftWindow = new TMDT.Views.Seller.PosShiftWindow();
            shiftWindow.ShowDialog();
            OpeningFloat = shiftWindow.OpeningFloat;

            // Create initial tab
            ExecuteAddTab();

            LoadProducts();
            _ = LoadActiveVouchersAsync();
            _ = LoadRecentCustomersAsync();
            _ = StartPendingPaymentWatcherAsync();
            StartOfflineSyncTimer();
            TMDT.Services.OfflinePaymentQueue.Instance.Changed += RefreshOfflineBadge;
        }

        private async System.Threading.Tasks.Task StartPendingPaymentWatcherAsync()
        {
            const int timeoutMinutes = 5;
            while (!System.Threading.CancellationToken.None.IsCancellationRequested)
            {
                try
                {
                    using var context = new TmdtContext();
                    var cutoff = DateTime.Now.AddMinutes(-timeoutMinutes);
                    var expired = await context.Orders
                        .Where(o => o.ShopId == _shopId
                                    && o.AddressId == null
                                    && o.OrderStatus == "AwaitingPayment"
                                    && o.OrderDate < cutoff)
                        .ToListAsync();

                    if (expired.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"[POS] Auto-cancelling {expired.Count} expired pending payment orders");
                        foreach (var order in expired)
                        {
                            try { await _orderService.CancelPosOrderAsync(order.OrderId); }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[POS] Auto-cancel fail #{order.OrderId}: {ex.Message}"); }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[POS] Pending watcher error: {ex.Message}");
                }

                await System.Threading.Tasks.Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private void ExecuteAddTab()
        {
            // 🟢 Auto-label theo thứ tự A/B/C… thay vì "Đơn 1, 2, 3" giúp nhân viên nhận biết nhanh ai là ai
            var index = Tabs.Count;
            string letter = IndexToLetter(index);
            var newTab = new PosOrderContext { TabTitle = $"Đơn {index + 1}", BillLabel = $"Khách {letter}" };
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }

        private static string IndexToLetter(int index)
        {
            // 0 → A, 1 → B, … 25 → Z, 26 → AA, 27 → AB…
            string s = "";
            int n = index;
            while (n >= 0)
            {
                s = (char)('A' + (n % 26)) + s;
                n = n / 26 - 1;
            }
            return s;
        }

        private void ExecuteCloseTab(PosOrderContext? tab)
        {
            if (tab != null && Tabs.Contains(tab))
            {
                // 🟢 Cảnh báo khi đóng tab mà giỏ chưa trống — tránh mất dữ liệu khi đóng nhầm
                if (tab.CartItems.Count > 0)
                {
                    var totalItems = tab.CartItems.Sum(i => i.Quantity);
                    var msg = $"Tab này còn {tab.CartItems.Count} sản phẩm ({totalItems} món, tổng {tab.TotalPayment:N0} đ).\n\nĐóng tab sẽ XÓA toàn bộ dữ liệu chưa thanh toán.\n\nBạn có chắc muốn đóng?";
                    if (MessageBox.Show(msg, "Xác nhận đóng tab", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        return;
                }

                if (Tabs.Count == 1)
                {
                    // If last tab, just clear it
                    ExecuteClearCart();
                    tab.CustomerPhone = "";
                    tab.VoucherCode = "";
                }
                else
                {
                    Tabs.Remove(tab);
                    SelectedTab = Tabs.Last();
                }
            }
        }

        private async void ExecuteEndShift()
        {
            try
            {
                using var context = new TmdtContext();
                var today = DateTime.Today;
                
                // Fetch POS orders for today (AddressId == null)
                var orders = await context.Orders
                    .Where(o => o.ShopId == _shopId && o.AddressId == null && o.OrderDate.HasValue && o.OrderDate.Value.Date == today && o.OrderStatus != "Cancelled")
                    .ToListAsync();

                var reportVM = new PosReportViewModel
                {
                    TotalOrders = orders.Count,
                    TotalRevenue = orders.Sum(o => o.TotalAmount ?? 0),
                    TotalCash = orders.Where(o => o.PaymentMethod == "Cash").Sum(o => o.TotalAmount ?? 0),
                    TotalMoMo = orders.Where(o => o.PaymentMethod == "MoMo").Sum(o => o.TotalAmount ?? 0),
                    TotalVNPay = orders.Where(o => o.PaymentMethod == "VNPay").Sum(o => o.TotalAmount ?? 0),
                    OpeningFloat = OpeningFloat
                };

                var reportWindow = new TMDT.Views.Seller.PosReportWindow(reportVM);
                reportWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo báo cáo chốt ca: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadProducts()
        {
            try
            {
                using var context = new TmdtContext();
                var query = context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .Where(p => p.ShopId == _shopId && p.Status != "Deleted");
                    
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    var keyword = SearchKeyword.ToLower();
                    query = query.Where(p => p.ProductName.ToLower().Contains(keyword));
                }

                var list = await query.Take(200).ToListAsync();
                Products = new ObservableCollection<Product>(list);
                HasProducts = Products.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sản phẩm: {ex.Message}");
            }
        }

        private ObservableCollection<Voucher> _activeVouchers = new();
        public ObservableCollection<Voucher> ActiveVouchers
        {
            get => _activeVouchers;
            set => SetProperty(ref _activeVouchers, value);
        }

        public async System.Threading.Tasks.Task LoadActiveVouchersAsync()
        {
            try
            {
                using var context = new TmdtContext();
                var list = await context.Vouchers
                    .Where(v => v.ShopId == _shopId && 
                                v.IsActive == true && 
                                v.StartDate <= DateTime.Now && 
                                v.EndDate >= DateTime.Now &&
                                (v.TotalQuantity == null || v.UsedCount < v.TotalQuantity))
                    .ToListAsync();
                ActiveVouchers = new ObservableCollection<Voucher>(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải active vouchers: {ex.Message}");
            }
        }

        private void ProcessBarcodeScan()
        {
            string code = BarcodeInput.Trim();
            if (string.IsNullOrEmpty(code)) return;
            
            var product = Products.FirstOrDefault(p => p.ProductId.ToString() == code || p.ProductName.Contains(code) || (p.ProductVariants != null && p.ProductVariants.Any(v => v.Sku == code)));
            
            if (product != null)
            {
                var variant = product.ProductVariants?.FirstOrDefault(v => v.Sku == code);
                ExecuteAddToCart(product, variant);
            }
            else
            {
                using var context = new TmdtContext();
                var dbProduct = context.Products.Include(p => p.ProductVariants).FirstOrDefault(p => p.ShopId == _shopId && (p.ProductId.ToString() == code || p.ProductName.Contains(code) || (p.ProductVariants != null && p.ProductVariants.Any(v => v.Sku == code))));
                if (dbProduct != null)
                {
                    var variant = dbProduct.ProductVariants?.FirstOrDefault(v => v.Sku == code);
                    ExecuteAddToCart(dbProduct, variant);
                }
            }
            
            BarcodeInput = ""; 
        }

        private void ExecuteAddToCart(Product? product, ProductVariant? preSelectedVariant = null)
        {
            if (product == null || SelectedTab == null) return;

            ProductVariant? selectedVariant = preSelectedVariant;

            if (selectedVariant == null && product.ProductVariants != null && product.ProductVariants.Any())
            {
                var variantWindow = new TMDT.Views.Components.VariantSelectionWindow(product.ProductVariants);
                if (variantWindow.ShowDialog() == true)
                {
                    selectedVariant = variantWindow.SelectedVariant;
                }
                else
                {
                    return; // Canceled selection
                }
            }

            int stock = selectedVariant != null ? (selectedVariant.Quantity ?? 0) : (product.StockQuantity ?? 0);

            if (stock <= 0)
            {
                MessageBox.Show("Sản phẩm đã hết hàng trong kho!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = SelectedTab.CartItems.FirstOrDefault(i => i.ProductId == product.ProductId && i.VariantId == selectedVariant?.VariantId);
            if (existing != null)
            {
                if (existing.Quantity < stock)
                {
                    existing.Quantity++;
                }
                else
                {
                    MessageBox.Show("Số lượng mua vượt quá tồn kho!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                SelectedTab.CartItems.Add(new PosCartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    VariantId = selectedVariant?.VariantId,
                    VariantName = selectedVariant?.VariantName,
                    UnitPrice = product.Price + (selectedVariant?.ExtraPrice ?? 0),
                    StockQuantity = stock,
                    Quantity = 1
                });
            }
            
            Recalculate();
        }

        private void ExecuteRemoveItem(PosCartItem? item)
        {
            if (item != null && SelectedTab != null)
            {
                SelectedTab.CartItems.Remove(item);
                Recalculate();
            }
        }

        private void ExecuteIncrease(PosCartItem? item)
        {
            if (item != null && item.Quantity < item.StockQuantity)
            {
                item.Quantity++;
                Recalculate();
            }
        }

        private void ExecuteDecrease(PosCartItem? item)
        {
            if (item != null && item.Quantity > 1)
            {
                item.Quantity--;
                Recalculate();
            }
        }
        
        private void ExecuteClearCart()
        {
            if (SelectedTab != null && SelectedTab.CartItems.Count > 0)
            {
                var result = MessageBox.Show("Bạn có muốn làm trống giỏ hàng?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SelectedTab.CartItems.Clear();
                    Recalculate();
                }
            }
        }

        private void Recalculate()
        {
            if (SelectedTab == null) return;

            SelectedTab.TotalPayment = SelectedTab.CartItems.Sum(i => i.LineTotal);
            
            decimal pointsDiscount = 0;
            if (SelectedTab.UseLoyaltyPoints)
            {
                pointsDiscount = SelectedTab.LoyaltyPoints * 1000m;
            }

            decimal voucherDiscount = 0;
            if (SelectedTab.AppliedVoucher != null)
            {
                if (SelectedTab.AppliedVoucher.DiscountType == "Percentage")
                    voucherDiscount = SelectedTab.TotalPayment * ((SelectedTab.AppliedVoucher.DiscountValue ?? 0) / 100m);
                else
                    voucherDiscount = SelectedTab.AppliedVoucher.DiscountValue ?? 0;

                if (SelectedTab.AppliedVoucher.MaxDiscount.HasValue && voucherDiscount > SelectedTab.AppliedVoucher.MaxDiscount.Value)
                    voucherDiscount = SelectedTab.AppliedVoucher.MaxDiscount.Value;
            }

            SelectedTab.DiscountAmount = pointsDiscount + voucherDiscount + SelectedTab.ManualDiscount;

            // Đảm bảo discount không vượt quá tổng tiền hàng
            if (SelectedTab.DiscountAmount > SelectedTab.TotalPayment)
                SelectedTab.DiscountAmount = SelectedTab.TotalPayment;

            SelectedTab.NetPayment = SelectedTab.TotalPayment - SelectedTab.DiscountAmount;
            if (SelectedTab.NetPayment < 0) SelectedTab.NetPayment = 0;

            // Kích hoạt re-evaluate các properties phụ thuộc vào TotalPayment
            var tempDiscount = SelectedTab.ManualDiscount;
            SelectedTab.ManualDiscount = tempDiscount; // re-triggers IsManualDiscountExceeded etc.

            if (SelectedTab.IsCashPayment)
            {
                if (SelectedTab.CustomerGivenAmount == 0)
                {
                    SelectedTab.CustomerGivenAmount = SelectedTab.NetPayment;
                }
                SelectedTab.ChangeAmount = SelectedTab.CustomerGivenAmount - SelectedTab.NetPayment;
            }
            else
            {
                SelectedTab.CustomerGivenAmount = 0;
                SelectedTab.ChangeAmount = 0;
            }
        }

        private async System.Threading.Tasks.Task SearchCustomerAsync()
        {
            if (SelectedTab == null) return;

            var phone = SelectedTab.CustomerPhone?.Trim() ?? "";

            // Reset nếu SĐT quá ngắn hoặc trống — bao gồm cả điểm + useLoyalty để tránh dùng dữ liệu cũ
            if (phone.Length < 9)
            {
                ResetCustomer();
                return;
            }

            // 🟢 Validate format SĐT VN (10 số, đầu 03/05/07/08/09)
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(03|05|07|08|09)\d{8}$"))
            {
                ResetCustomer();
                SelectedTab.CustomerLookupStatus = CustomerLookupStatus.NotFound;
                return;
            }

            SelectedTab.CustomerLookupStatus = CustomerLookupStatus.Searching;

            try
            {
                using var context = new TmdtContext();
                var user = await context.Users.FirstOrDefaultAsync(u => u.Phone == phone);
                if (user != null)
                {
                    SelectedTab.BuyerId = user.UserId;
                    SelectedTab.CustomerName = user.FullName ?? user.Email;
                    SelectedTab.LoyaltyPoints = user.LoyaltyPoints ?? 0;
                    SelectedTab.CustomerLookupStatus = CustomerLookupStatus.Found;

                    // Đếm số đơn hàng đã hoàn thành của khách tại shop này
                    var count = await context.Orders.CountAsync(o => o.BuyerId == user.UserId && o.ShopId == _shopId);
                    SelectedTab.OrderCount = count;
                }
                else
                {
                    ResetCustomer();
                    SelectedTab.CustomerLookupStatus = CustomerLookupStatus.NotFound;
                }
            }
            catch (Exception ex)
            {
                SelectedTab.CustomerLookupStatus = CustomerLookupStatus.None;
                System.Diagnostics.Debug.WriteLine($"Lỗi tìm khách hàng: {ex.Message}");
            }
        }

        private void ResetCustomer()
        {
            if (SelectedTab == null) return;
            SelectedTab.BuyerId = null;
            SelectedTab.CustomerName = "Khách vãng lai";
            SelectedTab.LoyaltyPoints = 0;
            SelectedTab.OrderCount = 0;
            SelectedTab.UseLoyaltyPoints = false;
            SelectedTab.AppliedVoucher = null;
            SelectedTab.VoucherCode = "";
            SelectedTab.CustomerLookupStatus = CustomerLookupStatus.None;
            Recalculate();
        }

        private async void ExecuteApplyVoucher()
        {
            if (SelectedTab == null) return;

            if (string.IsNullOrWhiteSpace(SelectedTab.VoucherCode))
            {
                SelectedTab.AppliedVoucher = null;
                Recalculate();
                return;
            }

            using var context = new TmdtContext();
            var voucher = await context.Vouchers.FirstOrDefaultAsync(v => 
                v.VoucherCode == SelectedTab.VoucherCode && 
                v.ShopId == _shopId && 
                v.IsActive == true &&
                v.StartDate <= DateTime.Now && 
                v.EndDate >= DateTime.Now);

            if (voucher != null)
            {
                if (SelectedTab.TotalPayment < (voucher.MinOrderValue ?? 0))
                {
                    MessageBox.Show($"Đơn hàng chưa đạt giá trị tối thiểu {voucher.MinOrderValue:N0}đ để áp dụng mã này.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SelectedTab.AppliedVoucher = null;
                }
                else if (voucher.TotalQuantity != null && voucher.UsedCount >= voucher.TotalQuantity)
                {
                    MessageBox.Show("Mã giảm giá đã hết lượt sử dụng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SelectedTab.AppliedVoucher = null;
                }
                else if (SelectedTab.BuyerId.HasValue)
                {
                    SelectedTab.AppliedVoucher = voucher;
                    MessageBox.Show("Áp dụng mã giảm giá thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    SelectedTab.AppliedVoucher = voucher;
                    MessageBox.Show("Áp dụng mã giảm giá thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Mã giảm giá không hợp lệ hoặc đã hết hạn.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                SelectedTab.AppliedVoucher = null;
            }
            
            Recalculate();
        }

        private async void ExecuteCheckout()
        {
            if (SelectedTab == null) return;

            if (SelectedTab.IsManualDiscountExceeded)
            {
                MessageBox.Show($"Giảm giá thủ công vượt quá tổng tiền hàng (tối đa: {SelectedTab.MaxManualDiscount:N0} đ)!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedTab.CartItems.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedTab.NetPayment < 0)
            {
                MessageBox.Show("Số tiền phải trả không hợp lệ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedTab.IsCashPayment && SelectedTab.CustomerGivenAmount < SelectedTab.NetPayment)
            {
                MessageBox.Show("Tiền khách đưa không đủ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Map payment method
            string paymentMethod = SelectedTab.SelectedPayment switch
            {
                0 => "Cash",
                1 => "VNPay",
                2 => "MoMo",
                _ => "Cash"
            };

            // Prepare items — tính TotalPrice = UnitPrice * Quantity vì CartOrderItem là DTO set sẵn
            var items = SelectedTab.CartItems.Select(i => new CartOrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                VariantId = i.VariantId,
                VariantName = i.VariantName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            IOrderService orderService = OrderService.Instance;

            // 🟢 Nếu đang sửa đơn đã có trong DB (PendingPosOrderId) → UPDATE thay vì INSERT
            // Điều kiện: chỉ áp dụng cho MoMo/VNPay (Cash: không cho phép sửa vì đã hoàn tất)
            Order? result = null;
            bool isEditingExisting = SelectedTab.PendingPosOrderId.HasValue && paymentMethod != "Cash";

            if (isEditingExisting)
            {
                result = await orderService.UpdatePosOrderAsync(
                    SelectedTab.PendingPosOrderId!.Value,
                    items,
                    SelectedTab.ManualDiscount,
                    SelectedTab.AppliedVoucher?.VoucherId,
                    SelectedTab.UseLoyaltyPoints ? SelectedTab.LoyaltyPoints : 0);

                if (result == null)
                {
                    MessageBox.Show("Lỗi cập nhật đơn hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                // Tạo mới — Cash: Completed ngay, MoMo/VNPay: AwaitingPayment
                string initialStatus = paymentMethod == "Cash" ? "Completed" : "AwaitingPayment";
                result = await orderService.CreatePosOrderAsync(
                    SelectedTab.BuyerId, _shopId,
                    SelectedTab.AppliedVoucher?.VoucherId, paymentMethod, items,
                    SelectedTab.UseLoyaltyPoints ? SelectedTab.LoyaltyPoints : 0,
                    SelectedTab.ManualDiscount,
                    initialStatus);
            }

            if (result == null)
            {
                MessageBox.Show("Lỗi thanh toán: Không thể tạo đơn hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 🟢 Lưu OrderId vào Tab context — nếu user chọn "Sửa đơn" ở QR window, ta có thể update lại DB
            SelectedTab.PendingPosOrderId = result.OrderId;

            try
            {
                if (paymentMethod == "Cash")
                {
                    _lastOrder = result;
                    _lastGivenAmount = SelectedTab.CustomerGivenAmount;
                    _lastChangeAmount = SelectedTab.ChangeAmount;

                    MessageBox.Show($"Thanh toán thành công!\nTiền thừa trả khách: {SelectedTab.ChangeAmount:N0} đ",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    var receiptWindow = new TMDT.Views.Seller.ReceiptWindow(result, SelectedTab.CustomerGivenAmount, SelectedTab.ChangeAmount);
                    receiptWindow.ShowDialog();

                    TrackRecentCustomerFromTab(SelectedTab, result);
                    ExecuteCloseTab(SelectedTab);
                }
                else if (paymentMethod == "MoMo")
                {
                    var mockWindow = new TMDT.Views.Components.MoMoMockWindow(SelectedTab.NetPayment, TMDT.Services.PosSettingsHelper.GetMoMoPhone(), result.OrderCode);
                    var dialogResult = mockWindow.ShowDialog();

                    if (dialogResult == true)
                    {
                        await orderService.ConfirmPosOrderAsync(result.OrderId, mockWindow.TransactionCode);
                        _lastOrder = result;
                        _lastGivenAmount = SelectedTab.CustomerGivenAmount;
                        _lastChangeAmount = SelectedTab.ChangeAmount;

                        MessageBox.Show("Thanh toán MoMo thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        var receiptWindow = new TMDT.Views.Seller.ReceiptWindow(result, SelectedTab.CustomerGivenAmount, SelectedTab.ChangeAmount);
                        receiptWindow.ShowDialog();
                        TrackRecentCustomerFromTab(SelectedTab, result);
                        SelectedTab.PendingPosOrderId = null; // Đã xác nhận → không còn pending
                        ExecuteCloseTab(SelectedTab);
                    }
                    else if (mockWindow.UserChoseOffline)
                    {
                        // 🟢 Cashier xác nhận offline (mạng lỗi) → set CompletedOffline + enqueue sync
                        await ConfirmPaymentOfflineAsync(orderService, result, "MoMo");
                    }
                    else if (mockWindow.UserChoseToEdit)
                    {
                        // 🟢 User chọn "Sửa đơn" → load cart về từ DB OrderDetails, giữ PendingPosOrderId để update
                        LoadOrderDetailsIntoTab(result);
                        MessageBox.Show($"Đã tải lại đơn hàng {result.OrderCode} về POS.\n\nBạn có thể chỉnh sửa số lượng / thêm sản phẩm, sau đó bấm THANH TOÁN lại.",
                            "Sửa đơn hàng", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // User hủy → rollback order AwaitingPayment
                        await orderService.CancelPosOrderAsync(result.OrderId);
                        SelectedTab.PendingPosOrderId = null;
                    }
                }
                else if (paymentMethod == "VNPay")
                {
                    var payUrl = TMDT.Services.VNPayService.CreatePaymentUrl(result);
                    if (string.IsNullOrEmpty(payUrl))
                    {
                        MessageBox.Show("Không thể tạo link thanh toán VNPAY. Đã hủy đơn.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        await orderService.CancelPosOrderAsync(result.OrderId);
                        SelectedTab.PendingPosOrderId = null;
                        return;
                    }

                    var mockWindow = new TMDT.Views.Components.VNPayMockWindow(SelectedTab.NetPayment, result.OrderCode);
                    var dialogResult = mockWindow.ShowDialog();

                    if (dialogResult == true)
                    {
                        await orderService.ConfirmPosOrderAsync(result.OrderId, $"VNP_{DateTime.Now.Ticks}");
                        _lastOrder = result;
                        _lastGivenAmount = SelectedTab.CustomerGivenAmount;
                        _lastChangeAmount = SelectedTab.ChangeAmount;

                        MessageBox.Show("Thanh toán VNPAY thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        var receiptWindow = new TMDT.Views.Seller.ReceiptWindow(result, SelectedTab.CustomerGivenAmount, SelectedTab.ChangeAmount);
                        receiptWindow.ShowDialog();
                        TrackRecentCustomerFromTab(SelectedTab, result);
                        SelectedTab.PendingPosOrderId = null;
                        ExecuteCloseTab(SelectedTab);
                    }
                    else if (mockWindow.UserChoseOffline)
                    {
                        // 🟢 Cashier xác nhận offline (mạng lỗi) → set CompletedOffline + enqueue sync
                        await ConfirmPaymentOfflineAsync(orderService, result, "VNPay");
                    }
                    else if (mockWindow.UserChoseToEdit)
                    {
                        // 🟢 Sửa đơn: load cart về từ DB OrderDetails
                        LoadOrderDetailsIntoTab(result);
                        MessageBox.Show($"Đã tải lại đơn hàng {result.OrderCode} về POS.\n\nBạn có thể chỉnh sửa số lượng / thêm sản phẩm, sau đó bấm THANH TOÁN lại.",
                            "Sửa đơn hàng", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        await orderService.CancelPosOrderAsync(result.OrderId);
                        SelectedTab.PendingPosOrderId = null;
                    }
                }
                else
                {
                    MessageBox.Show($"Chưa hỗ trợ quét mã QR của {paymentMethod} (Chưa tích hợp API).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await orderService.CancelPosOrderAsync(result.OrderId);
                    SelectedTab.PendingPosOrderId = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi trong quá trình thanh toán: {ex.Message}\nĐơn hàng sẽ bị hủy.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                try { await orderService.CancelPosOrderAsync(result.OrderId); SelectedTab.PendingPosOrderId = null; } catch { }
            }
        }

        /// <summary>
        /// Load OrderDetails từ DB về SelectedTab.CartItems — dùng khi user chọn "Sửa đơn" ở QR window.
        /// Giữ nguyên PendingPosOrderId để lần thanh toán tiếp theo sẽ UPDATE thay vì INSERT.
        /// </summary>
        private void LoadOrderDetailsIntoTab(Order order)
        {
            if (SelectedTab == null || order == null) return;

            SelectedTab.CartItems.Clear();
            foreach (var detail in order.OrderDetails ?? new List<OrderDetail>())
            {
                SelectedTab.CartItems.Add(new PosCartItem
                {
                    ProductId = detail.ProductId ?? 0,
                    ProductName = detail.ProductNameSnapshot ?? "",
                    VariantId = detail.VariantId,
                    UnitPrice = detail.UnitPrice ?? 0,
                    Quantity = detail.Quantity ?? 0,
                    StockQuantity = 0 // Không cần — chỉ dùng cho hiển thị
                });
            }
            SelectedTab.TabTitle = $"Sửa: {order.OrderCode}";

            // Reset các thuộc tính giảm giá vì SubTotal đã thay đổi
            SelectedTab.UseLoyaltyPoints = false;
            SelectedTab.AppliedVoucher = null;
            SelectedTab.VoucherCode = "";
            SelectedTab.ManualDiscount = 0;
            SelectedTab.ManualDiscountInput = "0";
            Recalculate();
        }

        private void ExecuteReprintReceipt()
        {
            if (_lastOrder != null)
            {
                var receiptWindow = new TMDT.Views.Seller.ReceiptWindow(_lastOrder, _lastGivenAmount, _lastChangeAmount);
                receiptWindow.ShowDialog();
            }
        }

        /// <summary>
        /// Ghi nhận khách vừa thanh toán vào RecentCustomers (cache 5 người gần nhất).
        /// </summary>
        private void TrackRecentCustomerFromTab(PosOrderContext tab, Order? order)
        {
            if (order == null || tab == null) return;
            if (order.BuyerId == null) return;
            PushRecentCustomer(order.BuyerId, order.Buyer?.Phone, order.Buyer?.FullName);
        }

        private void ExecuteRefund()
        {
            var refundWindow = new TMDT.Views.Seller.PosRefundWindow(_shopId);
            refundWindow.Owner = Application.Current.MainWindow;
            refundWindow.ShowDialog();
        }

        // ===========================================================
        // NEW METHODS (F-keys, Hold, Recent Customers)
        // ===========================================================

        private async System.Threading.Tasks.Task LoadRecentCustomersAsync()
        {
            try
            {
                using var context = new TmdtContext();
                var recent = await context.Orders
                    .AsNoTracking()
                    .Where(o => o.ShopId == _shopId
                                && o.AddressId == null
                                && o.OrderStatus == "Completed"
                                && o.BuyerId != null)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new { o.BuyerId, o.OrderDate })
                    .Take(100)
                    .ToListAsync();

                // Distinct by BuyerId, keep last 5 distinct
                var top5 = recent
                    .Where(x => x.BuyerId != null)
                    .GroupBy(x => x.BuyerId!.Value)
                    .Select(g => new { BuyerId = g.Key, LastOrder = g.Max(x => x.OrderDate) })
                    .OrderByDescending(x => x.LastOrder)
                    .Take(5)
                    .ToList();

                var buyerIds = top5.Select(x => x.BuyerId).ToList();
                var buyers = await context.Users
                    .AsNoTracking()
                    .Where(u => buyerIds.Contains(u.UserId))
                    .ToDictionaryAsync(u => u.UserId, u => u);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentCustomers.Clear();
                    foreach (var item in top5)
                    {
                        if (buyers.TryGetValue(item.BuyerId, out var u))
                        {
                            RecentCustomers.Add(new RecentCustomer
                            {
                                UserId = u.UserId,
                                FullName = u.FullName ?? "Khách",
                                Phone = u.Phone ?? "",
                                LoyaltyPoints = u.LoyaltyPoints ?? 0
                            });
                        }
                    }
                });
            }
            catch
            {
                // silent — recent customers is a nice-to-have
            }
        }

        public void PushRecentCustomer(int? buyerId, string? phone, string? name)
        {
            if (buyerId == null || string.IsNullOrWhiteSpace(phone)) return;

            // Move to top if exists
            var existing = RecentCustomers.FirstOrDefault(c => c.UserId == buyerId);
            if (existing != null) RecentCustomers.Remove(existing);

            RecentCustomers.Insert(0, new RecentCustomer
            {
                UserId = buyerId.Value,
                FullName = name ?? "Khách",
                Phone = phone,
                LoyaltyPoints = 0
            });

            // Keep only top 5
            while (RecentCustomers.Count > 5)
                RecentCustomers.RemoveAt(RecentCustomers.Count - 1);
        }

        private void ExecuteHoldOrder()
        {
            if (SelectedTab == null || SelectedTab.CartItems.Count == 0) return;

            var snapshot = new HeldOrderSnapshot
            {
                HeldAt = DateTime.Now,
                HeldBy = SessionManager.CurrentUser?.FullName ?? "Cashier",
                CustomerName = string.IsNullOrEmpty(SelectedTab.CustomerName) ? "Khách vãng lai" : SelectedTab.CustomerName,
                CustomerPhone = SelectedTab.CustomerPhone ?? "",
                CartItems = SelectedTab.CartItems.Select(i => new PosCartItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    VariantId = i.VariantId,
                    VariantName = i.VariantName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    StockQuantity = i.StockQuantity
                }).ToList(),
                VoucherCode = SelectedTab.VoucherCode,
                ManualDiscount = SelectedTab.ManualDiscount,
                TotalPayment = SelectedTab.TotalPayment,
                DiscountAmount = SelectedTab.DiscountAmount,
                NetPayment = SelectedTab.NetPayment,
                Note = $"Tab #{Tabs.IndexOf(SelectedTab) + 1}"
            };

            HeldOrders.Add(snapshot);
            Tabs.Remove(SelectedTab);

            if (Tabs.Count == 0) ExecuteAddTab();
            else SelectedTab = Tabs.Last();

            System.Windows.MessageBox.Show(
                $"Đã treo đơn '{snapshot.CustomerName}' ({snapshot.CartItems.Count} SP).\nTổng cộng đang treo: {HeldOrders.Count} đơn.",
                "Treo đơn", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteShowHeldOrders()
        {
            if (HeldOrders.Count == 0)
            {
                System.Windows.MessageBox.Show("Không có đơn nào đang treo.", "Đơn treo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new TMDT.Views.Seller.HeldOrdersDialog(HeldOrders);
            dlg.Owner = System.Windows.Application.Current.MainWindow;
            if (dlg.ShowDialog() == true && dlg.SelectedSnapshot != null)
            {
                ResumeHeldOrder(dlg.SelectedSnapshot);
            }
        }

        public void ResumeHeldOrder(HeldOrderSnapshot snapshot)
        {
            // Close current empty tab if any
            if (SelectedTab != null && SelectedTab.CartItems.Count == 0)
            {
                Tabs.Remove(SelectedTab);
            }

            var resumed = new PosOrderContext
            {
                TabTitle = $"📋 {snapshot.CustomerName}",
                CustomerName = snapshot.CustomerName,
                CustomerPhone = snapshot.CustomerPhone,
                VoucherCode = snapshot.VoucherCode,
                ManualDiscount = snapshot.ManualDiscount ?? 0m,
                CartItems = new ObservableCollection<PosCartItem>(snapshot.CartItems)
            };

            // re-attach property change handlers
            foreach (var item in resumed.CartItems)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PosCartItem.Quantity) || e.PropertyName == nameof(PosCartItem.UnitPrice))
                    {
                        RecalculateTotals(resumed);
                    }
                };
            }

            resumed.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PosOrderContext.CustomerPhone) ||
                    e.PropertyName == nameof(PosOrderContext.VoucherCode) ||
                    e.PropertyName == nameof(PosOrderContext.ManualDiscountInput) ||
                    e.PropertyName == nameof(PosOrderContext.CartItems))
                {
                    RecalculateTotals(resumed);
                }
            };

            Tabs.Add(resumed);
            SelectedTab = resumed;
            HeldOrders.Remove(snapshot);

            RecalculateTotals(resumed);
        }

        private void RecalculateTotals(PosOrderContext ctx)
        {
            decimal subTotal = ctx.CartItems.Sum(i => i.LineTotal);
            ctx.TotalPayment = subTotal;
            decimal voucherDiscount = 0;
            // re-apply voucher if any
            if (!string.IsNullOrWhiteSpace(ctx.VoucherCode))
            {
                var v = ActiveVouchers.FirstOrDefault(x =>
                    string.Equals(x.VoucherCode, ctx.VoucherCode, StringComparison.OrdinalIgnoreCase));
                if (v != null && subTotal >= (v.MinOrderValue ?? 0))
                {
                    voucherDiscount = v.DiscountType == "Percent"
                        ? Math.Min(subTotal * (v.DiscountValue ?? 0) / 100m, v.MaxDiscount ?? decimal.MaxValue)
                        : Math.Min(v.DiscountValue ?? 0m, v.MaxDiscount ?? decimal.MaxValue);
                }
            }
            ctx.DiscountAmount = voucherDiscount + ctx.ManualDiscount;
            ctx.NetPayment = subTotal - ctx.DiscountAmount;
        }

        public void QuickCashSet(decimal amount)
        {
            if (SelectedTab == null) return;
            SelectedTab.CustomerGivenAmountInput = ((long)amount).ToString();
        }

        /// <summary>
        /// 🟢 Xác nhận thanh toán offline (mạng lỗi) — set CompletedOffline, enqueue sync, vẫn in hóa đơn và đóng tab.
        /// </summary>
        private async Task ConfirmPaymentOfflineAsync(IOrderService orderService, Order order, string paymentMethod)
        {
            try
            {
                var tx = $"OFFLINE_{DateTime.Now.Ticks}";
                var ok = await orderService.ConfirmPosOrderOfflineAsync(order.OrderId, tx);
                if (!ok)
                {
                    MessageBox.Show("Không thể xác nhận offline. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Enqueue để sync sau
                TMDT.Services.OfflinePaymentQueue.Instance.Enqueue(new TMDT.Services.OfflinePaymentEntry
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode ?? "",
                    PaymentMethod = paymentMethod,
                    Amount = order.TotalAmount ?? 0,
                    TransactionCode = tx,
                    CreatedAt = DateTime.Now,
                    Note = "POS xác nhận offline do mất mạng"
                });

                _lastOrder = order;
                _lastGivenAmount = SelectedTab?.CustomerGivenAmount ?? 0;
                _lastChangeAmount = SelectedTab?.ChangeAmount ?? 0;

                MessageBox.Show(
                    $"✅ Đã nhận tiền OFFLINE!\n\n" +
                    $"Mạng đang lỗi — đơn {order.OrderCode} đã được lưu vào hàng chờ sync.\n" +
                    $"Khi có mạng POS sẽ tự đồng bộ và cộng tiền vào ví shop.\n\n" +
                    $"Số tiền: {order.TotalAmount:N0} đ",
                    "Thanh toán offline", MessageBoxButton.OK, MessageBoxImage.Information);

                // Vẫn in bill để khách có hóa đơn tạm
                var receiptWindow = new TMDT.Views.Seller.ReceiptWindow(order, _lastGivenAmount, _lastChangeAmount);
                receiptWindow.ShowDialog();

                TrackRecentCustomerFromTab(SelectedTab!, order);
                if (SelectedTab != null)
                {
                    SelectedTab.PendingPosOrderId = null;
                    ExecuteCloseTab(SelectedTab);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi offline: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 🟢 Tick theo backoff schedule (1s → 2s → 4s → ... → 60s max):
        /// - Lấy các entry đã đến giờ retry (NextRetryAt ≤ Now)
        /// - Gọi SyncOfflinePosOrderAsync
        /// - Thành công: mark synced
        /// - Thất bại: MarkRetryFailed → push NextRetryAt ra xa theo exponential backoff
        /// - Re-arm timer với interval = sớ nhất đến retry kế tiếp (min 1s, max 60s)
        /// </summary>
        private System.Windows.Threading.DispatcherTimer? _syncTimer;
        private void StartOfflineSyncTimer()
        {
            _syncTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _syncTimer.Tick += async (_, _) => await SyncTickAsync();
            _syncTimer.Start();
        }

        private async System.Threading.Tasks.Task SyncTickAsync()
        {
            if (_syncTimer == null) return;

            var queue = TMDT.Services.OfflinePaymentQueue.Instance;
            var ready = queue.GetReadyForRetry();
            if (ready.Count == 0)
            {
                RearmSyncTimer();
                return;
            }

            IsSyncingNow = true;
            bool anyChanged = false;
            try
            {
                foreach (var entry in ready)
                {
                    try
                    {
                        var ok = await OrderService.Instance.SyncOfflinePosOrderAsync(entry.OrderId);
                        if (ok)
                        {
                            queue.MarkSynced(entry.OrderId, DateTime.Now);
                            anyChanged = true;
                        }
                        else
                        {
                            // Server không throw nhưng trả false (order không ở CompletedOffline) → mark synced để kết thúc loop
                            queue.MarkSynced(entry.OrderId, DateTime.Now);
                            anyChanged = true;
                        }
                    }
                    catch
                    {
                        // 🟢 Mạng vẫn lỗi → push NextRetryAt ra xa theo backoff
                        queue.MarkRetryFailed(entry.OrderId);
                        anyChanged = true;
                    }
                }
            }
            finally
            {
                IsSyncingNow = false;
            }
            if (anyChanged)
                OnPropertyChanged(nameof(OfflinePendingBadgeText));

            RearmSyncTimer();
        }

        private void RearmSyncTimer()
        {
            if (_syncTimer == null) return;

            var earliest = TMDT.Services.OfflinePaymentQueue.Instance.GetEarliestNextRetry();
            if (!earliest.HasValue)
            {
                // Không còn pending → tick 30s để phát hiện entry mới
                _syncTimer.Interval = TimeSpan.FromSeconds(30);
                return;
            }

            var delay = earliest.Value - DateTime.Now;
            if (delay < TimeSpan.FromSeconds(1)) delay = TimeSpan.FromSeconds(1);
            if (delay > TimeSpan.FromSeconds(60)) delay = TimeSpan.FromSeconds(60);
            _syncTimer.Interval = delay;
        }

        public string OfflinePendingBadgeText =>
            TMDT.Services.OfflinePaymentQueue.Instance.PendingCount > 0
                ? $"🔌 {TMDT.Services.OfflinePaymentQueue.Instance.PendingCount} đơn chờ sync"
                : "";

        public bool HasOfflinePending => TMDT.Services.OfflinePaymentQueue.Instance.PendingCount > 0;

        private bool _isSyncingNow;
        public bool IsSyncingNow
        {
            get => _isSyncingNow;
            set { _isSyncingNow = value; OnPropertyChanged(); }
        }

        /// <summary>🟢 Mở window danh sách đơn offline đang chờ sync.</summary>
        public RelayCommand ShowOfflineQueueCommand => new RelayCommand(_ =>
        {
            var win = new TMDT.Views.Seller.OfflineQueueWindow();
            win.ShowDialog();
        });

        public void RefreshOfflineBadge()
        {
            OnPropertyChanged(nameof(OfflinePendingBadgeText));
            OnPropertyChanged(nameof(HasOfflinePending));
        }
    }

    public class HeldOrderSnapshot
    {
        public DateTime HeldAt { get; set; }
        public string HeldBy { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public List<PosCartItem> CartItems { get; set; } = new();
        public string? VoucherCode { get; set; }
        public decimal? ManualDiscount { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetPayment { get; set; }
        public string Note { get; set; } = "";
    }

    public class RecentCustomer
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public int LoyaltyPoints { get; set; }
    }
}

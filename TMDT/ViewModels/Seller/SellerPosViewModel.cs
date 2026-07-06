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
        public string CustomerPhone { get => _customerPhone; set => SetProperty(ref _customerPhone, value); }

        private string _customerName = "Khách vãng lai";
        public string CustomerName { get => _customerName; set => SetProperty(ref _customerName, value); }

        public int? BuyerId { get; set; }

        private int _loyaltyPoints;
        public int LoyaltyPoints { get => _loyaltyPoints; set => SetProperty(ref _loyaltyPoints, value); }

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
                        _manualDiscount = result >= 0 ? result : 0;
                        OnPropertyChanged(nameof(ManualDiscount));
                        // Force a refresh of dependent calculated properties via ViewModel
                    }
                    else if (string.IsNullOrEmpty(value))
                    {
                        _manualDiscount = 0;
                        OnPropertyChanged(nameof(ManualDiscount));
                    }
                }
            }
        }

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
                    _customerGivenAmountInput = value.ToString();
                    OnPropertyChanged(nameof(CustomerGivenAmountInput));
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
        
        private Order? _lastOrder;
        private decimal _lastGivenAmount;
        private decimal _lastChangeAmount;
        public decimal OpeningFloat { get; private set; } = 0;

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
                SearchCustomer();
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

        public SellerPosViewModel()
        {
            _shopId = SessionManager.CurrentUser?.ShopId ?? 0;
            
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

            // Show Opening Float dialog
            var shiftWindow = new TMDT.Views.Seller.PosShiftWindow();
            shiftWindow.ShowDialog();
            OpeningFloat = shiftWindow.OpeningFloat;

            // Create initial tab
            ExecuteAddTab();
            
            LoadProducts();
        }

        private void ExecuteAddTab()
        {
            var newTab = new PosOrderContext { TabTitle = $"Đơn {Tabs.Count + 1}" };
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }

        private void ExecuteCloseTab(PosOrderContext? tab)
        {
            if (tab != null && Tabs.Contains(tab))
            {
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

                var list = await query.Take(50).ToListAsync();
                Products = new ObservableCollection<Product>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sản phẩm: {ex.Message}");
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
            SelectedTab.NetPayment = SelectedTab.TotalPayment - SelectedTab.DiscountAmount;
            if (SelectedTab.NetPayment < 0) SelectedTab.NetPayment = 0;

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

        private async void SearchCustomer()
        {
            if (SelectedTab == null) return;

            if (string.IsNullOrWhiteSpace(SelectedTab.CustomerPhone) || SelectedTab.CustomerPhone.Length < 9)
            {
                SelectedTab.BuyerId = null;
                SelectedTab.CustomerName = "Khách vãng lai";
                SelectedTab.LoyaltyPoints = 0;
                SelectedTab.UseLoyaltyPoints = false;
                return;
            }

            using var context = new TmdtContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Phone == SelectedTab.CustomerPhone);
            if (user != null)
            {
                SelectedTab.BuyerId = user.UserId;
                SelectedTab.CustomerName = user.FullName ?? user.Email;
                SelectedTab.LoyaltyPoints = user.LoyaltyPoints ?? 0;
            }
            else
            {
                SelectedTab.BuyerId = null;
                SelectedTab.CustomerName = "Khách vãng lai";
                SelectedTab.LoyaltyPoints = 0;
                SelectedTab.UseLoyaltyPoints = false;
            }
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

            if (SelectedTab.CartItems.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            // Prepare items
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
            var result = await orderService.CreatePosOrderAsync(SelectedTab.BuyerId, _shopId, SelectedTab.AppliedVoucher?.VoucherId, paymentMethod, items, SelectedTab.UseLoyaltyPoints ? SelectedTab.LoyaltyPoints : 0, SelectedTab.ManualDiscount);

            if (result != null)
            {
                if (paymentMethod == "Cash")
                {
                    _lastOrder = result;
                    _lastGivenAmount = SelectedTab.CustomerGivenAmount;
                    _lastChangeAmount = SelectedTab.ChangeAmount;
                    _lastGivenAmount = SelectedTab.CustomerGivenAmount;
                    _lastChangeAmount = SelectedTab.ChangeAmount;

                    MessageBox.Show($"Thanh toán thành công!\nTiền thừa trả khách: {SelectedTab.ChangeAmount:N0} đ", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    var receiptWindow = new TMDT.Views.Seller.ReceiptWindow(result, SelectedTab.CustomerGivenAmount, SelectedTab.ChangeAmount);
                    receiptWindow.ShowDialog();

                    ExecuteCloseTab(SelectedTab);
                }
                else if (paymentMethod == "MoMo")
                {
                    // Mở cửa sổ MoMo với QR Code thật (nhantien.momo.vn)
                    var mockWindow = new TMDT.Views.Components.MoMoMockWindow(SelectedTab.NetPayment);
                    if (mockWindow.ShowDialog() == true)
                    {
                        _lastOrder = result;
                        _lastGivenAmount = SelectedTab.CustomerGivenAmount;
                        _lastChangeAmount = SelectedTab.ChangeAmount;

                        MessageBox.Show("Thanh toán MoMo thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        var receiptWindow = new TMDT.Views.Seller.ReceiptWindow(result, SelectedTab.CustomerGivenAmount, SelectedTab.ChangeAmount);
                        receiptWindow.ShowDialog();
                        ExecuteCloseTab(SelectedTab);
                    }
                }
                else if (paymentMethod == "VNPay")
                {
                    var payUrl = TMDT.Services.VNPayService.CreatePaymentUrl(result);
                    if (!string.IsNullOrEmpty(payUrl))
                    {
                        var mockWindow = new TMDT.Views.Components.VNPayMockWindow(SelectedTab.NetPayment);
                        if (mockWindow.ShowDialog() == true)
                        {
                            _lastOrder = result;
                            _lastGivenAmount = SelectedTab.CustomerGivenAmount;
                            _lastChangeAmount = SelectedTab.ChangeAmount;

                            MessageBox.Show("Thanh toán VNPAY thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            var receiptWindow = new TMDT.Views.Seller.ReceiptWindow(result, SelectedTab.CustomerGivenAmount, SelectedTab.ChangeAmount);
                            receiptWindow.ShowDialog();
                            ExecuteCloseTab(SelectedTab);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Không thể tạo link thanh toán VNPAY.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Chưa hỗ trợ quét mã QR của {paymentMethod} (Chưa tích hợp API).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show($"Lỗi thanh toán: Không thể tạo đơn hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteReprintReceipt()
        {
            if (_lastOrder != null)
            {
                var receiptWindow = new TMDT.Views.Seller.ReceiptWindow(_lastOrder, _lastGivenAmount, _lastChangeAmount);
                receiptWindow.ShowDialog();
            }
        }

        private void ExecuteRefund()
        {
            var refundWindow = new TMDT.Views.Seller.PosRefundWindow(_shopId);
            refundWindow.Owner = Application.Current.MainWindow;
            refundWindow.ShowDialog();
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class CartViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private ObservableCollection<TMDT.Services.CartItem> _items;
        private decimal _totalPrice;
        private decimal _shippingFee = 25000m;
        private decimal _totalPayment;
        private int _selectedPayment = 0;
        private Address? _selectedAddress;
        
        // --- LOYALTY POINTS ---
        private int _userLoyaltyPoints;
        private bool _usePoints;
        private int _pointsToUse;
        private decimal _pointsDiscountAmount;

        // --- VOUCHER ---
        private string _voucherCode = "";
        private Voucher? _appliedVoucher;
        private decimal _voucherDiscountAmount;

        public ObservableCollection<TMDT.Services.CartItem> Items
        {
            get => _items;
            set { SetProperty(ref _items, value); }
        }

        public decimal TotalPrice
        {
            get => _totalPrice;
            set { SetProperty(ref _totalPrice, value); }
        }

        public decimal ShippingFee
        {
            get => _shippingFee;
            set { SetProperty(ref _shippingFee, value); }
        }

        public decimal TotalPayment
        {
            get => _totalPayment;
            set { SetProperty(ref _totalPayment, value); }
        }

        public int SelectedPayment
        {
            get => _selectedPayment;
            set { SetProperty(ref _selectedPayment, value); }
        }

        public Address? SelectedAddress
        {
            get => _selectedAddress;
            set { SetProperty(ref _selectedAddress, value); }
        }

        public int UserLoyaltyPoints
        {
            get => _userLoyaltyPoints;
            set { SetProperty(ref _userLoyaltyPoints, value); }
        }

        public bool UsePoints
        {
            get => _usePoints;
            set 
            { 
                SetProperty(ref _usePoints, value);
                Recalculate();
            }
        }

        public int PointsToUse
        {
            get => _pointsToUse;
            set { SetProperty(ref _pointsToUse, value); }
        }

        public decimal PointsDiscountAmount
        {
            get => _pointsDiscountAmount;
            set { SetProperty(ref _pointsDiscountAmount, value); }
        }

        public string VoucherCode
        {
            get => _voucherCode;
            set { SetProperty(ref _voucherCode, value); }
        }

        public Voucher? AppliedVoucher
        {
            get => _appliedVoucher;
            set 
            { 
                SetProperty(ref _appliedVoucher, value); 
                OnPropertyChanged(nameof(HasVoucher));
            }
        }

        public decimal VoucherDiscountAmount
        {
            get => _voucherDiscountAmount;
            set { SetProperty(ref _voucherDiscountAmount, value); }
        }

        public bool HasVoucher => AppliedVoucher != null;

        public bool IsEmpty => Items.Count == 0;

        public ICommand RemoveItemCommand { get; }
        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }
        public ICommand PlaceOrderCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ContinueShoppingCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand ApplyVoucherCommand { get; }
        public ICommand RemoveVoucherCommand { get; }

        public CartViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;
            _items = CartService.Instance.Items;

            RemoveItemCommand = new RelayCommand(p => ExecuteRemove(p as TMDT.Services.CartItem));
            IncreaseCommand = new RelayCommand(p => ExecuteIncrease(p as TMDT.Services.CartItem));
            DecreaseCommand = new RelayCommand(p => ExecuteDecrease(p as TMDT.Services.CartItem));
            PlaceOrderCommand = new RelayCommand(_ => ExecutePlaceOrder(), _ => !IsEmpty && SessionManager.IsLoggedIn);
            BackCommand = new RelayCommand(_ => _mainVm.NavigateHome());
            ContinueShoppingCommand = new RelayCommand(_ => _mainVm.NavigateHome());
            ClearCartCommand = new RelayCommand(_ => ExecuteClear());
            ApplyVoucherCommand = new RelayCommand(_ => ExecuteApplyVoucher());
            RemoveVoucherCommand = new RelayCommand(_ => ExecuteRemoveVoucher());

            LoadAddress();
            LoadLoyaltyPoints();
            Recalculate();
        }

        private void LoadAddress()
        {
            if (!SessionManager.IsLoggedIn) return;
            try
            {
                using var context = new TmdtContext();
                var address = context.Addresses
                    .FirstOrDefault(a => a.UserId == SessionManager.CurrentUser!.UserId && a.IsDefault == true)
                    ?? context.Addresses.FirstOrDefault(a => a.UserId == SessionManager.CurrentUser!.UserId);
                SelectedAddress = address;
            }
            catch { }
        }

        private void LoadLoyaltyPoints()
        {
            if (!SessionManager.IsLoggedIn) return;
            try
            {
                using var context = new TmdtContext();
                var user = context.Users.FirstOrDefault(u => u.UserId == SessionManager.CurrentUser!.UserId);
                if (user != null)
                {
                    UserLoyaltyPoints = user.LoyaltyPoints ?? 0;
                }
            }
            catch { }
        }

        public void Recalculate()
        {
            TotalPrice = CartService.Instance.TotalPrice;
            
            // Tính toán điểm
            if (UsePoints && UserLoyaltyPoints > 0)
            {
                // Tối đa số điểm có thể dùng là tổng tiền hàng / 100
                int maxPointsNeeded = (int)((TotalPrice + ShippingFee) / 100m);
                PointsToUse = Math.Min(UserLoyaltyPoints, maxPointsNeeded);
                PointsDiscountAmount = PointsToUse * 100m;
            }
            else
            {
                PointsToUse = 0;
                PointsDiscountAmount = 0;
            }
            
            // Tính toán Voucher
            if (AppliedVoucher != null)
            {
                // Verify shop restriction again
                bool valid = false;
                decimal applicableAmount = 0;
                
                if (AppliedVoucher.ShopId == null)
                {
                    // Hệ thống voucher - apply to all
                    valid = true;
                    applicableAmount = TotalPrice;
                }
                else
                {
                    // Shop voucher - apply only to items from that shop
                    var shopItems = Items.Where(i => i.ShopId == AppliedVoucher.ShopId).ToList();
                    if (shopItems.Any())
                    {
                        valid = true;
                        applicableAmount = shopItems.Sum(i => i.LineTotal);
                    }
                }
                
                if (valid && applicableAmount >= (AppliedVoucher.MinOrderValue ?? 0))
                {
                    if (AppliedVoucher.DiscountType == "Percentage")
                    {
                        var discount = applicableAmount * (AppliedVoucher.DiscountValue ?? 0) / 100m;
                        if (AppliedVoucher.MaxDiscount.HasValue && discount > AppliedVoucher.MaxDiscount.Value)
                            discount = AppliedVoucher.MaxDiscount.Value;
                        VoucherDiscountAmount = discount;
                    }
                    else
                    {
                        VoucherDiscountAmount = AppliedVoucher.DiscountValue ?? 0;
                        if (VoucherDiscountAmount > applicableAmount) VoucherDiscountAmount = applicableAmount;
                    }
                }
                else
                {
                    VoucherDiscountAmount = 0;
                    // Tự động gỡ voucher nếu không còn hợp lệ
                    AppliedVoucher = null;
                    VoucherCode = "";
                }
            }
            else
            {
                VoucherDiscountAmount = 0;
            }
            
            TotalPayment = TotalPrice + ShippingFee - PointsDiscountAmount - VoucherDiscountAmount;
            if (TotalPayment < 0) TotalPayment = 0;
            
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void ExecuteRemove(TMDT.Services.CartItem? item)
        {
            if (item == null) return;
            var result = MessageBox.Show($"Xóa '{item.ProductName}' khỏi giỏ hàng?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            CartService.Instance.RemoveProduct(item.ProductId, item.VariantId);
            Recalculate();
        }

        private void ExecuteIncrease(TMDT.Services.CartItem? item)
        {
            if (item == null) return;
            CartService.Instance.UpdateQuantity(item.ProductId, item.VariantId, item.Quantity + 1);
            Recalculate();
        }

        private void ExecuteDecrease(TMDT.Services.CartItem? item)
        {
            if (item == null || item.Quantity <= 1) return;
            CartService.Instance.UpdateQuantity(item.ProductId, item.VariantId, item.Quantity - 1);
            Recalculate();
        }

        private void ExecuteApplyVoucher()
        {
            if (string.IsNullOrWhiteSpace(VoucherCode))
            {
                MessageBox.Show("Vui lòng nhập mã giảm giá.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new TmdtContext();
                var voucher = context.Vouchers.FirstOrDefault(v => v.VoucherCode.ToUpper() == VoucherCode.ToUpper());

                if (voucher == null)
                {
                    MessageBox.Show("Mã giảm giá không tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (voucher.IsActive != true)
                {
                    MessageBox.Show("Mã giảm giá đã bị khóa.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (voucher.StartDate.HasValue && voucher.StartDate.Value > DateTime.Now)
                {
                    MessageBox.Show($"Mã giảm giá chỉ bắt đầu áp dụng từ {voucher.StartDate.Value:dd/MM/yyyy HH:mm}.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (voucher.EndDate.HasValue && voucher.EndDate.Value < DateTime.Now)
                {
                    MessageBox.Show("Mã giảm giá đã hết hạn.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (voucher.TotalQuantity.HasValue && voucher.UsedCount.HasValue && voucher.UsedCount.Value >= voucher.TotalQuantity.Value)
                {
                    MessageBox.Show("Mã giảm giá đã hết lượt sử dụng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Temporary assign to test validity in Recalculate
                AppliedVoucher = voucher;
                Recalculate();

                if (VoucherDiscountAmount == 0)
                {
                    AppliedVoucher = null;
                    MessageBox.Show("Mã giảm giá không đủ điều kiện áp dụng cho giỏ hàng này (có thể do chưa đủ giá trị tối thiểu, hoặc không chứa sản phẩm của shop cấp mã).", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Áp dụng thành công! Được giảm {VoucherDiscountAmount:N0}đ", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteRemoveVoucher()
        {
            AppliedVoucher = null;
            VoucherCode = "";
            Recalculate();
        }

        private async void ExecutePlaceOrder()
        {
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập để đặt hàng.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Items.Count == 0)
            {
                MessageBox.Show("Giỏ hàng trống.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedAddress == null)
            {
                MessageBox.Show("Bạn chưa có địa chỉ nhận hàng.\nVui lòng vào mục 'Tài Khoản' -> 'Địa Chỉ' để thêm và chọn địa chỉ mặc định trước khi đặt hàng.", "Thiếu địa chỉ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string paymentMethod = SelectedPayment switch
            {
                0 => "COD",
                1 => "VNPay",
                2 => "MoMo",
                3 => "ZaloPay",
                _ => "COD"
            };

            var result = MessageBox.Show(
                $"Xác nhận đặt hàng?\n\nTổng tiền: {TotalPayment:N0} đ\nThanh toán: {paymentMethod}",
                "Xác nhận đặt hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                foreach (var group in Items.GroupBy(i => i.ShopId))
                {
                    var cartItems = group.Select(i => new DTOs.CartOrderItem
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        VariantId = i.VariantId,
                        VariantName = i.VariantName,
                        Quantity = i.Quantity,
                        UnitPrice = i.Price,
                        TotalPrice = i.LineTotal
                    }).ToList();

                    int? currentVoucherId = null;
                    if (AppliedVoucher != null)
                    {
                        if (AppliedVoucher.ShopId == null || AppliedVoucher.ShopId == group.Key)
                        {
                            currentVoucherId = AppliedVoucher.VoucherId;
                        }
                    }

                    var order = await OrderService.Instance.CreateOrderFromCartAsync(
                        SessionManager.CurrentUser!.UserId,
                        group.Key,
                        SelectedAddress?.AddressId,
                        currentVoucherId,
                        paymentMethod,
                        ShippingFee,
                        cartItems,
                        PointsToUse); // Pass points to service

                    if (paymentMethod == "VNPay" && order != null)
                    {
                        string vnpUrl = VNPayService.CreatePaymentUrl(order);
                        var vnPayWindow = new TMDT.Views.Components.VNPayWindow(vnpUrl);
                        bool? success = vnPayWindow.ShowDialog();

                        if (success == true)
                        {
                            await OrderService.Instance.UpdatePaymentSuccessAsync(order.OrderId, vnPayWindow.TransactionCode);
                        }
                        else
                        {
                            MessageBox.Show($"Thanh toán VNPay cho đơn hàng {order.OrderCode} thất bại hoặc đã bị hủy.\nĐơn hàng vẫn được tạo nhưng ở trạng thái Chưa thanh toán.", "Lưu ý", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else if (paymentMethod == "MoMo" && order != null)
                    {
                        var momoWindow = new TMDT.Views.Components.MoMoMockWindow(order.TotalAmount ?? 0);
                        bool? success = momoWindow.ShowDialog();

                        if (success == true)
                        {
                            await OrderService.Instance.UpdatePaymentSuccessAsync(order.OrderId, momoWindow.TransactionCode);
                        }
                        else
                        {
                            MessageBox.Show($"Thanh toán MoMo cho đơn hàng {order.OrderCode} đã bị hủy.\nĐơn hàng vẫn được tạo nhưng ở trạng thái Chưa thanh toán.", "Lưu ý", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else if (paymentMethod == "ZaloPay" && order != null)
                    {
                        var zaloWindow = new TMDT.Views.Components.ZaloPayMockWindow(order.TotalAmount ?? 0);
                        bool? success = zaloWindow.ShowDialog();

                        if (success == true)
                        {
                            await OrderService.Instance.UpdatePaymentSuccessAsync(order.OrderId, zaloWindow.TransactionCode);
                        }
                        else
                        {
                            MessageBox.Show($"Thanh toán ZaloPay cho đơn hàng {order.OrderCode} đã bị hủy.\nĐơn hàng vẫn được tạo nhưng ở trạng thái Chưa thanh toán.", "Lưu ý", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }

                CartService.Instance.Clear();
                Recalculate();

                MessageBox.Show("Đặt hàng thành công! Cảm ơn bạn đã mua sắm.",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                _mainVm.NavigateOrders();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi đặt hàng",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đặt hàng: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteClear()
        {
            var result = MessageBox.Show("Xóa tất cả sản phẩm trong giỏ hàng?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            CartService.Instance.Clear();
            Recalculate();
        }
    }
}

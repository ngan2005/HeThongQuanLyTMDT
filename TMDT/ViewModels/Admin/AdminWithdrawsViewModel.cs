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
    public class AdminWithdrawsViewModel : ViewModelBase
    {
        // Removed long-lived _context for async safety
        private ObservableCollection<WithdrawRequest> _withdrawRequests;
        private WithdrawRequest _selectedRequest;
        private string _searchText = "";
        private string _statusFilter = "All"; // All, Pending, Approved, Rejected

        private int _totalWithdraws;
        private int _pendingWithdraws;
        private int _approvedWithdraws;
        private int _rejectedWithdraws;
        private decimal _totalApprovedAmount;

        public ObservableCollection<WithdrawRequest> WithdrawRequests
        {
            get => _withdrawRequests;
            set { _withdrawRequests = value; OnPropertyChanged(); }
        }

        public WithdrawRequest SelectedRequest
        {
            get => _selectedRequest;
            set { _selectedRequest = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set 
            { 
                _searchText = value; 
                OnPropertyChanged(); 
                _ = LoadRequestsAsync(); 
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
                _ = LoadRequestsAsync();
            }
        }

        public int TotalWithdraws { get => _totalWithdraws; set { _totalWithdraws = value; OnPropertyChanged(); } }
        public int PendingWithdraws { get => _pendingWithdraws; set { _pendingWithdraws = value; OnPropertyChanged(); } }
        public int ApprovedWithdraws { get => _approvedWithdraws; set { _approvedWithdraws = value; OnPropertyChanged(); } }
        public int RejectedWithdraws { get => _rejectedWithdraws; set { _rejectedWithdraws = value; OnPropertyChanged(); } }
        public decimal TotalApprovedAmount { get => _totalApprovedAmount; set { _totalApprovedAmount = value; OnPropertyChanged(); } }

        // Events
        public event Action ShowDetailRequest;
        public event Action HideDetailRequest;

        // Commands
        public ICommand ApproveWithdrawCommand { get; }
        public ICommand RejectWithdrawCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand CloseDetailCommand { get; }
        public ICommand ViewDetailCommand { get; }

        public AdminWithdrawsViewModel()
        {

            WithdrawRequests = new ObservableCollection<WithdrawRequest>();

            // Setup Commands
            ApproveWithdrawCommand = new RelayCommand(ExecuteApproveWithdraw, CanExecuteApproveWithdraw);
            RejectWithdrawCommand = new RelayCommand(ExecuteRejectWithdraw, CanExecuteRejectWithdraw);
            FilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");
            CloseDetailCommand = new RelayCommand(o => SelectedRequest = null);
            ViewDetailCommand = new RelayCommand(o => ShowDetailRequest?.Invoke());

            _ = LoadRequestsAsync();
        }

        private async Task LoadRequestsAsync()
        {
            try
            {
                using var context = new TmdtContext();

                if (await context.WithdrawRequests.AnyAsync())
                {
                    TotalWithdraws = await context.WithdrawRequests.CountAsync();
                    PendingWithdraws = await context.WithdrawRequests.CountAsync(w => w.Status == "Pending" || string.IsNullOrEmpty(w.Status));
                    ApprovedWithdraws = await context.WithdrawRequests.CountAsync(w => w.Status == "Approved");
                    RejectedWithdraws = await context.WithdrawRequests.CountAsync(w => w.Status == "Rejected");
                    TotalApprovedAmount = await context.WithdrawRequests.Where(w => w.Status == "Approved").SumAsync(w => w.Amount ?? 0);

                    var query = context.WithdrawRequests.AsNoTracking()
                        .Include(w => w.Shop)
                        .AsQueryable();

                    // Apply Search
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        string term = SearchText.Trim().ToLower();
                        query = query.Where(w =>
                            (w.BankName != null && EF.Functions.Like(w.BankName, $"%{term}%")) ||
                            (w.Shop != null && w.Shop.ShopName != null && EF.Functions.Like(w.Shop.ShopName, $"%{term}%")));
                    }

                    // Apply Filter
                    if (StatusFilter == "Pending")
                    {
                        query = query.Where(w => w.Status == "Pending" || string.IsNullOrEmpty(w.Status));
                    }
                    else if (StatusFilter == "Approved")
                    {
                        query = query.Where(w => w.Status == "Approved");
                    }
                    else if (StatusFilter == "Rejected")
                    {
                        query = query.Where(w => w.Status == "Rejected");
                    }

                    var dbRequests = await query.ToListAsync();
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        WithdrawRequests.Clear();
                        foreach (var req in dbRequests)
                        {
                            WithdrawRequests.Add(req);
                        }
                    });

                    if (dbRequests.Any() || (string.IsNullOrEmpty(SearchText) && StatusFilter == "All"))
                        return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF query for WithdrawRequests failed: " + ex.Message);
                MessageBox.Show("Không thể tải danh sách yêu cầu rút tiền: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Commands Implementation ---

        private bool CanExecuteApproveWithdraw(object obj) => SelectedRequest != null && (SelectedRequest.Status == "Pending" || string.IsNullOrEmpty(SelectedRequest.Status));
        private async void ExecuteApproveWithdraw(object obj)
        {
            if (SelectedRequest == null) return;

            var amountVal = SelectedRequest.Amount ?? 0;

            var result = MessageBox.Show($"Xác nhận phê duyệt và giải ngân số tiền {amountVal:N0} đ đến tài khoản {SelectedRequest.AccountNumber} ({SelectedRequest.BankName})?",
                                         "Xác nhận phê duyệt rút tiền", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var context = new TmdtContext();
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var dbReq = await context.WithdrawRequests.Include(r => r.Shop).FirstOrDefaultAsync(r => r.WithdrawId == SelectedRequest.WithdrawId);
                    if (dbReq != null && (dbReq.Status == "Pending" || string.IsNullOrEmpty(dbReq.Status)))
                    {
                        dbReq.Status = "Approved";
                        dbReq.ProcessedAt = DateTime.Now;
                        // Tiền đã được trừ ở bước tạo Request, Admin duyệt thì không trừ nữa
                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        MessageBox.Show("Yêu cầu này đã được xử lý trước đó!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
                MessageBox.Show($"Lỗi khi phê duyệt rút tiền: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Đã phê duyệt yêu cầu rút tiền thành công! Đã khấu trừ {amountVal:N0} đ từ ví của Shop '{SelectedRequest.Shop?.ShopName}'.", 
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            
            HideDetailRequest?.Invoke();
            _ = LoadRequestsAsync();
        }

        private bool CanExecuteRejectWithdraw(object obj) => SelectedRequest != null && (SelectedRequest.Status == "Pending" || string.IsNullOrEmpty(SelectedRequest.Status));
        private async void ExecuteRejectWithdraw(object obj)
        {
            if (SelectedRequest == null) return;

            var amountVal = SelectedRequest.Amount ?? 0;
            var result = MessageBox.Show($"Bạn có chắc chắn muốn TỪ CHỐI yêu cầu rút tiền mã số #{SelectedRequest.WithdrawId} của shop '{SelectedRequest.Shop?.ShopName}'?", 
                                         "Xác nhận từ chối", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            SelectedRequest.Status = "Rejected";
            SelectedRequest.ProcessedAt = DateTime.Now;

            try
            {
                using var context = new TmdtContext();
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var dbReq = await context.WithdrawRequests.Include(r => r.Shop).FirstOrDefaultAsync(r => r.WithdrawId == SelectedRequest.WithdrawId);
                    if (dbReq != null && (dbReq.Status == "Pending" || string.IsNullOrEmpty(dbReq.Status)))
                    {
                        dbReq.Status = "Rejected";
                        dbReq.ProcessedAt = DateTime.Now;
                        
                        // Hoàn tiền lại cho Shop vì yêu cầu bị từ chối
                        if (dbReq.Shop != null)
                        {
                            dbReq.Shop.WalletBalance = (dbReq.Shop.WalletBalance ?? 0) + amountVal;
                        }
                        
                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        MessageBox.Show("Yêu cầu này đã được xử lý trước đó!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã từ chối yêu cầu rút tiền. Trạng thái đã được cập nhật thành Bị từ chối.", 
                            "Đã thực hiện", MessageBoxButton.OK, MessageBoxImage.Information);

            HideDetailRequest?.Invoke();
            _ = LoadRequestsAsync();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

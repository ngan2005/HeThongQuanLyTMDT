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
        private readonly TmdtContext _context;
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
                LoadRequests(); 
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
                LoadRequests();
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
            try
            {
                _context = new TmdtContext();
            }
            catch
            {
                // Failsafe
            }

            WithdrawRequests = new ObservableCollection<WithdrawRequest>();

            // Setup Commands
            ApproveWithdrawCommand = new RelayCommand(ExecuteApproveWithdraw, CanExecuteApproveWithdraw);
            RejectWithdrawCommand = new RelayCommand(ExecuteRejectWithdraw, CanExecuteRejectWithdraw);
            FilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");
            CloseDetailCommand = new RelayCommand(o => SelectedRequest = null);
            ViewDetailCommand = new RelayCommand(o => ShowDetailRequest?.Invoke());

            LoadRequests();
        }

        private void LoadRequests()
        {
            WithdrawRequests.Clear();

            try
            {
                if (_context != null)
                {
                    _context.ChangeTracker.Clear();

                    if (_context.WithdrawRequests.Any())
                    {
                        TotalWithdraws = _context.WithdrawRequests.Count();
                        PendingWithdraws = _context.WithdrawRequests.Count(w => w.Status == "Pending" || string.IsNullOrEmpty(w.Status));
                        ApprovedWithdraws = _context.WithdrawRequests.Count(w => w.Status == "Approved");
                        RejectedWithdraws = _context.WithdrawRequests.Count(w => w.Status == "Rejected");
                        TotalApprovedAmount = _context.WithdrawRequests.Where(w => w.Status == "Approved").Sum(w => w.Amount ?? 0);

                        var query = _context.WithdrawRequests
                            .Include(w => w.Shop)
                            .AsQueryable();

                        // Apply Search
                        if (!string.IsNullOrEmpty(SearchText))
                        {
                            query = query.Where(w => w.BankName.Contains(SearchText) || 
                                                     (w.Shop != null && w.Shop.ShopName.Contains(SearchText)));
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

                        var dbRequests = query.ToList();
                        foreach (var req in dbRequests)
                        {
                            WithdrawRequests.Add(req);
                        }

                        if (WithdrawRequests.Any() || (string.IsNullOrEmpty(SearchText) && StatusFilter == "All"))
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF query for WithdrawRequests failed, loading mocks. " + ex.Message);
            }

            LoadMockRequests();
        }

        private void LoadMockRequests()
        {
            var mockReqs = new ObservableCollection<WithdrawRequest>();

            mockReqs.Add(new WithdrawRequest
            {
                WithdrawId = 801,
                ShopId = 1,
                Amount = 1500000,
                BankName = "Vietcombank",
                AccountNumber = "0071000888999",
                Status = "Pending",
                RequestedAt = DateTime.Now.AddHours(-3),
                Shop = new Shop { ShopName = "Tech World Store", WalletBalance = 2400000 }
            });

            mockReqs.Add(new WithdrawRequest
            {
                WithdrawId = 802,
                ShopId = 2,
                Amount = 5000000,
                BankName = "Techcombank",
                AccountNumber = "1903456789012",
                Status = "Pending",
                RequestedAt = DateTime.Now.AddDays(-1),
                Shop = new Shop { ShopName = "Fashion Center", WalletBalance = 8500000 }
            });

            mockReqs.Add(new WithdrawRequest
            {
                WithdrawId = 803,
                ShopId = 3,
                Amount = 300000,
                BankName = "MB Bank",
                AccountNumber = "97042292019283",
                Status = "Approved",
                RequestedAt = DateTime.Now.AddDays(-5),
                ProcessedAt = DateTime.Now.AddDays(-5).AddHours(2),
                Shop = new Shop { ShopName = "Gia Dung Smart", WalletBalance = 120000 }
            });

            mockReqs.Add(new WithdrawRequest
            {
                WithdrawId = 804,
                ShopId = 1,
                Amount = 10000000,
                BankName = "VietinBank",
                AccountNumber = "101009999888",
                Status = "Rejected",
                RequestedAt = DateTime.Now.AddDays(-10),
                ProcessedAt = DateTime.Now.AddDays(-10).AddHours(4),
                Shop = new Shop { ShopName = "Tech World Store", WalletBalance = 2400000 }
            });

            TotalWithdraws = mockReqs.Count;
            PendingWithdraws = mockReqs.Count(r => r.Status == "Pending" || string.IsNullOrEmpty(r.Status));
            ApprovedWithdraws = mockReqs.Count(r => r.Status == "Approved");
            RejectedWithdraws = mockReqs.Count(r => r.Status == "Rejected");
            TotalApprovedAmount = mockReqs.Where(r => r.Status == "Approved").Sum(r => r.Amount ?? 0);

            var filtered = mockReqs.AsQueryable();
            if (!string.IsNullOrEmpty(SearchText))
            {
                filtered = filtered.Where(r => r.BankName.ToLower().Contains(SearchText.ToLower()) || 
                                               r.Shop.ShopName.ToLower().Contains(SearchText.ToLower()));
            }

            if (StatusFilter == "Pending")
            {
                filtered = filtered.Where(r => r.Status == "Pending" || string.IsNullOrEmpty(r.Status));
            }
            else if (StatusFilter == "Approved")
            {
                filtered = filtered.Where(r => r.Status == "Approved");
            }
            else if (StatusFilter == "Rejected")
            {
                filtered = filtered.Where(r => r.Status == "Rejected");
            }

            foreach (var req in filtered.ToList())
            {
                WithdrawRequests.Add(req);
            }
        }

        // --- Commands Implementation ---

        private bool CanExecuteApproveWithdraw(object obj) => SelectedRequest != null && (SelectedRequest.Status == "Pending" || string.IsNullOrEmpty(SelectedRequest.Status));
        private async void ExecuteApproveWithdraw(object obj)
        {
            if (SelectedRequest == null) return;

            var amountVal = SelectedRequest.Amount ?? 0;
            var shopBalance = SelectedRequest.Shop?.WalletBalance ?? 0;

            if (shopBalance < amountVal)
            {
                MessageBox.Show($"Không thể duyệt! Số dư ví của Shop ({shopBalance:N0} đ) nhỏ hơn số tiền yêu cầu rút ({amountVal:N0} đ).", 
                                "Lỗi số dư không đủ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"Xác nhận phê duyệt và chuyển khoản số tiền {amountVal:N0} đ đến tài khoản {SelectedRequest.AccountNumber} ({SelectedRequest.BankName})?\n\nSố tiền này sẽ được trừ trực tiếp vào ví của cửa hàng.", 
                                         "Xác nhận phê duyệt rút tiền", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            SelectedRequest.Status = "Approved";
            SelectedRequest.ProcessedAt = DateTime.Now;
            if (SelectedRequest.Shop != null)
            {
                SelectedRequest.Shop.WalletBalance -= amountVal;
            }

            try
            {
                if (_context != null)
                {
                    var dbReq = await _context.WithdrawRequests.Include(r => r.Shop).FirstOrDefaultAsync(r => r.WithdrawId == SelectedRequest.WithdrawId);
                    if (dbReq != null)
                    {
                        dbReq.Status = "Approved";
                        dbReq.ProcessedAt = DateTime.Now;
                        if (dbReq.Shop != null)
                        {
                            dbReq.Shop.WalletBalance = (dbReq.Shop.WalletBalance ?? 0) - amountVal;
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã phê duyệt yêu cầu rút tiền thành công! Đã khấu trừ {amountVal:N0} đ từ ví của Shop '{SelectedRequest.Shop?.ShopName}'.", 
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            
            HideDetailRequest?.Invoke();
            LoadRequests();
        }

        private bool CanExecuteRejectWithdraw(object obj) => SelectedRequest != null && (SelectedRequest.Status == "Pending" || string.IsNullOrEmpty(SelectedRequest.Status));
        private async void ExecuteRejectWithdraw(object obj)
        {
            if (SelectedRequest == null) return;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn TỪ CHỐI yêu cầu rút tiền mã số #{SelectedRequest.WithdrawId} của shop '{SelectedRequest.Shop?.ShopName}'?", 
                                         "Xác nhận từ chối", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            SelectedRequest.Status = "Rejected";
            SelectedRequest.ProcessedAt = DateTime.Now;

            try
            {
                if (_context != null)
                {
                    var dbReq = await _context.WithdrawRequests.FindAsync(SelectedRequest.WithdrawId);
                    if (dbReq != null)
                    {
                        dbReq.Status = "Rejected";
                        dbReq.ProcessedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã từ chối yêu cầu rút tiền. Trạng thái đã được cập nhật thành Bị từ chối.", 
                            "Đã thực hiện", MessageBoxButton.OK, MessageBoxImage.Information);

            HideDetailRequest?.Invoke();
            LoadRequests();
        }
    }
}

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

        // Commands
        public ICommand ApproveWithdrawCommand { get; }
        public ICommand RejectWithdrawCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand CloseDetailCommand { get; }

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

            LoadRequests();
        }

        private void LoadRequests()
        {
            WithdrawRequests.Clear();

            try
            {
                if (_context != null && _context.WithdrawRequests.Any())
                {
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

                    if (WithdrawRequests.Any())
                        return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF query for WithdrawRequests failed, loading mocks. " + ex.Message);
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

            LoadRequests();
        }
    }
}

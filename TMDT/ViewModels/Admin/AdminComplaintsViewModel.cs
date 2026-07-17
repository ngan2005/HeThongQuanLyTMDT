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
    public class AdminComplaintsViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        private ObservableCollection<Complaint> _complaints;
        private Complaint _selectedComplaint;
        private string _searchText = "";
        private string _statusFilter = "All"; // All, Pending, Resolved, Dismissed
        private string _resolutionText = "";

        private int _totalComplaints;
        private int _pendingComplaints;
        private int _resolvedComplaints;
        private int _dismissedComplaints;

        private readonly AiService _aiService;
        private string _aiSummary = "";
        private bool _isAiSummarizing;

        public string AiSummary { get => _aiSummary; set { _aiSummary = value; OnPropertyChanged(); } }
        public bool IsAiSummarizing { get => _isAiSummarizing; set { _isAiSummarizing = value; OnPropertyChanged(); } }

        public ObservableCollection<Complaint> Complaints
        {
            get => _complaints;
            set { _complaints = value; OnPropertyChanged(); }
        }

        public Complaint SelectedComplaint
        {
            get => _selectedComplaint;
            set 
            { 
                _selectedComplaint = value; 
                OnPropertyChanged(); 
                if (value != null)
                {
                    ResolutionText = value.Resolution ?? "";
                    ShowDetailRequest?.Invoke();
                }
                else
                {
                    HideDetailRequest?.Invoke();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set 
            { 
                _searchText = value; 
                OnPropertyChanged(); 
                _ = LoadComplaintsAsync(); 
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
                _ = LoadComplaintsAsync();
            }
        }

        public string ResolutionText
        {
            get => _resolutionText;
            set { _resolutionText = value; OnPropertyChanged(); }
        }

        public int TotalComplaints { get => _totalComplaints; set { _totalComplaints = value; OnPropertyChanged(); } }
        public int PendingComplaints { get => _pendingComplaints; set { _pendingComplaints = value; OnPropertyChanged(); } }
        public int ResolvedComplaints { get => _resolvedComplaints; set { _resolvedComplaints = value; OnPropertyChanged(); } }
        public int DismissedComplaints { get => _dismissedComplaints; set { _dismissedComplaints = value; OnPropertyChanged(); } }

        // Events
        public event Action ShowDetailRequest;
        public event Action HideDetailRequest;

        // Commands
        public ICommand ResolveComplaintCommand { get; }
        public ICommand DismissComplaintCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand CloseDetailCommand { get; }
        public ICommand ViewDetailCommand { get; }
        public ICommand SummarizeComplaintsCommand { get; }

        public AdminComplaintsViewModel()
        {
            try
            {
                _context = new TmdtContext();
            }
            catch
            {
                // Failsafe
            }

            Complaints = new ObservableCollection<Complaint>();

            // Setup Commands
            ResolveComplaintCommand = new RelayCommand(ExecuteResolveComplaint, CanExecuteResolveComplaint);
            DismissComplaintCommand = new RelayCommand(ExecuteDismissComplaint, CanExecuteDismissComplaint);
            FilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");
            CloseDetailCommand = new RelayCommand(o => SelectedComplaint = null);
            ViewDetailCommand = new RelayCommand(o => ShowDetailRequest?.Invoke());
            SummarizeComplaintsCommand = new RelayCommand(ExecuteSummarizeComplaints, o => !IsAiSummarizing);

            _aiService = new AiService();

            _ = LoadComplaintsAsync();
        }

        private async void ExecuteSummarizeComplaints(object? obj)
        {
            if (Complaints == null || Complaints.Count == 0)
            {
                AiSummary = "Không có khiếu nại nào để tóm tắt.";
                return;
            }

            IsAiSummarizing = true;
            AiSummary = "Đang đọc và phân tích các khiếu nại...";

            try
            {
                var contentList = Complaints.Where(c => !string.IsNullOrWhiteSpace(c.Content)).Select(c => c.Content!).ToList();
                AiSummary = await _aiService.SummarizeComplaintsAsync(contentList);
            }
            finally
            {
                IsAiSummarizing = false;
            }
        }

        private async System.Threading.Tasks.Task LoadComplaintsAsync()
        {
            try
            {
                if (_context == null) return;

                // Capture filter values on UI thread
                string searchText = SearchText;
                string statusFilter = StatusFilter;

                // Run DB queries on background thread to avoid freezing UI
                var result = await System.Threading.Tasks.Task.Run(() =>
                {
                    using var ctx = new TmdtContext();

                    int total = ctx.Complaints.Count();
                    int pending = ctx.Complaints.Count(c => c.Status == "Open" || string.IsNullOrEmpty(c.Status));
                    int resolved = ctx.Complaints.Count(c => c.Status == "Resolved");
                    int dismissed = ctx.Complaints.Count(c => c.Status == "Dismissed");

                    var query = ctx.Complaints
                        .Include(c => c.Buyer)
                        .Include(c => c.Order)
                        .AsQueryable();

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        string term = searchText.Trim().ToLower();
                        query = query.Where(c =>
                            (c.Content != null && EF.Functions.Like(c.Content, $"%{term}%")) ||
                            (c.Buyer != null && c.Buyer.FullName != null && EF.Functions.Like(c.Buyer.FullName, $"%{searchText}%")));
                    }

                    if (statusFilter == "Pending")
                        query = query.Where(c => c.Status == "Open" || string.IsNullOrEmpty(c.Status));
                    else if (statusFilter == "Resolved")
                        query = query.Where(c => c.Status == "Resolved");
                    else if (statusFilter == "Dismissed")
                        query = query.Where(c => c.Status == "Dismissed");

                    return new
                    {
                        Total = total, Pending = pending, Resolved = resolved, Dismissed = dismissed,
                        Items = query.ToList()
                    };
                });

                // Update UI on main thread
                TotalComplaints = result.Total;
                PendingComplaints = result.Pending;
                ResolvedComplaints = result.Resolved;
                DismissedComplaints = result.Dismissed;

                Complaints.Clear();
                foreach (var comp in result.Items)
                    Complaints.Add(comp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadComplaintsAsync error: " + ex.Message);
            }
        }

        // --- Commands Implementation ---

        private bool CanExecuteResolveComplaint(object obj) => SelectedComplaint != null && (SelectedComplaint.Status == "Open" || string.IsNullOrEmpty(SelectedComplaint.Status));
        private async void ExecuteResolveComplaint(object obj)
        {
            if (SelectedComplaint == null) return;

            if (string.IsNullOrWhiteSpace(ResolutionText))
            {
                MessageBox.Show("Vui lòng nhập phương án giải quyết (Resolution) trước khi xác nhận!", 
                                "Thông tin bắt buộc", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Xác nhận giải quyết khiếu nại #{SelectedComplaint.ComplaintId} với phương án đã nhập?", 
                                         "Xác nhận giải quyết", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            SelectedComplaint.Status = "Resolved";
            SelectedComplaint.Resolution = ResolutionText;
            SelectedComplaint.ResolvedAt = DateTime.Now;

            try
            {
                if (_context != null)
                {
                    var dbComp = await _context.Complaints.FindAsync(SelectedComplaint.ComplaintId);
                    if (dbComp != null)
                    {
                        dbComp.Status = "Resolved";
                        dbComp.Resolution = ResolutionText;
                        dbComp.ResolvedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã xử lý khiếu nại #{SelectedComplaint.ComplaintId} thành công! Phương án giải quyết đã gửi đến cả Người mua và Shop.", 
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            
            HideDetailRequest?.Invoke();
            _ = LoadComplaintsAsync();
        }

        private bool CanExecuteDismissComplaint(object obj) => SelectedComplaint != null && (SelectedComplaint.Status == "Open" || string.IsNullOrEmpty(SelectedComplaint.Status));
        private async void ExecuteDismissComplaint(object obj)
        {
            if (SelectedComplaint == null) return;

            if (string.IsNullOrWhiteSpace(ResolutionText))
            {
                MessageBox.Show("Vui lòng nhập lý do bác bỏ trước khi xác nhận!", 
                                "Thông tin bắt buộc", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc chắn muốn BÁC BỎ khiếu nại #{SelectedComplaint.ComplaintId} của khách hàng?", 
                                         "Xác nhận bác bỏ", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            SelectedComplaint.Status = "Dismissed";
            SelectedComplaint.Resolution = ResolutionText;
            SelectedComplaint.ResolvedAt = DateTime.Now;

            try
            {
                if (_context != null)
                {
                    var dbComp = await _context.Complaints.FindAsync(SelectedComplaint.ComplaintId);
                    if (dbComp != null)
                    {
                        dbComp.Status = "Dismissed";
                        dbComp.Resolution = ResolutionText;
                        dbComp.ResolvedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã bác bỏ khiếu nại #{SelectedComplaint.ComplaintId}.", 
                            "Đã thực hiện", MessageBoxButton.OK, MessageBoxImage.Information);

            HideDetailRequest?.Invoke();
            _ = LoadComplaintsAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}

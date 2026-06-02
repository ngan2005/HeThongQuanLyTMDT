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
                LoadComplaints(); 
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
                LoadComplaints();
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

            LoadComplaints();
        }

        private void LoadComplaints()
        {
            Complaints.Clear();

            try
            {
                if (_context != null)
                {
                    TotalComplaints = _context.Complaints.Count();
                    PendingComplaints = _context.Complaints.Count(c => c.Status == "Pending" || string.IsNullOrEmpty(c.Status));
                    ResolvedComplaints = _context.Complaints.Count(c => c.Status == "Resolved");
                    DismissedComplaints = _context.Complaints.Count(c => c.Status == "Dismissed");

                    var query = _context.Complaints
                        .Include(c => c.Buyer)
                        .Include(c => c.Order)
                        .AsQueryable();

                    // Apply Search
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        query = query.Where(c => c.Content.Contains(SearchText) || 
                                                 (c.Buyer != null && c.Buyer.FullName.Contains(SearchText)));
                    }

                    // Apply Filter
                    if (StatusFilter == "Pending")
                    {
                        query = query.Where(c => c.Status == "Pending" || string.IsNullOrEmpty(c.Status));
                    }
                    else if (StatusFilter == "Resolved")
                    {
                        query = query.Where(c => c.Status == "Resolved");
                    }
                    else if (StatusFilter == "Dismissed")
                    {
                        query = query.Where(c => c.Status == "Dismissed");
                    }

                    var dbComplaints = query.ToList();
                    foreach (var comp in dbComplaints)
                    {
                        Complaints.Add(comp);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF query for Complaints failed: " + ex.Message);
            }
        }

        // --- Commands Implementation ---

        private bool CanExecuteResolveComplaint(object obj) => SelectedComplaint != null && (SelectedComplaint.Status == "Pending" || string.IsNullOrEmpty(SelectedComplaint.Status));
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
            LoadComplaints();
        }

        private bool CanExecuteDismissComplaint(object obj) => SelectedComplaint != null && (SelectedComplaint.Status == "Pending" || string.IsNullOrEmpty(SelectedComplaint.Status));
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
            LoadComplaints();
        }
    }
}

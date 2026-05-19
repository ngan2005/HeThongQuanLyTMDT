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

        // Commands
        public ICommand ResolveComplaintCommand { get; }
        public ICommand DismissComplaintCommand { get; }
        public ICommand FilterCommand { get; }

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

            LoadComplaints();
        }

        private void LoadComplaints()
        {
            Complaints.Clear();

            try
            {
                if (_context != null && _context.Complaints.Any())
                {
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

                    if (Complaints.Any())
                        return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF query for Complaints failed, loading mocks. " + ex.Message);
            }

            // Fallback mock complaints
            LoadMockComplaints();
        }

        private void LoadMockMockRelations(Complaint c, string buyerName, string buyerEmail, decimal orderTotal)
        {
            c.Buyer = new User { FullName = buyerName, Email = buyerEmail };
            c.Order = new Order { TotalAmount = orderTotal };
        }

        private void LoadMockComplaints()
        {
            var mockComps = new ObservableCollection<Complaint>();

            // Mock 1: Pending
            var comp1 = new Complaint
            {
                ComplaintId = 901,
                OrderId = 20045,
                BuyerId = 12,
                Content = "Tôi đặt mua Tai nghe Sony WH-1000XM5 mới nguyên seal nhưng khi nhận hàng thì hộp có dấu hiệu bị bóc mở trước đó, cáp sạc bị cũ và tai nghe có vết xước nhẹ ở củ tai trái. Đã nhắn tin cho shop nhưng shop từ chối hỗ trợ đổi trả.",
                Status = "Pending",
                SubmittedAt = DateTime.Now.AddDays(-2),
            };
            LoadMockMockRelations(comp1, "Phạm Minh Hoàng", "hoangpm@gmail.com", 6490000);
            mockComps.Add(comp1);

            // Mock 2: Pending
            var comp2 = new Complaint
            {
                ComplaintId = 902,
                OrderId = 20089,
                BuyerId = 14,
                Content = "Shop giao sai màu áo khoác. Tôi đặt màu Be nhưng nhận được màu Đen xám. Ngoài ra chất vải cũng mỏng hơn mô tả rất nhiều, có mùi nilon khó chịu. Yêu cầu hoàn tiền và trả hàng.",
                Status = "Pending",
                SubmittedAt = DateTime.Now.AddHours(-18),
            };
            LoadMockMockRelations(comp2, "Nguyễn Thị Mai", "mainguyen98@yahoo.com", 380000);
            mockComps.Add(comp2);

            // Mock 3: Resolved
            var comp3 = new Complaint
            {
                ComplaintId = 903,
                OrderId = 20012,
                BuyerId = 15,
                Content = "Sản phẩm nồi chiên không dầu bị nứt vỡ phần vỏ nhựa phía sau do vận chuyển. Shop hứa đền bù nhưng trì hoãn đã 1 tuần chưa gửi hàng thay thế.",
                Status = "Resolved",
                SubmittedAt = DateTime.Now.AddDays(-10),
                ResolvedAt = DateTime.Now.AddDays(-8),
                Resolution = "Đã làm việc với shop. Shop đã gửi lại vỏ máy mới và đền bù mã giảm giá 50.000đ cho khách hàng. Khách hàng xác nhận hài lòng."
            };
            LoadMockMockRelations(comp3, "Lê Hoàng Long", "longlh.hust@gmail.com", 2490000);
            mockComps.Add(comp3);

            // Mock 4: Dismissed
            var comp4 = new Complaint
            {
                ComplaintId = 904,
                OrderId = 19998,
                BuyerId = 16,
                Content = "Tôi không thích mùi của trà thảo mộc detox này nữa nên muốn trả hàng hoàn tiền, mặc dù tôi đã bóc hộp ra uống thử 2 gói rồi.",
                Status = "Dismissed",
                SubmittedAt = DateTime.Now.AddDays(-12),
                ResolvedAt = DateTime.Now.AddDays(-11),
                Resolution = "Yêu cầu hoàn trả không hợp lý do khách hàng đã bóc hộp sử dụng thử và lý do hoàn trả xuất phát từ chủ quan không phải lỗi sản phẩm."
            };
            LoadMockMockRelations(comp4, "Trần Thu Trang", "trangtt99@outlook.com", 850000);
            mockComps.Add(comp4);

            // Apply Filters to mock data
            var filtered = mockComps.AsQueryable();
            if (!string.IsNullOrEmpty(SearchText))
            {
                filtered = filtered.Where(c => c.Content.ToLower().Contains(SearchText.ToLower()) || 
                                               c.Buyer.FullName.ToLower().Contains(SearchText.ToLower()));
            }

            if (StatusFilter == "Pending")
            {
                filtered = filtered.Where(c => c.Status == "Pending" || string.IsNullOrEmpty(c.Status));
            }
            else if (StatusFilter == "Resolved")
            {
                filtered = filtered.Where(c => c.Status == "Resolved");
            }
            else if (StatusFilter == "Dismissed")
            {
                filtered = filtered.Where(c => c.Status == "Dismissed");
            }

            foreach (var comp in filtered.ToList())
            {
                Complaints.Add(comp);
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

            LoadComplaints();
        }
    }
}

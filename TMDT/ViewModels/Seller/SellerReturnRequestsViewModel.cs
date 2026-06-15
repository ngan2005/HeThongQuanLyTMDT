using System;
using System.Collections.Generic;
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
    public class ReturnRequestItem : ViewModelBase
    {
        public ReturnRequest ReturnData { get; set; } = null!;

        public string BuyerName => ReturnData.Buyer?.FullName ?? "Khách hàng";
        public string RequestDate => ReturnData.RequestedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
        
        public string OrderCode => ReturnData.OrderDetail?.Order?.OrderCode ?? "N/A";
        public string ProductName => ReturnData.OrderDetail?.Product?.ProductName ?? "Sản phẩm";
        public int Quantity => ReturnData.OrderDetail?.Quantity ?? 1;
        public decimal TotalPrice => ReturnData.OrderDetail?.UnitPrice * Quantity ?? 0;

        public string Reason => string.IsNullOrWhiteSpace(ReturnData.Reason) ? "Không có lý do" : ReturnData.Reason;
        public string EvidenceImage => ReturnData.EvidenceImage ?? "";
        public bool HasEvidence => !string.IsNullOrEmpty(EvidenceImage);

        public string Status => ReturnData.Status ?? "Pending";
        public bool IsPending => Status == "Pending";
        
        public string StatusText
        {
            get
            {
                return Status switch
                {
                    "Pending" => "Chờ xử lý",
                    "Approved" => "Đã chấp nhận",
                    "Rejected" => "Đã từ chối",
                    _ => Status
                };
            }
        }
        
        public string ProcessedDate => ReturnData.ProcessedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
        public bool IsProcessed => ReturnData.ProcessedAt.HasValue;
    }

    public class SellerReturnRequestsViewModel : ViewModelBase
    {
        private readonly TmdtContext _context = null!;
        private ObservableCollection<ReturnRequestItem> _requests = new();
        private string _statusFilter = "All"; // All, Pending, Approved, Rejected

        public ObservableCollection<ReturnRequestItem> Requests
        {
            get => _requests;
            set { _requests = value; OnPropertyChanged(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); LoadRequests(); }
        }

        public ICommand SetFilterCommand { get; }
        public ICommand ApproveRequestCommand { get; }
        public ICommand RejectRequestCommand { get; }

        public SellerReturnRequestsViewModel()
        {
            try { _context = new TmdtContext(); } catch { }

            SetFilterCommand = new RelayCommand(o => StatusFilter = o?.ToString() ?? "All");
            ApproveRequestCommand = new RelayCommand(ExecuteApproveRequest);
            RejectRequestCommand = new RelayCommand(ExecuteRejectRequest);

            LoadRequests();
        }

        private int GetCurrentShopId()
        {
            return SessionManager.CurrentUser?.ShopId ?? 0;
        }

        private void LoadRequests()
        {
            Requests.Clear();
            int shopId = GetCurrentShopId();
            if (shopId <= 0 || _context == null) return;

            try
            {
                var query = _context.ReturnRequests
                    .Include(r => r.Buyer)
                    .Include(r => r.OrderDetail)
                        .ThenInclude(od => od.Order)
                    .Include(r => r.OrderDetail)
                        .ThenInclude(od => od.Product)
                    .Where(r => r.OrderDetail != null && r.OrderDetail.Order != null && r.OrderDetail.Order.ShopId == shopId)
                    .AsQueryable();

                if (StatusFilter != "All")
                {
                    query = query.Where(r => r.Status == StatusFilter);
                }

                var results = query.OrderByDescending(r => r.RequestedAt).ToList();

                foreach (var r in results)
                {
                    Requests.Add(new ReturnRequestItem { ReturnData = r });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadRequests Error: " + ex.Message);
            }
        }

        private void ExecuteApproveRequest(object? parameter)
        {
            if (parameter is ReturnRequestItem item)
            {
                var result = MessageBox.Show($"Xác nhận CHẤP NHẬN hoàn trả cho đơn hàng {item.OrderCode}?", 
                                             "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                try
                {
                    var req = _context.ReturnRequests.Find(item.ReturnData.ReturnId);
                    if (req != null)
                    {
                        req.Status = "Approved";
                        req.ProcessedAt = DateTime.Now;

                        // Cập nhật trạng thái đơn hàng (OrderDetail hoặc Order)
                        var order = _context.Orders.Find(item.ReturnData.OrderDetail?.OrderId);
                        if (order != null && order.OrderStatus != "Returned")
                        {
                            order.OrderStatus = "Returned";
                            
                            // Hoàn lại tiền cho KH (nếu thanh toán trước) hoặc cộng tồn kho
                            if (item.ReturnData.OrderDetail?.ProductId != null)
                            {
                                var product = _context.Products.Find(item.ReturnData.OrderDetail.ProductId);
                                if (product != null)
                                {
                                    product.StockQuantity = (product.StockQuantity ?? 0) + (item.ReturnData.OrderDetail.Quantity ?? 0);
                                }
                            }
                        }

                        _context.SaveChanges();
                        MessageBox.Show("Đã chấp nhận yêu cầu hoàn trả thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRequests();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteRejectRequest(object? parameter)
        {
            if (parameter is ReturnRequestItem item)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn TỪ CHỐI yêu cầu hoàn trả cho đơn hàng {item.OrderCode} không?", 
                                             "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;

                try
                {
                    var req = _context.ReturnRequests.Find(item.ReturnData.ReturnId);
                    if (req != null)
                    {
                        req.Status = "Rejected";
                        req.ProcessedAt = DateTime.Now;

                        _context.SaveChanges();
                        MessageBox.Show("Đã từ chối yêu cầu hoàn trả.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRequests();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

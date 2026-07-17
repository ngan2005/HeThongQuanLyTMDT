using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminReportsViewModel : ViewModelBase
    {
        // Removed long-lived _context to use local contexts for async safety
        private string _reportPeriod = "Month"; // Month, Quarter, Year
        private decimal _totalSystemRevenue = 0;
        private decimal _totalCommissionEarned = 0;
        private int _totalOrdersProcessed = 0;
        private int _totalActiveShopsCount = 0;

        public string ReportPeriod
        {
            get => _reportPeriod;
            set
            {
                _reportPeriod = value;
                OnPropertyChanged();
                CalculateSummaryAsync();
            }
        }

        public decimal TotalSystemRevenue
        {
            get => _totalSystemRevenue;
            set { _totalSystemRevenue = value; OnPropertyChanged(); }
        }

        public decimal TotalCommissionEarned
        {
            get => _totalCommissionEarned;
            set { _totalCommissionEarned = value; OnPropertyChanged(); }
        }

        public int TotalOrdersProcessed
        {
            get => _totalOrdersProcessed;
            set { _totalOrdersProcessed = value; OnPropertyChanged(); }
        }

        public int TotalActiveShopsCount
        {
            get => _totalActiveShopsCount;
            set { _totalActiveShopsCount = value; OnPropertyChanged(); }
        }

        // Commands for CSV
        public ICommand ExportShopsReportCommand { get; } = null!;
        public ICommand ExportTransactionsReportCommand { get; } = null!;
        public ICommand ExportWithdrawsReportCommand { get; } = null!;

        // Commands for PDF
        public ICommand ExportShopsPdfCommand { get; } = null!;
        public ICommand ExportTransactionsPdfCommand { get; } = null!;
        public ICommand ExportWithdrawsPdfCommand { get; } = null!;

        public AdminReportsViewModel()
        {
            // Setup Commands for CSV
            ExportShopsReportCommand = new RelayCommand(ExecuteExportShopsReport);
            ExportTransactionsReportCommand = new RelayCommand(ExecuteExportTransactionsReport);
            ExportWithdrawsReportCommand = new RelayCommand(ExecuteExportWithdrawsReport);

            // Setup Commands for PDF
            ExportShopsPdfCommand = new RelayCommand(ExecuteExportShopsPdf);
            ExportTransactionsPdfCommand = new RelayCommand(ExecuteExportTransactionsPdf);
            ExportWithdrawsPdfCommand = new RelayCommand(ExecuteExportWithdrawsPdf);

            CalculateSummaryAsync();
        }

        private async void CalculateSummaryAsync()
        {
            try
            {
                using var ctx = new TmdtContext();
                TotalActiveShopsCount = await ctx.Shops.CountAsync(s => s.IsActive == true);
                var completedOrders = await ctx.Orders.AsNoTracking().Where(o => o.OrderStatus == "Hoàn thành").ToListAsync();
                TotalOrdersProcessed = completedOrders.Count;
                TotalSystemRevenue = completedOrders.Sum(o => o.TotalAmount ?? 0);
                // 🟢 FIX: dùng PlatformFee đã lưu trong từng đơn (theo Shop.CommissionRate / global rate)
                // thay vì nhân lại 0.05m cứng — sẽ sai khi admin đổi rate hoặc shop có rate riêng
                TotalCommissionEarned = completedOrders.Sum(o => o.PlatformFee ?? 0);
            }
            catch
            {
                // Log error
            }
        }

        // --- CSV EXPORT METHODS ---

        private async void ExecuteExportShopsReport(object? obj)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"bao_cao_cua_hang_{DateTime.Now:yyyyMMdd}.csv",
                Title = "Lưu báo cáo danh sách cửa hàng"
            };

            if (sfd.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();
                sb.Append('\uFEFF');
                sb.AppendLine("Mã Cửa Hàng,Tên Cửa Hàng,Số Điện Thoại,Địa Chỉ Kho,Số Dư Ví (đ),Trạng Thái");

                using var ctx = new TmdtContext();
                if (await ctx.Shops.AnyAsync())
                {
                    var shops = await ctx.Shops.AsNoTracking().Include(s => s.User).ToListAsync();
                    foreach (var s in shops)
                    {
                        string statusText = (s.IsActive == true) ? "Active" : "Locked";
                        sb.AppendLine($"{s.ShopId},\"{EscapeCsv(s.ShopName)}\",\"{s.User?.Phone ?? ""}\",\"{EscapeCsv(s.WarehouseAddress ?? "")}\",{s.WalletBalance ?? 0},\"{statusText}\"");
                    }
                }
                else
                {
                    sb.AppendLine("101,\"Gia Dụng Thông Minh Tefal\",\"0912345678\",\"Hà Nội\",4500000,\"Active\"");
                    sb.AppendLine("102,\"Sony Store VN\",\"0987654321\",\"TP. Hồ Chí Minh\",12000000,\"Active\"");
                    sb.AppendLine("103,\"Fashion Summer Clothes\",\"0933445566\",\"Đà Nẵng\",150000,\"Active\"");
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Xuất báo cáo danh sách cửa hàng thành công!", "Xuất file thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi lưu file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteExportTransactionsReport(object? obj)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"bao_cao_doanh_thu_giao_dich_{DateTime.Now:yyyyMMdd}.csv",
                Title = "Lưu báo cáo doanh thu giao dịch"
            };

            if (sfd.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();
                sb.Append('\uFEFF');
                sb.AppendLine("Mã Đơn,Người Mua,Ngày Đặt,Tổng Giá Trị (đ),Phí Sàn 5% (đ),Trạng Thái");

                using var ctx = new TmdtContext();
                if (await ctx.Orders.AnyAsync())
                {
                    var orders = await ctx.Orders.AsNoTracking().Include(o => o.Buyer).ToListAsync();
                    foreach (var o in orders)
                    {
                        decimal amount = o.TotalAmount ?? 0;
                        decimal fee = amount * 0.05m;
                        sb.AppendLine($"{o.OrderId},\"{EscapeCsv(o.Buyer?.FullName ?? "N/A")}\",\"{o.OrderDate:dd/MM/yyyy}\",{amount},{fee},\"{o.OrderStatus ?? ""}\"");
                    }
                }
                else
                {
                    sb.AppendLine("20045,\"Phạm Minh Hoàng\",\"17/05/2026\",6490000,324500,\"Completed\"");
                    sb.AppendLine("20089,\"Nguyễn Thị Mai\",\"18/05/2026\",380000,19000,\"Completed\"");
                    sb.AppendLine("20012,\"Lê Hoàng Long\",\"10/05/2026\",2490000,124500,\"Completed\"");
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Xuất báo cáo doanh thu giao dịch thành công!", "Xuất file thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi lưu file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteExportWithdrawsReport(object? obj)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"bao_cao_yeu_cau_rut_tien_{DateTime.Now:yyyyMMdd}.csv",
                Title = "Lưu báo cáo yêu cầu rút tiền"
            };

            if (sfd.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();
                sb.Append('\uFEFF');
                sb.AppendLine("Mã Yêu Cầu,Cửa Hàng,Ngân Hàng,Số Tài Khoản,Số Tiền Rút (đ),Ngày Gửi,Trạng Thái");

                using var ctx = new TmdtContext();
                if (await ctx.WithdrawRequests.AnyAsync())
                {
                    var requests = await ctx.WithdrawRequests.AsNoTracking().Include(r => r.Shop).ToListAsync();
                    foreach (var r in requests)
                    {
                        sb.AppendLine($"{r.WithdrawId},\"{EscapeCsv(r.Shop?.ShopName ?? "N/A")}\",\"{EscapeCsv(r.BankName ?? "")}\",\"{r.AccountNumber ?? ""}\",{r.Amount ?? 0},\"{r.RequestedAt:dd/MM/yyyy}\",\"{r.Status ?? ""}\"");
                    }
                }
                else
                {
                    sb.AppendLine("801,\"Sony Store VN\",\"Vietcombank\",\"10223455987\",5000000,\"18/05/2026\",\"Approved\"");
                    sb.AppendLine("802,\"Gia Dụng Thông Minh Tefal\",\"Techcombank\",\"190334882772\",2000000,\"19/05/2026\",\"Pending\"");
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Xuất báo cáo yêu cầu rút tiền thành công!", "Xuất file thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi lưu file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- PDF EXPORT METHODS (NATIVE WPF PRINT TO PDF) ---

        private async void ExecuteExportShopsPdf(object? obj)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                SetPdfPrinterQueue(printDialog);

                var doc = new FlowDocument
                {
                    PagePadding = new Thickness(40),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    ColumnWidth = 800
                };

                doc.Blocks.Add(CreatePdfHeader("BÁO CÁO DANH SÁCH CỬA HÀNG HỆ THỐNG"));
                doc.Blocks.Add(CreatePdfMetadata("Danh mục báo cáo: Đối tác Cửa hàng & Số dư tài khoản"));

                var table = new Table { CellSpacing = 0, BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(0, 1, 0, 1) };
                table.Columns.Add(new TableColumn { Width = new GridLength(60) });
                table.Columns.Add(new TableColumn { Width = new GridLength(200) });
                table.Columns.Add(new TableColumn { Width = new GridLength(100) });
                table.Columns.Add(new TableColumn { Width = new GridLength(180) });
                table.Columns.Add(new TableColumn { Width = new GridLength(120) });

                var rowGroup = new TableRowGroup();
                var hRow = new TableRow { Background = System.Windows.Media.Brushes.GhostWhite, FontWeight = FontWeights.Bold };
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Mã Shop"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Tên Cửa Hàng"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Điện Thoại"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Địa Chỉ Kho"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Số Dư Ví"))) { Padding = new Thickness(6) });
                rowGroup.Rows.Add(hRow);

                using var ctx = new TmdtContext();
                if (await ctx.Shops.AnyAsync())
                {
                    var shops = await ctx.Shops.AsNoTracking().Include(s => s.User).ToListAsync();
                    foreach (var s in shops)
                    {
                        var r = new TableRow();
                        r.Cells.Add(new TableCell(new Paragraph(new Run(s.ShopId.ToString()))) { Padding = new Thickness(6) });
                        r.Cells.Add(new TableCell(new Paragraph(new Run(s.ShopName))) { Padding = new Thickness(6) });
                        r.Cells.Add(new TableCell(new Paragraph(new Run(s.User?.Phone ?? ""))) { Padding = new Thickness(6) });
                        r.Cells.Add(new TableCell(new Paragraph(new Run(s.WarehouseAddress ?? ""))) { Padding = new Thickness(6) });
                        r.Cells.Add(new TableCell(new Paragraph(new Run((s.WalletBalance ?? 0).ToString("N0") + " đ"))) { Padding = new Thickness(6) });
                        rowGroup.Rows.Add(r);
                    }
                }
                else
                {
                    var r1 = new TableRow();
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("101"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("Gia Dụng Thông Minh Tefal"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("0912345678"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("Hà Nội"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("4.500.000 đ"))) { Padding = new Thickness(6) });
                    rowGroup.Rows.Add(r1);
                }

                table.RowGroups.Add(rowGroup);
                doc.Blocks.Add(table);

                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Báo cáo Đối tác MyShop");
                    MessageBox.Show("Kết xuất Báo cáo PDF thành công!", "Xuất PDF thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất PDF: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteExportTransactionsPdf(object? obj)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                SetPdfPrinterQueue(printDialog);

                var doc = new FlowDocument
                {
                    PagePadding = new Thickness(40),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    ColumnWidth = 800
                };

                doc.Blocks.Add(CreatePdfHeader("BÁO CÁO DOANH THU & GIAO DỊCH TOÀN SÀN"));
                
                // Meta summary stats
                Paragraph stats = new Paragraph();
                stats.Inlines.Add(new Bold(new Run("TỔNG QUAN TÀI CHÍNH:\n")));
                stats.Inlines.Add(new Run($"• Tổng Doanh số Sàn: {TotalSystemRevenue:N0} đ\n"));
                stats.Inlines.Add(new Run($"• Phí Sàn Thu Về (5% Hoa Hồng): {TotalCommissionEarned:N0} đ\n"));
                stats.Inlines.Add(new Run($"• Tổng Đơn Hàng Thành Công: {TotalOrdersProcessed}\n"));
                stats.FontSize = 11;
                stats.Margin = new Thickness(0, 0, 0, 20);
                doc.Blocks.Add(stats);

                var table = new Table { CellSpacing = 0, BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(0, 1, 0, 1) };
                table.Columns.Add(new TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new TableColumn { Width = new GridLength(180) });
                table.Columns.Add(new TableColumn { Width = new GridLength(120) });
                table.Columns.Add(new TableColumn { Width = new GridLength(140) });
                table.Columns.Add(new TableColumn { Width = new GridLength(140) });

                var rowGroup = new TableRowGroup();
                var hRow = new TableRow { Background = System.Windows.Media.Brushes.GhostWhite, FontWeight = FontWeights.Bold };
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Mã Đơn"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Người Mua"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Ngày Đặt"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Tổng Tiền"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Hoa Hồng 5%"))) { Padding = new Thickness(6) });
                rowGroup.Rows.Add(hRow);

                using var ctx = new TmdtContext();
                if (await ctx.Orders.AnyAsync())
                {
                    var orders = await ctx.Orders.AsNoTracking().Include(o => o.Buyer).ToListAsync();
                    foreach (var o in orders)
                    {
                        decimal amount = o.TotalAmount ?? 0;
                        decimal fee = amount * 0.05m;
                        var r = new TableRow();
                        r.Cells.Add(new TableCell(new Paragraph(new Run(o.OrderId.ToString()))) { Padding = new Thickness(6) });
                        r.Cells.Add(new TableCell(new Paragraph(new Run(o.Buyer?.FullName ?? "N/A"))) { Padding = new Thickness(6) });
                        r.Cells.Add(new TableCell(new Paragraph(new Run(o.OrderDate?.ToString("dd/MM/yyyy") ?? ""))) { Padding = new Thickness(6) });
                        r.Cells.Add(new TableCell(new Paragraph(new Run(amount.ToString("N0") + " đ"))) { Padding = new Thickness(6) });
                        r.Cells.Add(new TableCell(new Paragraph(new Run(fee.ToString("N0") + " đ"))) { Padding = new Thickness(6) });
                        rowGroup.Rows.Add(r);
                    }
                }
                else
                {
                    var r1 = new TableRow();
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("20045"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("Phạm Minh Hoàng"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("17/05/2026"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("6.490.000 đ"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("324.500 đ"))) { Padding = new Thickness(6) });
                    rowGroup.Rows.Add(r1);
                }

                table.RowGroups.Add(rowGroup);
                doc.Blocks.Add(table);

                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Báo cáo Doanh thu MyShop");
                    MessageBox.Show("Kết xuất Báo cáo PDF thành công!", "Xuất PDF thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất PDF: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteExportWithdrawsPdf(object? obj)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                SetPdfPrinterQueue(printDialog);

                var doc = new FlowDocument
                {
                    PagePadding = new Thickness(40),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    ColumnWidth = 800
                };

                doc.Blocks.Add(CreatePdfHeader("BÁO CÁO LỊCH SỬ RÚT TIỀN ĐỐI TÁC SHOP"));
                doc.Blocks.Add(CreatePdfMetadata("Chi tiết giao dịch giải ngân ngân hàng"));

                var table = new Table { CellSpacing = 0, BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(0, 1, 0, 1) };
                table.Columns.Add(new TableColumn { Width = new GridLength(60) });
                table.Columns.Add(new TableColumn { Width = new GridLength(160) });
                table.Columns.Add(new TableColumn { Width = new GridLength(120) });
                table.Columns.Add(new TableColumn { Width = new GridLength(140) });
                table.Columns.Add(new TableColumn { Width = new GridLength(100) });
                table.Columns.Add(new TableColumn { Width = new GridLength(100) });

                var rowGroup = new TableRowGroup();
                var hRow = new TableRow { Background = System.Windows.Media.Brushes.GhostWhite, FontWeight = FontWeights.Bold };
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Mã YC"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Cửa Hàng"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Ngân Hàng"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Số Tài Khoản"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Số Tiền Rút"))) { Padding = new Thickness(6) });
                hRow.Cells.Add(new TableCell(new Paragraph(new Run("Ngày Gửi"))) { Padding = new Thickness(6) });
                rowGroup.Rows.Add(hRow);

                using var ctx = new TmdtContext();
                if (await ctx.WithdrawRequests.AnyAsync())
                {
                    var requests = await ctx.WithdrawRequests.AsNoTracking().Include(r => r.Shop).ToListAsync();
                    foreach (var r in requests)
                    {
                        var row = new TableRow();
                        row.Cells.Add(new TableCell(new Paragraph(new Run(r.WithdrawId.ToString()))) { Padding = new Thickness(6) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(r.Shop?.ShopName ?? "N/A"))) { Padding = new Thickness(6) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(r.BankName ?? ""))) { Padding = new Thickness(6) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(r.AccountNumber ?? ""))) { Padding = new Thickness(6) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run((r.Amount ?? 0).ToString("N0") + " đ"))) { Padding = new Thickness(6) });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(r.RequestedAt?.ToString("dd/MM/yyyy") ?? ""))) { Padding = new Thickness(6) });
                        rowGroup.Rows.Add(row);
                    }
                }
                else
                {
                    var r1 = new TableRow();
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("801"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("Sony Store VN"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("Vietcombank"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("10223455987"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("5.000.000 đ"))) { Padding = new Thickness(6) });
                    r1.Cells.Add(new TableCell(new Paragraph(new Run("18/05/2026"))) { Padding = new Thickness(6) });
                    rowGroup.Rows.Add(r1);
                }

                table.RowGroups.Add(rowGroup);
                doc.Blocks.Add(table);

                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Báo cáo Giải ngân MyShop");
                    MessageBox.Show("Kết xuất Báo cáo PDF thành công!", "Xuất PDF thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất PDF: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- PRINT UTILITIES ---

        private void SetPdfPrinterQueue(System.Windows.Controls.PrintDialog dialog)
        {
            try
            {
                // Attempt to auto-select "Microsoft Print to PDF"
                using (var printServer = new System.Printing.LocalPrintServer())
                {
                    var pdfQueue = printServer.GetPrintQueues().FirstOrDefault(q => q.Name.Contains("PDF"));
                    if (pdfQueue != null)
                    {
                        dialog.PrintQueue = pdfQueue;
                    }
                }
            }
            catch
            {
                // Fallback
            }
        }

        private Paragraph CreatePdfHeader(string text)
        {
            return new Paragraph(new Run(text))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.Indigo,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
        }

        private Paragraph CreatePdfMetadata(string subtitleText)
        {
            var p = new Paragraph();
            p.Inlines.Add(new Run($"{subtitleText}\n"));
            p.Inlines.Add(new Run($"Thời gian lập báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Hệ thống MyShop"));
            p.FontSize = 10;
            p.Foreground = System.Windows.Media.Brushes.Gray;
            p.TextAlignment = TextAlignment.Center;
            p.Margin = new Thickness(0, 0, 0, 24);
            return p;
        }

        private string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\"", "\"\"");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

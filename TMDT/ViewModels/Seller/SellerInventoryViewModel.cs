using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using TMDT.Models;
using TMDT.Services;
using TMDT.Services.Interfaces;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller;

/// <summary>
/// 🟢 ViewModel cho trang Quản lý kho — bảng tồn kho + nhập/xuất/kiểm kê + lịch sử + CSV + báo cáo.
/// </summary>
public class SellerInventoryViewModel : ViewModelBase
{
    private readonly IInventoryService _inventoryService;

    public ObservableCollection<InventoryRow> Inventory { get; set; } = new();
    public ObservableCollection<InventoryTransaction> RecentTransactions { get; set; } = new();
    public ObservableCollection<InventoryTransaction> FilteredTransactions { get; set; } = new();
    public ObservableCollection<TopProductMovementRow> TopMovedProducts { get; set; } = new();

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            _ = LoadInventoryAsync();
        }
    }

    private string _stockFilter = "All";
    public string StockFilter
    {
        get => _stockFilter;
        set
        {
            _stockFilter = value;
            OnPropertyChanged();
            _ = LoadInventoryAsync();
        }
    }

    private int _totalSkus;
    public int TotalSkus { get => _totalSkus; set { _totalSkus = value; OnPropertyChanged(); } }

    private int _lowStockCount;
    public int LowStockCount { get => _lowStockCount; set { _lowStockCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLowStock)); } }

    public bool HasLowStock => LowStockCount > 0;

    private int _outOfStockCount;
    public int OutOfStockCount { get => _outOfStockCount; set { _outOfStockCount = value; OnPropertyChanged(); } }

    private decimal _totalInventoryValue;
    public decimal TotalInventoryValue { get => _totalInventoryValue; set { _totalInventoryValue = value; OnPropertyChanged(); } }

    // Selected row + edit panel
    private InventoryRow? _selectedRow;
    public InventoryRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            _selectedRow = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRowSelected));
            if (value != null)
            {
                EditQuantity = value.StockQuantity;
                EditType = "Import";
            }
        }
    }
    public bool IsRowSelected => SelectedRow != null;

    private int _editQuantity;
    public int EditQuantity { get => _editQuantity; set { _editQuantity = value; OnPropertyChanged(); } }

    private string _editType = "Import";
    /// <summary>"Import" | "Export" | "Adjust".</summary>
    public string EditType { get => _editType; set { _editType = value; OnPropertyChanged(); } }

    private string _editReason = "Nhập từ NCC";
    public string EditReason { get => _editReason; set { _editReason = value; OnPropertyChanged(); } }

    // History date range
    private DateTime _historyFrom = DateTime.Today.AddDays(-30);
    public DateTime HistoryFrom { get => _historyFrom; set { _historyFrom = value; OnPropertyChanged(); _ = LoadTransactionsAsync(); } }

    private DateTime _historyTo = DateTime.Today;
    public DateTime HistoryTo { get => _historyTo; set { _historyTo = value; OnPropertyChanged(); _ = LoadTransactionsAsync(); } }

    // Active panel: Inventory | History | ImportCsv | Report
    private string _activePanel = "Inventory";
    public string ActivePanel { get => _activePanel; set { _activePanel = value; OnPropertyChanged(); } }

    // Report
    private InventoryReportRow? _report;
    public InventoryReportRow? Report { get => _report; set { _report = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasReport)); } }
    public bool HasReport => Report != null;

    private DateTime _reportFrom = DateTime.Today.AddDays(-30);
    public DateTime ReportFrom { get => _reportFrom; set { _reportFrom = value; OnPropertyChanged(); } }

    private DateTime _reportTo = DateTime.Today;
    public DateTime ReportTo { get => _reportTo; set { _reportTo = value; OnPropertyChanged(); } }

    // CSV import
    private string _csvPreviewText = "";
    public string CsvPreviewText { get => _csvPreviewText; set { _csvPreviewText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasCsvLoaded)); } }
    public bool HasCsvLoaded => !string.IsNullOrWhiteSpace(CsvPreviewText);

    private string _csvImportResult = "";
    public string CsvImportResult { get => _csvImportResult; set { _csvImportResult = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasCsvResult)); } }
    public bool HasCsvResult => !string.IsNullOrWhiteSpace(CsvImportResult);

    // Commands
    public System.Windows.Input.ICommand RefreshCommand { get; }
    public System.Windows.Input.ICommand SetFilterCommand { get; }
    public System.Windows.Input.ICommand ConfirmEditCommand { get; }
    public System.Windows.Input.ICommand OpenImportCsvPanelCommand { get; }
    public System.Windows.Input.ICommand OpenHistoryPanelCommand { get; }
    public System.Windows.Input.ICommand OpenReportPanelCommand { get; }
    public System.Windows.Input.ICommand ChooseCsvFileCommand { get; }
    public System.Windows.Input.ICommand DownloadCsvTemplateCommand { get; }
    public System.Windows.Input.ICommand ConfirmImportCsvCommand { get; }
    public System.Windows.Input.ICommand GenerateReportCommand { get; }
    public System.Windows.Input.ICommand ExportReportCsvCommand { get; }

    public SellerInventoryViewModel()
    {
        _inventoryService = InventoryService.Instance;

        RefreshCommand = new RelayCommand(_ => _ = LoadAllAsync());
        SetFilterCommand = new RelayCommand(o => StockFilter = o?.ToString() ?? "All");
        ConfirmEditCommand = new RelayCommand(_ => ExecuteConfirmEdit(), _ => SelectedRow != null);
        OpenImportCsvPanelCommand = new RelayCommand(_ => ActivePanel = "ImportCsv");
        OpenHistoryPanelCommand = new RelayCommand(_ => { ActivePanel = "History"; _ = LoadTransactionsAsync(); });
        OpenReportPanelCommand = new RelayCommand(_ => ActivePanel = "Report");
        ChooseCsvFileCommand = new RelayCommand(_ => ExecuteChooseCsvFile());
        DownloadCsvTemplateCommand = new RelayCommand(_ => ExecuteDownloadCsvTemplate());
        ConfirmImportCsvCommand = new RelayCommand(_ => _ = ExecuteImportCsvAsync(), _ => HasCsvLoaded);
        GenerateReportCommand = new RelayCommand(_ => _ = ExecuteGenerateReportAsync());
        ExportReportCsvCommand = new RelayCommand(_ => ExecuteExportReportCsv(), _ => HasReport);

        _ = LoadAllAsync();
    }

    private async Task<int> GetCurrentShopIdAsync()
    {
        int sid = SessionManager.CurrentUser?.ShopId ?? 0;
        if (sid > 0) return sid;

        try
        {
            var user = SessionManager.CurrentUser;
            if (user == null) return 0;
            using var ctx = new Models.TmdtContext();
            var shop = await ctx.Shops.FirstOrDefaultAsync(s => s.UserId == user.UserId);
            return shop?.ShopId ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task LoadAllAsync()
    {
        await LoadInventoryAsync();
        await LoadRecentTransactionsAsync();
        await LoadStatsAsync();
    }

    private async Task LoadInventoryAsync()
    {
        int shopId = await GetCurrentShopIdAsync();
        if (shopId <= 0) return;

        var rows = await _inventoryService.GetInventoryAsync(shopId, SearchText, StockFilter);
        Inventory.Clear();
        foreach (var r in rows) Inventory.Add(r);

        TotalSkus = rows.Count;
        OutOfStockCount = rows.Count(r => r.IsOutOfStock);
        TotalInventoryValue = rows.Sum(r => r.InventoryValue);
    }

    private async Task LoadStatsAsync()
    {
        int shopId = await GetCurrentShopIdAsync();
        if (shopId <= 0) return;
        LowStockCount = await _inventoryService.GetLowStockCountAsync(shopId);
    }

    private async Task LoadRecentTransactionsAsync()
    {
        int shopId = await GetCurrentShopIdAsync();
        if (shopId <= 0) return;
        var txs = await _inventoryService.GetTransactionsAsync(shopId, DateTime.Today.AddDays(-7), DateTime.Today);
        RecentTransactions.Clear();
        foreach (var t in txs.Take(20)) RecentTransactions.Add(t);
    }

    private async Task LoadTransactionsAsync()
    {
        int shopId = await GetCurrentShopIdAsync();
        if (shopId <= 0) return;
        var txs = await _inventoryService.GetTransactionsAsync(shopId, HistoryFrom, HistoryTo);
        FilteredTransactions.Clear();
        foreach (var t in txs) FilteredTransactions.Add(t);
    }

    private async void ExecuteConfirmEdit()
    {
        if (SelectedRow == null) return;
        int shopId = await GetCurrentShopIdAsync();
        if (shopId <= 0) return;

        string? performer = SessionManager.CurrentUser?.FullName;
        try
        {
            bool ok = false;
            switch (EditType)
            {
                case "Import":
                    if (EditQuantity <= 0) throw new ArgumentException("Số lượng nhập phải > 0.");
                    ok = await _inventoryService.ImportStockAsync(
                        SelectedRow.ProductId, SelectedRow.VariantId,
                        EditQuantity, EditReason, performer);
                    break;
                case "Export":
                    if (EditQuantity <= 0) throw new ArgumentException("Số lượng xuất phải > 0.");
                    ok = await _inventoryService.ExportStockAsync(
                        SelectedRow.ProductId, SelectedRow.VariantId,
                        EditQuantity, EditReason, performer);
                    break;
                case "Adjust":
                    if (EditQuantity < 0) throw new ArgumentException("Tồn kho sau kiểm kê không được âm.");
                    ok = await _inventoryService.AdjustStockAsync(
                        SelectedRow.ProductId, SelectedRow.VariantId,
                        EditQuantity, EditReason, performer);
                    break;
                default:
                    throw new ArgumentException($"Loại '{EditType}' không hợp lệ.");
            }

            if (ok)
            {
                MessageBox.Show(
                    $"Đã {GetEditTypeVietnamese(EditType)} {EditQuantity} cho sản phẩm '{SelectedRow.ProductName}'.",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                SelectedRow = null;
                EditReason = EditType == "Import" ? "Nhập từ NCC" : (EditType == "Export" ? "Hàng lỗi trả NCC" : "Sau kiểm kê");
                await LoadAllAsync();
                await LoadTransactionsAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string GetEditTypeVietnamese(string type) => type switch
    {
        "Import" => "nhập",
        "Export" => "xuất",
        "Adjust" => "kiểm kê",
        _ => type
    };

    private void ExecuteChooseCsvFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Chọn file CSV cập nhật tồn kho"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                CsvPreviewText = File.ReadAllText(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không đọc được file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExecuteDownloadCsvTemplate()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = "inventory_import_template.csv",
            Title = "Tải template CSV"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                string template =
                    "ProductCode,VariantSku,QuantityChange,Type,Reason\n" +
                    "SP001,,50,Import,Nhập từ NCC XYZ\n" +
                    "SP002,SP002-DEN,20,Import,Hàng về kho HCM\n" +
                    "SP003,,-5,Export,Hàng lỗi trả NCC\n" +
                    "SP001,,100,Adjust,Sau kiểm kê tháng\n";
                File.WriteAllText(dlg.FileName, template, new System.Text.UTF8Encoding(true));
                MessageBox.Show("Đã tải template. Mở file CSV bằng Excel/Notepad để điền dữ liệu.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không ghi được file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task ExecuteImportCsvAsync()
    {
        if (!HasCsvLoaded) return;
        int shopId = await GetCurrentShopIdAsync();
        if (shopId <= 0) return;

        string? performer = SessionManager.CurrentUser?.FullName;
        try
        {
            var (success, failed, errors) = await _inventoryService.ImportCsvAsync(shopId, CsvPreviewText, performer);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Thành công: {success} dòng");
            sb.AppendLine($"Lỗi: {failed} dòng");
            if (errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Chi tiết lỗi:");
                foreach (var e in errors.Take(20)) sb.AppendLine($"  - {e}");
                if (errors.Count > 20) sb.AppendLine($"  ... và {errors.Count - 20} lỗi khác");
            }
            CsvImportResult = sb.ToString();

            if (success > 0)
            {
                CsvPreviewText = "";
                await LoadAllAsync();
                await LoadTransactionsAsync();
            }
        }
        catch (Exception ex)
        {
            CsvImportResult = $"Lỗi: {ex.Message}";
        }
    }

    private async Task ExecuteGenerateReportAsync()
    {
        int shopId = await GetCurrentShopIdAsync();
        if (shopId <= 0) return;

        try
        {
            var report = await _inventoryService.GetReportAsync(shopId, ReportFrom, ReportTo);
            Report = report;
            TopMovedProducts.Clear();
            foreach (var p in report.TopMovedProducts) TopMovedProducts.Add(p);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExecuteExportReportCsv()
    {
        int shopId = await GetCurrentShopIdAsync();
        if (shopId <= 0 || Report == null) return;

        var dlg = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"inventory_report_{ReportFrom:yyyyMMdd}_{ReportTo:yyyyMMdd}.csv",
            Title = "Xuất báo cáo CSV"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                string csv = await Task.Run(() => _inventoryService.ExportReportCsv(shopId, ReportFrom, ReportTo));
                File.WriteAllText(dlg.FileName, csv, new System.Text.UTF8Encoding(true));
                MessageBox.Show("Đã xuất báo cáo CSV.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminAuditLogsViewModel : ViewModelBase
    {
        private ObservableCollection<AuditLogEntry> _allLogs;
        private ObservableCollection<AuditLogEntry> _filteredLogs;

        public ObservableCollection<AuditLogEntry> FilteredLogs
        {
            get => _filteredLogs;
            set { _filteredLogs = value; OnPropertyChanged(); }
        }

        private string _searchKeyword = "";
        public string SearchKeyword
        {
            get => _searchKeyword;
            set { _searchKeyword = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private string _selectedCategory = "Tất cả";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private string _selectedSeverity = "Tất cả";
        public string SelectedSeverity
        {
            get => _selectedSeverity;
            set { _selectedSeverity = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string>
        {
            "Tất cả", "Shop", "Sản phẩm", "Cài đặt hệ thống", "Thành viên", "Đơn hàng", "Khiếu nại", "Tài chính"
        };

        public ObservableCollection<string> Severities { get; } = new ObservableCollection<string>
        {
            "Tất cả", "Normal", "Warning", "Critical"
        };

        public int TotalCount => _allLogs?.Count ?? 0;
        public int WarningCount => _allLogs?.Count(l => l.Severity == "Warning") ?? 0;
        public int CriticalCount => _allLogs?.Count(l => l.Severity == "Critical") ?? 0;

        public ICommand RefreshCommand { get; }
        public ICommand ClearAllLogsCommand { get; }

        public AdminAuditLogsViewModel()
        {
            _allLogs = new ObservableCollection<AuditLogEntry>();
            _filteredLogs = new ObservableCollection<AuditLogEntry>();

            RefreshCommand = new RelayCommand(o => LoadLogs());
            ClearAllLogsCommand = new RelayCommand(o => ClearLogs());

            LoadLogs();
        }

        private void LoadLogs()
        {
            var entries = AuditLogHelper.Load();
            _allLogs = new ObservableCollection<AuditLogEntry>(entries);
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(CriticalCount));
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var result = _allLogs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var kw = SearchKeyword.ToLower();
                result = result.Where(l =>
                    l.Description.ToLower().Contains(kw) ||
                    l.Action.ToLower().Contains(kw) ||
                    l.AdminName.ToLower().Contains(kw));
            }

            if (SelectedCategory != "Tất cả")
                result = result.Where(l => l.Category == SelectedCategory);

            if (SelectedSeverity != "Tất cả")
                result = result.Where(l => l.Severity == SelectedSeverity);

            FilteredLogs = new ObservableCollection<AuditLogEntry>(result);
        }

        private void ClearLogs()
        {
            var result = System.Windows.MessageBox.Show(
                "Bạn có chắc muốn XÓA TOÀN BỘ nhật ký hoạt động?\nHành động này không thể hoàn tác.",
                "Xác nhận xóa nhật ký",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var logPath = System.IO.Path.Combine(
                    System.AppDomain.CurrentDomain.BaseDirectory, "auditlogs.json");
                if (System.IO.File.Exists(logPath))
                    System.IO.File.Delete(logPath);
                LoadLogs();
            }
        }
    }
}

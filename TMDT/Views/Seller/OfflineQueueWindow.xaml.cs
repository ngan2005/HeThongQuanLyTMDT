using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using TMDT.Services;

namespace TMDT.Views.Seller
{
    public partial class OfflineQueueWindow : Window
    {
        private readonly ObservableCollection<OfflineEntryVm> _items = new();
        private readonly DispatcherTimer _refreshTimer;

        public OfflineQueueWindow()
        {
            InitializeComponent();
            dgEntries.ItemsSource = _items;
            OfflinePaymentQueue.Instance.Changed += Refresh;

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _refreshTimer.Tick += (_, _) => UpdateCountdowns();
            _refreshTimer.Start();

            Refresh();
            Closed += (_, _) =>
            {
                _refreshTimer.Stop();
                OfflinePaymentQueue.Instance.Changed -= Refresh;
            };
        }

        private void Refresh()
        {
            var entries = OfflinePaymentQueue.Instance.Entries;
            _items.Clear();
            foreach (var e in entries.OrderByDescending(x => x.CreatedAt))
                _items.Add(new OfflineEntryVm(e));
            UpdateCounts();
        }

        private void UpdateCountdowns()
        {
            var now = DateTime.Now;
            foreach (var vm in _items)
                vm.RefreshTick(now);
        }

        private void UpdateCounts()
        {
            int pending = _items.Count(x => !x.IsSynced);
            int synced = _items.Count(x => x.IsSynced);
            int total = _items.Count;
            txtPendingCount.Text = pending.ToString();
            txtSyncedCount.Text = synced.ToString();
            txtTotalCount.Text = total.ToString();
            txtSubtitle.Text = pending == 0
                ? "Tất cả đơn đã được đồng bộ lên server."
                : $"{pending} đơn đang chờ mạng — sẽ tự retry theo backoff (1s → 2s → 4s → … → 60s).";
        }

        private async void BtnSyncOne_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is OfflineEntryVm vm)
            {
                btn.IsEnabled = false;
                try
                {
                    var ok = await OrderService.Instance.SyncOfflinePosOrderAsync(vm.OrderId);
                    if (ok)
                    {
                        OfflinePaymentQueue.Instance.MarkSynced(vm.OrderId, DateTime.Now);
                        MessageBox.Show($"Đã sync đơn {vm.OrderCode} thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        OfflinePaymentQueue.Instance.MarkSynced(vm.OrderId, DateTime.Now);
                        MessageBox.Show($"Đơn {vm.OrderCode} không còn ở trạng thái chờ sync (có thể đã xử lý từ trước).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    OfflinePaymentQueue.Instance.MarkRetryFailed(vm.OrderId);
                    MessageBox.Show($"Sync thất bại: {ex.Message}\nSẽ thử lại sau theo backoff.", "Lỗi mạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private async void BtnSyncAll_Click(object sender, RoutedEventArgs e)
        {
            var pending = _items.Where(x => !x.IsSynced).ToList();
            if (pending.Count == 0)
            {
                MessageBox.Show("Không có đơn nào đang chờ sync.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            btnSyncAll.IsEnabled = false;
            int success = 0, failed = 0;
            try
            {
                foreach (var vm in pending)
                {
                    try
                    {
                        var ok = await OrderService.Instance.SyncOfflinePosOrderAsync(vm.OrderId);
                        if (ok)
                        {
                            OfflinePaymentQueue.Instance.MarkSynced(vm.OrderId, DateTime.Now);
                            success++;
                        }
                        else
                        {
                            OfflinePaymentQueue.Instance.MarkSynced(vm.OrderId, DateTime.Now);
                            success++;
                        }
                    }
                    catch
                    {
                        OfflinePaymentQueue.Instance.MarkRetryFailed(vm.OrderId);
                        failed++;
                    }
                }
                MessageBox.Show($"Hoàn tất sync: {success} thành công, {failed} thất bại (sẽ retry sau).", "Kết quả", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                btnSyncAll.IsEnabled = true;
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }

    public class OfflineEntryVm : INotifyPropertyChanged
    {
        public OfflinePaymentEntry Source { get; }
        public int OrderId => Source.OrderId;
        public string OrderCode => Source.OrderCode;
        public string PaymentMethod => Source.PaymentMethod;
        public decimal Amount => Source.Amount;
        public string TransactionCode => Source.TransactionCode;
        public DateTime CreatedAt => Source.CreatedAt;
        public bool IsSynced => Source.SyncedAt.HasValue;

        public string StatusText => IsSynced ? "Đã sync" : "Đang chờ";

        private string _retryCountdownText = "—";
        public string RetryCountdownText
        {
            get => _retryCountdownText;
            set { _retryCountdownText = value; OnPropertyChanged(nameof(RetryCountdownText)); }
        }

        public OfflineEntryVm(OfflinePaymentEntry source) { Source = source; }

        public void RefreshTick(DateTime now)
        {
            if (IsSynced)
            {
                RetryCountdownText = "Đã sync";
                return;
            }
            var next = Source.NextRetryAt ?? Source.CreatedAt;
            var remaining = next - now;
            if (remaining <= TimeSpan.Zero)
            {
                RetryCountdownText = "đang thử…";
            }
            else
            {
                int attempts = Source.SyncAttempts ?? 0;
                RetryCountdownText = $"{Math.Ceiling(remaining.TotalSeconds)}s (lần {attempts + 1})";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
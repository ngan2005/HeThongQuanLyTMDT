using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TMDT.Services
{
    /// <summary>
    /// 🟢 Queue lưu các giao dịch QR đã xác nhận nhận tiền khi mất mạng (POS offline).
    /// - Persist vào file JSON trong AppData (không phụ thuộc DB).
    /// - Background sync timer (mỗi 30s) sẽ thử ghi lại OrderStatusHistory.SyncedAt để đánh dấu đã đồng bộ server.
    /// - Cashier thấy badge "🔌 Offline — chờ sync" trên tab đang chờ.
    /// </summary>
    public class OfflinePaymentQueue
    {
        private static OfflinePaymentQueue? _instance;
        public static OfflinePaymentQueue Instance => _instance ??= new OfflinePaymentQueue();

        private readonly string _storePath;
        private readonly object _lock = new();
        private List<OfflinePaymentEntry> _entries = new();

        /// <summary>Bắn ra khi có entry mới hoặc entry được sync xong (để UI cập nhật badge).</summary>
        public event Action? Changed;

        private OfflinePaymentQueue()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "TMDT_POS");
            Directory.CreateDirectory(folder);
            _storePath = Path.Combine(folder, "offline_payments.json");
            Load();
        }

        public IReadOnlyList<OfflinePaymentEntry> Entries
        {
            get { lock (_lock) return _entries.ToList(); }
        }

        public int PendingCount => Entries.Count(e => !e.SyncedAt.HasValue);

        public void Enqueue(OfflinePaymentEntry entry)
        {
            lock (_lock)
            {
                _entries.Add(entry);
                Save();
            }
            Changed?.Invoke();
        }

        public void MarkSynced(int orderId, DateTime syncedAt)
        {
            lock (_lock)
            {
                var entry = _entries.FirstOrDefault(e => e.OrderId == orderId);
                if (entry == null || entry.SyncedAt.HasValue) return;
                entry.SyncedAt = syncedAt;
                entry.SyncAttempts = (entry.SyncAttempts ?? 0) + 1;
                Save();
            }
            Changed?.Invoke();
        }

        public bool IsPending(int orderId) => Entries.Any(e => e.OrderId == orderId && !e.SyncedAt.HasValue);

        /// <summary>
        /// 🟢 Trả về các entry đã đến giờ retry (NextRetryAt null hoặc ≤ Now). Mỗi lần sync fail sẽ push lịch ra xa hơn.
        /// </summary>
        public List<OfflinePaymentEntry> GetReadyForRetry()
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                return _entries
                    .Where(e => !e.SyncedAt.HasValue && (!e.NextRetryAt.HasValue || e.NextRetryAt.Value <= now))
                    .ToList();
            }
        }

        /// <summary>
        /// 🟢 Đánh dấu 1 lần retry thất bại — push NextRetryAt ra xa theo backoff.
        /// </summary>
        public void MarkRetryFailed(int orderId)
        {
            lock (_lock)
            {
                var entry = _entries.FirstOrDefault(e => e.OrderId == orderId);
                if (entry == null || entry.SyncedAt.HasValue) return;
                int failedAttempts = entry.SyncAttempts ?? 0; // trước đó đã fail 0..N lần
                entry.SyncAttempts = failedAttempts + 1;
                entry.NextRetryAt = BackoffHelper.ComputeNextRetryAt(failedAttempts);
                Save();
            }
            Changed?.Invoke();
        }

        /// <summary>
        /// 🟢 Earliest NextRetryAt trong tất cả pending entries — timer dùng để re-arm interval.
        /// Null = không cần tick sớm (đã sync hết hoặc tất cả đang chờ xa).
        /// </summary>
        public DateTime? GetEarliestNextRetry()
        {
            lock (_lock)
            {
                var pending = _entries.Where(e => !e.SyncedAt.HasValue).ToList();
                if (pending.Count == 0) return null;
                return pending.Min(e => e.NextRetryAt ?? e.CreatedAt);
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_storePath)) return;
                var json = File.ReadAllText(_storePath);
                _entries = JsonSerializer.Deserialize<List<OfflinePaymentEntry>>(json) ?? new();
                // 🟢 Backfill NextRetryAt cho entry cũ (tương thích ngược với file trước khi có field này)
                foreach (var e in _entries)
                {
                    if (!e.SyncedAt.HasValue && !e.NextRetryAt.HasValue)
                        e.NextRetryAt = e.CreatedAt;
                }
            }
            catch
            {
                // File lỗi → reset để không crash POS
                _entries = new();
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_storePath, json);
            }
            catch
            {
                // Bỏ qua — lần sau sẽ thử lại
            }
        }
    }

    public class OfflinePaymentEntry
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public decimal Amount { get; set; }
        public string TransactionCode { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? SyncedAt { get; set; }
        public int? SyncAttempts { get; set; }
        /// <summary>🟢 Thời điểm sớm nhất được thử sync lại (backoff schedule). Null = sẵn sàng ngay khi load.</summary>
        public DateTime? NextRetryAt { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// 🟢 Helper tính exponential backoff cho retry queue.
    /// Schedule (seconds): 1 → 2 → 4 → 8 → 16 → 32 → 60 → 60 → ...
    /// </summary>
    public static class BackoffHelper
    {
        private const int BaseSeconds = 1;
        private const int MaxSeconds = 60;

        public static TimeSpan ComputeDelay(int failedAttempts)
        {
            // failedAttempts: 0 = lần đầu fail, 1 = lần 2 fail, ...
            int seconds = BaseSeconds * (int)Math.Pow(2, Math.Min(failedAttempts, 6));
            if (seconds > MaxSeconds) seconds = MaxSeconds;
            return TimeSpan.FromSeconds(seconds);
        }

        public static DateTime ComputeNextRetryAt(int failedAttempts)
        {
            return DateTime.Now.Add(ComputeDelay(failedAttempts));
        }
    }
}
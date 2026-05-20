using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TMDT.Utilities
{
    public class AuditLogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string AdminName { get; set; } = "Administrator";
        public string Action { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Severity { get; set; } = "Normal"; // Normal | Warning | Critical
    }

    public static class AuditLogHelper
    {
        private static readonly string LogFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "auditlogs.json");

        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static void Log(string action, string description, string category, string severity = "Normal", string adminName = "Administrator")
        {
            try
            {
                var entries = Load();
                entries.Insert(0, new AuditLogEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now,
                    AdminName = adminName,
                    Action = action,
                    Description = description,
                    Category = category,
                    Severity = severity
                });

                // Keep max 500 entries
                if (entries.Count > 500)
                    entries = entries.GetRange(0, 500);

                var json = JsonSerializer.Serialize(entries, _options);
                File.WriteAllText(LogFilePath, json);
            }
            catch { /* Silent fail — logging should never crash the app */ }
        }

        public static List<AuditLogEntry> Load()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                    return new List<AuditLogEntry>();

                var json = File.ReadAllText(LogFilePath);
                return JsonSerializer.Deserialize<List<AuditLogEntry>>(json, _options)
                       ?? new List<AuditLogEntry>();
            }
            catch
            {
                return new List<AuditLogEntry>();
            }
        }
    }
}

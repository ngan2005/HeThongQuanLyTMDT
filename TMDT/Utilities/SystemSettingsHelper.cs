using System;
using System.IO;
using System.Text.Json;

namespace TMDT.Utilities
{
    public class SystemSettings
    {
        public decimal PlatformCommissionRate { get; set; } = 5.0m;
        public decimal MinWithdrawAmount { get; set; } = 100000m;
        public bool MaintenanceMode { get; set; } = false;
        public bool RequireProductApproval { get; set; } = true;
        public string SupportEmail { get; set; } = "support@myshop.vn";
    }

    public static class SystemSettingsHelper
    {
        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemsettings.json");
        private static SystemSettings _currentSettings;

        public static SystemSettings Current
        {
            get
            {
                if (_currentSettings == null)
                {
                    LoadSettings();
                }
                return _currentSettings;
            }
        }

        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    _currentSettings = JsonSerializer.Deserialize<SystemSettings>(json) ?? new SystemSettings();
                }
                else
                {
                    _currentSettings = new SystemSettings();
                    SaveSettings();
                }
            }
            catch
            {
                _currentSettings = new SystemSettings();
            }
        }

        public static void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to save system settings: " + ex.Message);
            }
        }
    }
}

using System.Configuration;
using System.Data;
using System.Windows;

namespace TMDT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Cấu hình QuestPDF
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // Đăng ký xử lý lỗi unhandled
            AppDomain.CurrentDomain.UnhandledException += (s, args) => LogException(args.ExceptionObject as Exception, "AppDomain");
            DispatcherUnhandledException += (s, args) =>
            {
                LogException(args.Exception, "Dispatcher");
                args.Handled = true;
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogException(args.Exception, "TaskScheduler");
                args.SetObserved();
            };

            // Tự động seed dữ liệu mẫu (Roles, Admin, Seller)
            try
            {
                TMDT.Helpers.DbSeeder.Seed();
            }
            catch (Exception ex)
            {
                // Có thể log lỗi nếu cần thiết
                System.Diagnostics.Debug.WriteLine($"DbSeeder Error: {ex.Message}");
            }
        }

        private bool _isLogging = false;

        private void LogException(Exception? ex, string source)
        {
            if (ex == null) return;
            if (_isLogging) return; // Prevent infinite recursion
            _isLogging = true;
            try
            {
                string logMessage = $"[{DateTime.Now}] Source: {source}\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStackTrace:\n{ex.StackTrace}\n\n";
                try
                {
                    System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), logMessage);
                }
                catch {}
                MessageBox.Show($"Đã xảy ra lỗi hệ thống ({source}): {ex.Message}\nChi tiết xem tại crash.log", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLogging = false;
            }
        }
    }
}

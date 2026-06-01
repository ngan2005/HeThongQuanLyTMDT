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
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;

namespace TMDT.Utilities
{
    public class GoogleUser
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
    }

    public static class GoogleAuthService
    {
        // Replace these with your real Google OAuth values from Google Cloud Console before running login.
        private const string ClientId = "YOUR_GOOGLE_CLIENT_ID";
        private const string ClientSecret = "YOUR_GOOGLE_CLIENT_SECRET";

        public static async Task<GoogleUser> LoginAsync()
        {
            try
            {
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = ClientId,
                        ClientSecret = ClientSecret
                    },
                    new[] { Oauth2Service.Scope.UserinfoEmail, Oauth2Service.Scope.UserinfoProfile },
                    "user",
                    CancellationToken.None);

                var oauth2Service = new Oauth2Service(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Volox"
                });

                var userInfo = await oauth2Service.Userinfo.Get().ExecuteAsync();

                return new GoogleUser
                {
                    Email = userInfo.Email,
                    FullName = userInfo.Name,
                    AvatarUrl = userInfo.Picture
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Google Login Error: " + ex.Message);
                System.Windows.MessageBox.Show($"Google Login Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Google Auth Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
        }

        public static void Logout()
        {
            try
            {
                var folderPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Google.Apis.Auth", "user");
                if (System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.Delete(folderPath, true);
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi nếu không thể xóa cache
            }
        }
    }
}

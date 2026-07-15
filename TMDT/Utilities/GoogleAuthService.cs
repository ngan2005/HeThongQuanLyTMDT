using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;

namespace TMDT.Utilities;

public class GoogleUser
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public static class GoogleAuthConfig
{
    private const string AppSettingsFileName = "appsettings.json";
    private static readonly string AppSettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, AppSettingsFileName);

    private static string? _clientId;
    private static string? _clientSecret;

    public static string ClientId
    {
        get
        {
            if (_clientId == null) LoadConfig();
            return _clientId!;
        }
    }

    public static string ClientSecret
    {
        get
        {
            if (_clientSecret == null) LoadConfig();
            return _clientSecret!;
        }
    }

    private static void LoadConfig()
    {
        try
        {
            if (!File.Exists(AppSettingsPath))
            {
                throw new FileNotFoundException(
                    $"Không tìm thấy file {AppSettingsFileName}. Vui lòng tạo file cấu hình với section \"GoogleAuth\".");
            }

            var json = File.ReadAllText(AppSettingsPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("GoogleAuth", out var googleAuth))
            {
                _clientId = googleAuth.GetProperty("ClientId").GetString();
                _clientSecret = googleAuth.GetProperty("ClientSecret").GetString();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Lỗi đọc cấu hình Google Auth từ {AppSettingsFileName}: {ex.Message}", ex);
        }
    }

    public static void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId) ||
            ClientId.Contains("YOUR_GOOGLE") ||
            ClientId.Contains("REPLACE"))
        {
            throw new InvalidOperationException(
                "Google ClientId chưa được cấu hình. Vui lòng cập nhật GoogleAuth:ClientId trong appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(ClientSecret) ||
            ClientSecret.Contains("YOUR_GOOGLE") ||
            ClientSecret.Contains("REPLACE"))
        {
            throw new InvalidOperationException(
                "Google ClientSecret chưa được cấu hình. Vui lòng cập nhật GoogleAuth:ClientSecret trong appsettings.json.");
        }
    }
}

public static class GoogleAuthService
{
    public static async Task<GoogleUser?> LoginAsync()
    {
        try
        {
            // Luôn xóa cache để cho phép chọn tài khoản khác nhau mỗi lần đăng nhập
            Logout();

            GoogleAuthConfig.Validate();

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = GoogleAuthConfig.ClientId,
                    ClientSecret = GoogleAuthConfig.ClientSecret
                },
                new[] { 
                    PeopleServiceService.Scope.UserinfoEmail,
                    PeopleServiceService.Scope.UserinfoProfile
                },
                "user",
                CancellationToken.None);

            var peopleService = new PeopleServiceService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Volox"
            });

            var profileRequest = peopleService.People.Get("people/me");
            profileRequest.PersonFields = "names,emailAddresses,photos";

            var person = await profileRequest.ExecuteAsync();

            var name = person.Names != null && person.Names.Count > 0
                ? person.Names[0].DisplayName ?? person.Names[0].GivenName ?? string.Empty
                : string.Empty;

            var email = person.EmailAddresses != null && person.EmailAddresses.Count > 0
                ? person.EmailAddresses[0].Value ?? string.Empty
                : string.Empty;

            var avatar = person.Photos != null && person.Photos.Count > 0
                ? person.Photos[0].Url ?? string.Empty
                : string.Empty;

            return new GoogleUser
            {
                Email = email,
                FullName = name,
                AvatarUrl = avatar
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Google Login Error: " + ex.Message);
            System.Windows.MessageBox.Show(
                $"Google Login Error: {ex.Message}",
                "Google Auth Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return null;
        }
    }

    public static void Logout()
    {
        try
        {
            var folderPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "Google.Apis.Auth", "Stored");
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

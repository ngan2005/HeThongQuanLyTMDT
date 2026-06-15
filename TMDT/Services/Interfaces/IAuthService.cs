using System.Threading.Tasks;
using TMDT.DTOs;

namespace TMDT.Services.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto> LoginAsync(string email, string password);

        Task<UserDto> LoginWithGoogleAsync(string email, string fullName, string avatarUrl);

        /// <summary>Returns (true, null) on success, or (false, errorReason) on failure.</summary>
        Task<(bool Success, string? ErrorMessage)> RegisterAsync(RegisterRequest request);

        Task<bool> LogoutAsync();
    }
}

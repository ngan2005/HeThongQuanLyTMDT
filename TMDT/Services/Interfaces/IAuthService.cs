using System.Threading.Tasks;
using TMDT.DTOs;

namespace TMDT.Services.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto> LoginAsync(string email, string password);
        Task<bool> RegisterAsync(RegisterRequest request);
        Task<bool> LogoutAsync();
    }
}

using System.Threading.Tasks;

namespace TMDT.Services.Interfaces
{
    public interface IEmailService
    {
        Task<(bool Success, string Message)> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    }
}

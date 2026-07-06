using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TMDT.Services.Interfaces;
using TMDT.Utilities;

namespace TMDT.Services
{
    public class EmailService : IEmailService
    {
        public async Task<(bool Success, string Message)> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpConfig = ConfigurationHelper.Configuration.GetSection("SmtpConfig");
                var host = smtpConfig["Host"];
                var portStr = smtpConfig["Port"];
                var useSslStr = smtpConfig["UseSsl"];
                var email = smtpConfig["Email"];
                var password = smtpConfig["Password"];
                var displayName = smtpConfig["DisplayName"];

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return (false, "Cấu hình SMTP chưa được thiết lập. Vui lòng kiểm tra appsettings.json.");
                }
                
                if (email == "YOUR_GMAIL@gmail.com" || password == "YOUR_APP_PASSWORD")
                {
                    return (false, "Bạn chưa điền thông tin Gmail thật vào cấu hình SMTP trong appsettings.json!");
                }

                int.TryParse(portStr, out int port);
                bool.TryParse(useSslStr, out bool useSsl);

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = useSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(email, password);

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(email, displayName);
                        mailMessage.To.Add(toEmail);
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = isHtml;

                        await client.SendMailAsync(mailMessage);
                        return (true, "Gửi email thành công.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi gửi email: {ex.Message}");
            }
        }
    }
}

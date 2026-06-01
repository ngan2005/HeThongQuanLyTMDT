using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System;
using System.Threading.Tasks;
using TMDT.Services.Interfaces;
using TMDT.Utilities;

namespace TMDT.Services
{
    public class CloudinaryService : IImageUploadService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService()
        {
            var config = ConfigurationHelper.Configuration.GetSection("CloudinarySettings");
            var account = new Account(
                config["CloudName"],
                config["ApiKey"],
                config["ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(string filePath)
        {
            try
            {
                var config = ConfigurationHelper.Configuration.GetSection("CloudinarySettings");

                var cloudName = config["CloudName"];
                var apiKey = config["ApiKey"];
                var apiSecret = config["ApiSecret"];

                // Validate credentials are present
                if (string.IsNullOrWhiteSpace(cloudName) ||
                    string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(apiSecret))
                {
                    throw new InvalidOperationException(
                        "Cloudinary credentials chưa được cấu hình trong appsettings.json. " +
                        "Vui lòng thêm CloudName, ApiKey, ApiSecret vào mục CloudinarySettings.");
                }

                var account = new Account(cloudName, apiKey, apiSecret);
                var cloudinary = new Cloudinary(account);

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(filePath),
                    Folder = "MyShop_WPF",
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    throw new InvalidOperationException($"Lỗi Cloudinary: {uploadResult.Error.Message}");
                }

                return uploadResult.SecureUrl.ToString();
            }
            catch (InvalidOperationException)
            {
                // Re-throw validation errors as-is (already descriptive)
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cloudinary Upload Error: {ex.Message}");
                return null;
            }
        }
    }
}

using System.Threading.Tasks;

namespace TMDT.Services.Interfaces
{
    public interface IImageUploadService
    {
        Task<string> UploadImageAsync(string filePath);
    }
}

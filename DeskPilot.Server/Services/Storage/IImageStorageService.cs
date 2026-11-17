using System.IO;
using System.Threading.Tasks;

namespace DeskPilot.Server.Services.Storage
{
    public interface IImageStorageService
    {
        /// <summary>
        /// Tải hình ảnh lên ImageKit vào thư mục chỉ định (mặc định /chat) và trả về URL CDN của ảnh.
        /// </summary>
        Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder = "/chat");
    }
}

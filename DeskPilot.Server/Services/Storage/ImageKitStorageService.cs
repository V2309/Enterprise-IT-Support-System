using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeskPilot.Server.Services.Storage
{
    public class ImageKitStorageService : IImageStorageService
    {
        private const string UploadApiUrl = "https://upload.imagekit.io/api/v1/files/upload";
        private readonly HttpClient _httpClient;
        private readonly ImageKitOptions _options;
        private readonly ILogger<ImageKitStorageService> _logger;

        public ImageKitStorageService(
            HttpClient httpClient,
            IOptions<ImageKitOptions> options,
            ILogger<ImageKitStorageService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder = "/chat")
        {
            if (string.IsNullOrWhiteSpace(_options.PrivateKey))
            {
                throw new InvalidOperationException("Chưa cấu hình ImageKit:PrivateKey trong appsettings hoặc biến môi trường.");
            }

            // Chuẩn hóa tên file nếu cần
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"chat_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
            }

            // Chuẩn hóa folder (ImageKit chấp nhận /chat hoặc chat)
            if (!folder.StartsWith("/"))
            {
                folder = "/" + folder;
            }

            using var form = new MultipartFormDataContent();

            // Stream file
            var streamContent = new StreamContent(fileStream);
            form.Add(streamContent, "file", fileName);

            // Tên file lưu trên ImageKit
            form.Add(new StringContent(fileName), "fileName");

            // Thư mục lưu trữ trên ImageKit (ví dụ: /chat)
            form.Add(new StringContent(folder), "folder");

            // Tự động thêm đuôi ngẫu nhiên chống trùng lặp tên file
            form.Add(new StringContent("true"), "useUniqueFileName");

            // Basic Auth: base64(private_key + ":")
            var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.PrivateKey}:"));
            using var request = new HttpRequestMessage(HttpMethod.Post, UploadApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            request.Content = form;

            _logger.LogInformation("Đang tải ảnh {FileName} lên ImageKit thư mục {Folder}...", fileName, folder);

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Lỗi khi tải ảnh lên ImageKit: {StatusCode} - {Body}", response.StatusCode, responseJson);
                throw new InvalidOperationException($"Lỗi ImageKit: {response.StatusCode} - {responseJson}");
            }

            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("url", out var urlElement))
            {
                var url = urlElement.GetString();
                if (!string.IsNullOrEmpty(url))
                {
                    _logger.LogInformation("Tải ảnh lên ImageKit thành công: {Url}", url);
                    return url;
                }
            }

            throw new InvalidOperationException("ImageKit không trả về đường dẫn URL hợp lệ: " + responseJson);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DeskPilot.Server.Services;
using DeskPilot.Server.Services.Storage;
using DeskPilot.Shared.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeskPilot.Server.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly MachineStateCache _machineCache;
        private readonly IImageStorageService _imageStorage;

        private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

        public ChatController(
            IChatService chatService,
            MachineStateCache machineCache,
            IImageStorageService imageStorage)
        {
            _chatService = chatService;
            _machineCache = machineCache;
            _imageStorage = imageStorage;
        }

        /// <summary>Tải hình ảnh đính kèm chat lên ImageKit (thư mục /chat).</summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Không tìm thấy file hình ảnh tải lên." });
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { message = "Kích thước hình ảnh vượt quá giới hạn 10MB." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Định dạng file không hỗ trợ. Chỉ chấp nhận png, jpg, jpeg, webp, gif, bmp." });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var safeFileName = $"chat_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";
                var url = await _imageStorage.UploadImageAsync(stream, safeFileName, "/chat");

                return Ok(new { url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lưu ảnh lên ImageKit: " + ex.Message });
            }
        }

        /// <summary>Danh sách cuộc hội thoại (nhóm theo máy, bổ sung trạng thái online).</summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var conversations = await _chatService.GetConversationsAsync();

            // Bổ sung trạng thái Online/Offline từ cache RAM
            foreach (var c in conversations)
            {
                c.IsOnline = _machineCache.IsOnline(c.MachineName);
            }

            return Ok(conversations);
        }

        /// <summary>Lịch sử chat của một máy trạm (cũ → mới).</summary>
        [HttpGet("{machineName}/history")]
        public async Task<IActionResult> GetHistory(string machineName, [FromQuery] int count = 50)
        {
            var messages = await _chatService.GetHistoryAsync(machineName, count);
            return Ok(messages);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeskPilot.Server.Data;
using DeskPilot.Server.Models;
using DeskPilot.Shared.DTO;
using Microsoft.EntityFrameworkCore;

namespace DeskPilot.Server.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;

        public ChatService(AppDbContext db) => _db = db;

        public async Task<ChatMessageDto> SaveMessageAsync(string machineName, string senderName, string message, bool isFromAdmin, string? imageUrl = null)
        {
            var entity = new ChatMessage
            {
                MachineName = machineName,
                SenderName = senderName,
                Message = message,
                ImageUrl = imageUrl,
                IsFromAdmin = isFromAdmin,
                SentAt = DateTime.UtcNow
            };

            _db.ChatMessages.Add(entity);
            await _db.SaveChangesAsync();

            return ToDto(entity);
        }

        public async Task<List<ChatMessageDto>> GetHistoryAsync(string machineName, int count = 50)
        {
            var messages = await _db.ChatMessages
                .Where(c => c.MachineName.ToLower() == machineName.ToLower())
                .OrderByDescending(c => c.SentAt)
                .Take(count)
                .ToListAsync();

            // Đảo ngược để trả về theo thứ tự cũ → mới (UI hiển thị từ trên xuống)
            messages.Reverse();
            return messages.Select(ToDto).ToList();
        }

        public async Task<List<ChatConversationDto>> GetConversationsAsync()
        {
            // Lấy 500 tin nhắn gần nhất rồi nhóm theo máy trong bộ nhớ
            var recentMessages = await _db.ChatMessages
                .OrderByDescending(c => c.SentAt)
                .Take(500)
                .ToListAsync();

            return recentMessages
                .GroupBy(c => c.MachineName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var lastMsg = g.First(); // đã sắp xếp desc
                    var userName = g.FirstOrDefault(c => !c.IsFromAdmin)?.SenderName ?? "";
                    var displayMessage = !string.IsNullOrWhiteSpace(lastMsg.Message)
                        ? lastMsg.Message
                        : (!string.IsNullOrEmpty(lastMsg.ImageUrl) ? "[Hình ảnh]" : string.Empty);

                    return new ChatConversationDto
                    {
                        MachineName = g.Key,
                        UserName = userName,
                        LastMessage = displayMessage.Length > 80
                            ? displayMessage[..80] + "…"
                            : displayMessage,
                        LastMessageAt = lastMsg.SentAt
                    };
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();
        }

        private static ChatMessageDto ToDto(ChatMessage c) => new()
        {
            Id = c.Id,
            MachineName = c.MachineName,
            SenderName = c.SenderName,
            Message = c.Message ?? string.Empty,
            ImageUrl = c.ImageUrl,
            IsFromAdmin = c.IsFromAdmin,
            SentAt = c.SentAt
        };
    }
}

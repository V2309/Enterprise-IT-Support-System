using System.Collections.Generic;
using System.Threading.Tasks;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Server.Services
{
    public interface IChatService
    {
        /// <summary>Lưu tin nhắn chat vào PostgreSQL và trả về DTO.</summary>
        Task<ChatMessageDto> SaveMessageAsync(string machineName, string senderName, string message, bool isFromAdmin, string? imageUrl = null);

        /// <summary>Lấy lịch sử chat của một máy (mới nhất trước, client tự đảo ngược).</summary>
        Task<List<ChatMessageDto>> GetHistoryAsync(string machineName, int count = 50);

        /// <summary>Danh sách các cuộc hội thoại (phân nhóm theo máy, sắp xếp theo tin nhắn mới nhất).</summary>
        Task<List<ChatConversationDto>> GetConversationsAsync();
    }
}

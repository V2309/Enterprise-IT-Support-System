using DeskPilot.Shared;

namespace DeskPilot.Server.Services
{
    public interface ITicketService
    {
        // Tạo ticket mới từ Client
        Task<TicketDto> CreateAsync(string machineName, string userName, string description, CancellationToken ct = default);

        // Lấy các ticket chưa Resolved (mới nhất lên đầu)
        Task<List<TicketDto>> GetActiveAsync(CancellationToken ct = default);

        // Đánh dấu hoàn tất ticket + ghi ActionLog. Trả về null nếu không tìm thấy hoặc đã Resolved.
        Task<ActionLogDto?> ResolveAsync(int ticketId, string resolvedBy = "IT Admin", CancellationToken ct = default);
    }
}

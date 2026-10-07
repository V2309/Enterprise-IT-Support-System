using DeskPilot.Shared;

namespace DeskPilot.Server.Services
{
    public interface IActionLogService
    {
        // Ghi nhật ký một hành động/lệnh IT
        Task<ActionLogDto> CreateAsync(string targetMachine, string commandName, string executedBy = "IT Admin", CancellationToken ct = default);

        // Lấy N nhật ký gần nhất
        Task<List<ActionLogDto>> GetRecentAsync(int count = 20, CancellationToken ct = default);
    }
}

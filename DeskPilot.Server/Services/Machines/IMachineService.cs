using DeskPilot.Shared.DTO;

namespace DeskPilot.Server.Services
{
    public interface IMachineService
    {
        // Lấy toàn bộ danh sách máy trạm từ DB
        Task<List<MachineStatusDto>> GetAllAsync(CancellationToken ct = default);

        // Lưu theo lô các snapshot trạng thái máy (insert nếu chưa có, update nếu đã có)
        Task SaveSnapshotsAsync(IReadOnlyCollection<MachineStatusDto> snapshots, CancellationToken ct = default);

        // Đánh dấu toàn bộ máy offline (dùng khi Server khởi động lại)
        Task MarkAllOfflineAsync(CancellationToken ct = default);
    }
}

using DeskPilot.Shared.DTO;

namespace DeskPilot.Server.Services
{
    public interface IDeviceService
    {
        Task<List<DeviceDto>> GetAllAsync(
            string? department = null,
            string? status = null,
            string? deviceType = null,
            string? search = null,
            CancellationToken ct = default);

        Task<DeviceDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<DeviceDto?> GetByCodeAsync(string deviceCode, CancellationToken ct = default);

        Task<DeviceDto> CreateAsync(DeviceUpsertRequest request, CancellationToken ct = default);

        Task<DeviceDto?> UpdateAsync(Guid id, DeviceUpsertRequest request, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

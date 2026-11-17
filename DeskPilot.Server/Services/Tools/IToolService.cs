using DeskPilot.Shared.DTO;

namespace DeskPilot.Server.Services
{
    public interface IToolService
    {
        Task<List<ToolDto>> GetAllAsync(
            string? category = null,
            bool? isActive = null,
            string? search = null,
            CancellationToken ct = default);

        Task<ToolDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<ToolDto?> GetByCodeAsync(string code, CancellationToken ct = default);

        Task<ToolDto> CreateAsync(ToolUpsertRequest request, CancellationToken ct = default);

        Task<ToolDto?> UpdateAsync(Guid id, ToolUpsertRequest request, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

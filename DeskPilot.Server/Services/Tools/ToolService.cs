using DeskPilot.Server.Data;
using DeskPilot.Server.Models;
using DeskPilot.Server.Models.Enums;
using DeskPilot.Shared.DTO;
using Microsoft.EntityFrameworkCore;

namespace DeskPilot.Server.Services
{
    public class ToolService : IToolService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ToolService> _logger;

        public ToolService(AppDbContext db, ILogger<ToolService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<ToolDto>> GetAllAsync(
            string? category = null,
            bool? isActive = null,
            string? search = null,
            CancellationToken ct = default)
        {
            var query = _db.Tools.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ToolCategory>(category, true, out var parsedCategory))
            {
                query = query.Where(t => t.Category == parsedCategory);
            }

            if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(t =>
                    t.Code.ToLower().Contains(s) ||
                    t.Name.ToLower().Contains(s) ||
                    t.Description.ToLower().Contains(s) ||
                    t.Author.ToLower().Contains(s));
            }

            var tools = await query
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync(ct);

            return tools.Select(ToDto).ToList();
        }

        public async Task<ToolDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var tool = await _db.Tools.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
            return tool == null ? null : ToDto(tool);
        }

        public async Task<ToolDto?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            var tool = await _db.Tools.AsNoTracking().FirstOrDefaultAsync(t => t.Code.ToLower() == code.Trim().ToLower(), ct);
            return tool == null ? null : ToDto(tool);
        }

        public async Task<ToolDto> CreateAsync(ToolUpsertRequest request, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var tool = new Tool
            {
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Category = ParseEnum(request.Category, ToolCategory.System),
                Description = request.Description?.Trim() ?? string.Empty,
                CommandOrScript = request.CommandOrScript.Trim(),
                DefaultParameters = request.DefaultParameters?.Trim(),
                Version = string.IsNullOrWhiteSpace(request.Version) ? "1.0.0" : request.Version.Trim(),
                Author = string.IsNullOrWhiteSpace(request.Author) ? "IT Admin" : request.Author.Trim(),
                IsActive = request.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Tools.Add(tool);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[TOOL] Đã thêm công cụ mới: {Code} - {Name}", tool.Code, tool.Name);

            return ToDto(tool);
        }

        public async Task<ToolDto?> UpdateAsync(Guid id, ToolUpsertRequest request, CancellationToken ct = default)
        {
            var tool = await _db.Tools.FindAsync(new object[] { id }, ct);
            if (tool == null) return null;

            tool.Code = request.Code.Trim().ToUpperInvariant();
            tool.Name = request.Name.Trim();
            tool.Category = ParseEnum(request.Category, ToolCategory.System);
            tool.Description = request.Description?.Trim() ?? string.Empty;
            tool.CommandOrScript = request.CommandOrScript.Trim();
            tool.DefaultParameters = request.DefaultParameters?.Trim();
            tool.Version = string.IsNullOrWhiteSpace(request.Version) ? "1.0.0" : request.Version.Trim();
            tool.Author = string.IsNullOrWhiteSpace(request.Author) ? "IT Admin" : request.Author.Trim();
            tool.IsActive = request.IsActive;
            tool.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[TOOL] Đã cập nhật công cụ: {Code} - {Name}", tool.Code, tool.Name);

            return ToDto(tool);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var tool = await _db.Tools.FindAsync(new object[] { id }, ct);
            if (tool == null) return false;

            _db.Tools.Remove(tool);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[TOOL] Đã xóa công cụ #{Id} - {Code}", id, tool.Code);
            return true;
        }

        private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return Enum.TryParse<TEnum>(value, true, out var result) ? result : defaultValue;
        }

        private static ToolDto ToDto(Tool t) => new()
        {
            Id = t.Id,
            Code = t.Code,
            Name = t.Name,
            Category = t.Category.ToString(),
            Description = t.Description,
            CommandOrScript = t.CommandOrScript,
            DefaultParameters = t.DefaultParameters,
            Version = t.Version,
            Author = t.Author,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}

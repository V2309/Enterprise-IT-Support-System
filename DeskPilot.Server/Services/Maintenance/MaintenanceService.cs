using DeskPilot.Server.Data;
using DeskPilot.Server.Models;
using DeskPilot.Server.Models.Enums;
using DeskPilot.Shared.DTO;
using Microsoft.EntityFrameworkCore;

namespace DeskPilot.Server.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<MaintenanceService> _logger;

        public MaintenanceService(AppDbContext db, ILogger<MaintenanceService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<MaintenanceScheduleDto>> GetAllAsync(
            string? status = null,
            string? department = null,
            Guid? deviceId = null,
            string? search = null,
            bool? isOverdue = null,
            CancellationToken ct = default)
        {
            var query = _db.MaintenanceSchedules
                .Include(m => m.Device)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MaintenanceStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(m => m.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                var d = department.Trim().ToLower();
                query = query.Where(m => (m.Department != null && m.Department.ToLower() == d) ||
                                         (m.Device != null && m.Device.Department != null && m.Device.Department.ToLower() == d));
            }

            if (deviceId.HasValue)
            {
                query = query.Where(m => m.DeviceId == deviceId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(m =>
                    m.Title.ToLower().Contains(s) ||
                    m.AssignedTechnician.ToLower().Contains(s) ||
                    (m.ChecklistNotes != null && m.ChecklistNotes.ToLower().Contains(s)) ||
                    (m.Device != null && (m.Device.DeviceCode.ToLower().Contains(s) || m.Device.Name.ToLower().Contains(s))));
            }

            if (isOverdue.HasValue && isOverdue.Value)
            {
                var now = DateTime.UtcNow;
                query = query.Where(m => m.Status != MaintenanceStatus.Completed &&
                                         m.Status != MaintenanceStatus.Cancelled &&
                                         ((m.NextDueDate != null && m.NextDueDate < now) ||
                                          (m.ScheduledDate < now && m.Status == MaintenanceStatus.Planned)));
            }

            var list = await query
                .OrderBy(m => m.ScheduledDate)
                .ToListAsync(ct);

            return list.Select(ToDto).ToList();
        }

        public async Task<MaintenanceScheduleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var item = await _db.MaintenanceSchedules
                .Include(m => m.Device)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            return item == null ? null : ToDto(item);
        }

        public async Task<MaintenanceScheduleDto> CreateAsync(MaintenanceScheduleUpsertRequest request, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var freq = ParseEnum(request.Frequency, MaintenanceFrequency.Quarterly);
            var scheduled = request.ScheduledDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(request.ScheduledDate, DateTimeKind.Utc) 
                : request.ScheduledDate.ToUniversalTime();

            var schedule = new MaintenanceSchedule
            {
                Title = request.Title.Trim(),
                DeviceId = request.DeviceId,
                Department = request.Department?.Trim(),
                MaintenanceType = ParseEnum(request.MaintenanceType, MaintenanceType.FullInspection),
                Frequency = freq,
                ScheduledDate = scheduled,
                NextDueDate = scheduled,
                AssignedTechnician = string.IsNullOrWhiteSpace(request.AssignedTechnician) ? "IT Support" : request.AssignedTechnician.Trim(),
                Status = ParseEnum(request.Status, MaintenanceStatus.Planned),
                ChecklistNotes = request.ChecklistNotes?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.MaintenanceSchedules.Add(schedule);
            await _db.SaveChangesAsync(ct);

            // Tải thông tin Device nếu có
            if (schedule.DeviceId.HasValue)
            {
                await _db.Entry(schedule).Reference(m => m.Device).LoadAsync(ct);
            }

            _logger.LogInformation("[MAINTENANCE] Đã tạo kế hoạch bảo trì mới: #{Id} - {Title}", schedule.Id, schedule.Title);

            return ToDto(schedule);
        }

        public async Task<MaintenanceScheduleDto?> UpdateAsync(Guid id, MaintenanceScheduleUpsertRequest request, CancellationToken ct = default)
        {
            var schedule = await _db.MaintenanceSchedules
                .Include(m => m.Device)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (schedule == null) return null;

            var scheduled = request.ScheduledDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(request.ScheduledDate, DateTimeKind.Utc) 
                : request.ScheduledDate.ToUniversalTime();

            schedule.Title = request.Title.Trim();
            schedule.DeviceId = request.DeviceId;
            schedule.Department = request.Department?.Trim();
            schedule.MaintenanceType = ParseEnum(request.MaintenanceType, MaintenanceType.FullInspection);
            schedule.Frequency = ParseEnum(request.Frequency, MaintenanceFrequency.Quarterly);
            schedule.ScheduledDate = scheduled;
            schedule.AssignedTechnician = string.IsNullOrWhiteSpace(request.AssignedTechnician) ? "IT Support" : request.AssignedTechnician.Trim();
            schedule.Status = ParseEnum(request.Status, MaintenanceStatus.Planned);
            schedule.ChecklistNotes = request.ChecklistNotes?.Trim();
            schedule.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[MAINTENANCE] Đã cập nhật kế hoạch bảo trì: #{Id} - {Title}", schedule.Id, schedule.Title);

            return ToDto(schedule);
        }

        public async Task<MaintenanceScheduleDto?> CompleteAsync(Guid id, CompleteMaintenanceRequest request, CancellationToken ct = default)
        {
            var schedule = await _db.MaintenanceSchedules
                .Include(m => m.Device)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (schedule == null) return null;

            var performedAt = request.PerformedDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.PerformedDate, DateTimeKind.Utc)
                : request.PerformedDate.ToUniversalTime();

            schedule.Status = MaintenanceStatus.Completed;
            schedule.LastPerformedDate = performedAt;
            schedule.ResultSummary = request.ResultSummary.Trim();
            if (!string.IsNullOrWhiteSpace(request.PerformedBy))
            {
                schedule.AssignedTechnician = request.PerformedBy.Trim();
            }

            // Nếu là bảo trì định kỳ, tính NextDueDate cho kỳ tiếp theo
            if (schedule.Frequency != MaintenanceFrequency.OneTime)
            {
                schedule.NextDueDate = CalculateNextDueDate(performedAt, schedule.Frequency);
            }

            schedule.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[MAINTENANCE] Đã hoàn tất bảo trì #{Id} - {Title}. Kỳ tiếp theo: {NextDueDate}",
                schedule.Id, schedule.Title, schedule.NextDueDate);

            return ToDto(schedule);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var schedule = await _db.MaintenanceSchedules.FindAsync(new object[] { id }, ct);
            if (schedule == null) return false;

            _db.MaintenanceSchedules.Remove(schedule);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[MAINTENANCE] Đã xóa kế hoạch bảo trì #{Id}", id);
            return true;
        }

        private static DateTime CalculateNextDueDate(DateTime baseDate, MaintenanceFrequency frequency) => frequency switch
        {
            MaintenanceFrequency.Weekly => baseDate.AddDays(7),
            MaintenanceFrequency.Monthly => baseDate.AddMonths(1),
            MaintenanceFrequency.Quarterly => baseDate.AddMonths(3),
            MaintenanceFrequency.SemiAnnually => baseDate.AddMonths(6),
            MaintenanceFrequency.Annually => baseDate.AddYears(1),
            _ => baseDate
        };

        private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return Enum.TryParse<TEnum>(value, true, out var result) ? result : defaultValue;
        }

        private static MaintenanceScheduleDto ToDto(MaintenanceSchedule m) => new()
        {
            Id = m.Id,
            Title = m.Title,
            DeviceId = m.DeviceId,
            DeviceCode = m.Device?.DeviceCode,
            DeviceName = m.Device?.Name,
            Department = m.Department ?? m.Device?.Department,
            MaintenanceType = m.MaintenanceType.ToString(),
            Frequency = m.Frequency.ToString(),
            ScheduledDate = m.ScheduledDate,
            LastPerformedDate = m.LastPerformedDate,
            NextDueDate = m.NextDueDate,
            AssignedTechnician = m.AssignedTechnician,
            Status = m.Status.ToString(),
            ChecklistNotes = m.ChecklistNotes,
            ResultSummary = m.ResultSummary,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };
    }
}

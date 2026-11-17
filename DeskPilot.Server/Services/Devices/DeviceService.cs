using DeskPilot.Server.Data;
using DeskPilot.Server.Models;
using DeskPilot.Server.Models.Enums;
using DeskPilot.Shared.DTO;
using Microsoft.EntityFrameworkCore;

namespace DeskPilot.Server.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(AppDbContext db, ILogger<DeviceService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<DeviceDto>> GetAllAsync(
            string? department = null,
            string? status = null,
            string? deviceType = null,
            string? search = null,
            CancellationToken ct = default)
        {
            var query = _db.Devices.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(d => d.Department != null && d.Department.ToLower() == department.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DeviceStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(d => d.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(deviceType) && Enum.TryParse<DeviceType>(deviceType, true, out var parsedType))
            {
                query = query.Where(d => d.DeviceType == parsedType);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(d =>
                    d.DeviceCode.ToLower().Contains(s) ||
                    d.Name.ToLower().Contains(s) ||
                    (d.SerialNumber != null && d.SerialNumber.ToLower().Contains(s)) ||
                    (d.AssignedTo != null && d.AssignedTo.ToLower().Contains(s)) ||
                    (d.IpAddress != null && d.IpAddress.ToLower().Contains(s)));
            }

            var devices = await query
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync(ct);

            return devices.Select(ToDto).ToList();
        }

        public async Task<DeviceDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var device = await _db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
            return device == null ? null : ToDto(device);
        }

        public async Task<DeviceDto?> GetByCodeAsync(string deviceCode, CancellationToken ct = default)
        {
            var device = await _db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.DeviceCode.ToLower() == deviceCode.Trim().ToLower(), ct);
            return device == null ? null : ToDto(device);
        }

        public async Task<DeviceDto> CreateAsync(DeviceUpsertRequest request, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var device = new Device
            {
                DeviceCode = request.DeviceCode.Trim(),
                Name = request.Name.Trim(),
                DeviceType = ParseEnum(request.DeviceType, DeviceType.Computer),
                SerialNumber = request.SerialNumber?.Trim(),
                Model = request.Model?.Trim(),
                Manufacturer = request.Manufacturer?.Trim(),
                IpAddress = request.IpAddress?.Trim(),
                MacAddress = request.MacAddress?.Trim(),
                Department = request.Department?.Trim(),
                AssignedTo = request.AssignedTo?.Trim(),
                Status = ParseEnum(request.Status, DeviceStatus.InUse),
                Specifications = request.Specifications?.Trim(),
                Notes = request.Notes?.Trim(),
                PurchaseDate = request.PurchaseDate,
                WarrantyExpiry = request.WarrantyExpiry,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Devices.Add(device);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[DEVICE] Đã tạo thiết bị mới: {DeviceCode} - {Name}", device.DeviceCode, device.Name);

            return ToDto(device);
        }

        public async Task<DeviceDto?> UpdateAsync(Guid id, DeviceUpsertRequest request, CancellationToken ct = default)
        {
            var device = await _db.Devices.FindAsync(new object[] { id }, ct);
            if (device == null) return null;

            device.DeviceCode = request.DeviceCode.Trim();
            device.Name = request.Name.Trim();
            device.DeviceType = ParseEnum(request.DeviceType, DeviceType.Computer);
            device.SerialNumber = request.SerialNumber?.Trim();
            device.Model = request.Model?.Trim();
            device.Manufacturer = request.Manufacturer?.Trim();
            device.IpAddress = request.IpAddress?.Trim();
            device.MacAddress = request.MacAddress?.Trim();
            device.Department = request.Department?.Trim();
            device.AssignedTo = request.AssignedTo?.Trim();
            device.Status = ParseEnum(request.Status, DeviceStatus.InUse);
            device.Specifications = request.Specifications?.Trim();
            device.Notes = request.Notes?.Trim();
            device.PurchaseDate = request.PurchaseDate;
            device.WarrantyExpiry = request.WarrantyExpiry;
            device.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[DEVICE] Đã cập nhật thiết bị: {DeviceCode} - {Name}", device.DeviceCode, device.Name);

            return ToDto(device);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var device = await _db.Devices.FindAsync(new object[] { id }, ct);
            if (device == null) return false;

            _db.Devices.Remove(device);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[DEVICE] Đã xóa thiết bị #{Id} - {DeviceCode}", id, device.DeviceCode);
            return true;
        }

        private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return Enum.TryParse<TEnum>(value, true, out var result) ? result : defaultValue;
        }

        private static DeviceDto ToDto(Device d) => new()
        {
            Id = d.Id,
            DeviceCode = d.DeviceCode,
            Name = d.Name,
            DeviceType = d.DeviceType.ToString(),
            SerialNumber = d.SerialNumber,
            Model = d.Model,
            Manufacturer = d.Manufacturer,
            IpAddress = d.IpAddress,
            MacAddress = d.MacAddress,
            Department = d.Department,
            AssignedTo = d.AssignedTo,
            Status = d.Status.ToString(),
            Specifications = d.Specifications,
            Notes = d.Notes,
            PurchaseDate = d.PurchaseDate,
            WarrantyExpiry = d.WarrantyExpiry,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        };
    }
}

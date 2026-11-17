using System;
using DeskPilot.Server.Models.Enums;

namespace DeskPilot.Server.Models
{
    public class Device
    {
        public Guid Id { get; set; }
        public string DeviceCode { get; set; } = string.Empty;     // Mã thiết bị / tài sản (ví dụ: TB-001)
        public string Name { get; set; } = string.Empty;           // Tên thiết bị (ví dụ: Laptop Dell Latitude 5420)
        public DeviceType DeviceType { get; set; } = DeviceType.Computer;
        public string? SerialNumber { get; set; }
        public string? Model { get; set; }
        public string? Manufacturer { get; set; }
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public string? Department { get; set; }                   // Phòng ban quản lý / sử dụng
        public string? AssignedTo { get; set; }                   // Người được giao sử dụng
        public DeviceStatus Status { get; set; } = DeviceStatus.InUse;
        public string? Specifications { get; set; }              // Cấu hình (Core i5, 16GB RAM, 512GB SSD)
        public string? Notes { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Quan hệ 1 Thiết bị có thể có nhiều Kế hoạch bảo trì
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();
    }
}

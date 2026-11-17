using System;
using DeskPilot.Server.Models.Enums;

namespace DeskPilot.Server.Models
{
    public class MaintenanceSchedule
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;                       // Tiêu đề kế hoạch (VD: Vệ sinh định kỳ quý 2 dàn máy Kế toán)
        public Guid? DeviceId { get; set; }                                     // Thiết bị cụ thể (nếu có)
        public virtual Device? Device { get; set; }                             // Navigation property
        public string? Department { get; set; }                                 // Phòng ban áp dụng (nếu bảo trì cả phòng)
        public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.FullInspection;
        public MaintenanceFrequency Frequency { get; set; } = MaintenanceFrequency.Quarterly;
        public DateTime ScheduledDate { get; set; }                             // Ngày dự kiến thực hiện
        public DateTime? LastPerformedDate { get; set; }                        // Ngày bảo trì gần nhất
        public DateTime? NextDueDate { get; set; }                              // Hạn bảo trì tiếp theo
        public string AssignedTechnician { get; set; } = "IT Support";          // Kỹ thuật viên phụ trách
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Planned;
        public string? ChecklistNotes { get; set; }                             // Hạng mục kiểm tra / ghi chú công việc
        public string? ResultSummary { get; set; }                              // Kết quả nghiệm thu sau bảo trì
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

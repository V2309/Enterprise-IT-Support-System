using System;

namespace DeskPilot.Shared.DTO
{
    public class MaintenanceScheduleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid? DeviceId { get; set; }
        public string? DeviceCode { get; set; }
        public string? DeviceName { get; set; }
        public string? Department { get; set; }
        public string MaintenanceType { get; set; } = "FullInspection";
        public string Frequency { get; set; } = "Quarterly";
        public DateTime ScheduledDate { get; set; }
        public DateTime? LastPerformedDate { get; set; }
        public DateTime? NextDueDate { get; set; }
        public string AssignedTechnician { get; set; } = "IT Support";
        public string Status { get; set; } = "Planned";
        public string? ChecklistNotes { get; set; }
        public string? ResultSummary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Cờ cảnh báo quá hạn dựa trên NextDueDate hoặc ScheduledDate
        public bool IsOverdue => Status != "Completed" && Status != "Cancelled" &&
            ((NextDueDate.HasValue && NextDueDate.Value < DateTime.UtcNow) ||
             (ScheduledDate < DateTime.UtcNow && Status == "Planned"));

        public string MaintenanceTypeDisplay => MaintenanceType switch
        {
            "HardwareCleaning" => "Vệ sinh phần cứng",
            "SoftwareUpdate" => "Cập nhật phần mềm",
            "SecurityCheck" => "Quét virus & Bảo mật",
            "BackupData" => "Sao lưu dữ liệu",
            "PrinterService" => "Bảo dưỡng máy in",
            "NetworkInspection" => "Kiểm tra mạng",
            "FullInspection" => "Kiểm tra toàn diện",
            _ => MaintenanceType
        };

        public string FrequencyDisplay => Frequency switch
        {
            "OneTime" => "Một lần",
            "Weekly" => "Hàng tuần",
            "Monthly" => "Hàng tháng",
            "Quarterly" => "Hàng quý (3T)",
            "SemiAnnually" => "6 tháng/lần",
            "Annually" => "Hàng năm",
            _ => Frequency
        };

        public string StatusDisplay => Status switch
        {
            "Planned" => "Đã lên lịch",
            "InProgress" => "Đang thực hiện",
            "Completed" => "Đã hoàn thành",
            "Overdue" => "Quá hạn",
            "Cancelled" => "Đã hủy",
            _ => Status
        };

        public string TargetDisplay => !string.IsNullOrEmpty(DeviceCode)
            ? $"{DeviceCode}"
            : (!string.IsNullOrEmpty(Department) ? $"Phòng {Department}" : "Toàn hệ thống");

        public string TargetSubDisplay => !string.IsNullOrEmpty(DeviceName)
            ? DeviceName
            : (!string.IsNullOrEmpty(Department) ? $"Thiết bị phòng {Department}" : "Tất cả máy");

        public bool HasDevice => !string.IsNullOrEmpty(DeviceCode);

        public string StatusBgColor => Status switch
        {
            "Completed" => "#ecfdf5",
            "Overdue" => "#fef2f2",
            "InProgress" => "#fffbeb",
            "Cancelled" => "#f1f5f9",
            _ => "#eff6ff"
        };

        public string StatusFgColor => Status switch
        {
            "Completed" => "#059669",
            "Overdue" => "#dc2626",
            "InProgress" => "#d97706",
            "Cancelled" => "#64748b",
            _ => "#2563eb"
        };

        public string StatusBorderColor => Status switch
        {
            "Completed" => "#a7f3d0",
            "Overdue" => "#fecaca",
            "InProgress" => "#fde68a",
            "Cancelled" => "#e2e8f0",
            _ => "#bfdbfe"
        };

        public string StatusDotColor => Status switch
        {
            "Completed" => "#10b981",
            "Overdue" => "#ef4444",
            "InProgress" => "#f59e0b",
            "Cancelled" => "#94a3b8",
            _ => "#3b82f6"
        };

        public string DueDateFormatted => ScheduledDate.ToLocalTime().ToString("dd/MM/yyyy");
        public string NextDueDateFormatted => NextDueDate.HasValue ? NextDueDate.Value.ToLocalTime().ToString("dd/MM/yyyy") : "—";
        public bool IsCompleted => Status == "Completed";
    }

    public class MaintenanceScheduleUpsertRequest
    {
        public string Title { get; set; } = string.Empty;
        public Guid? DeviceId { get; set; }
        public string? Department { get; set; }
        public string MaintenanceType { get; set; } = "FullInspection";
        public string Frequency { get; set; } = "Quarterly";
        public DateTime ScheduledDate { get; set; } = DateTime.UtcNow.AddDays(7);
        public string AssignedTechnician { get; set; } = "IT Support";
        public string Status { get; set; } = "Planned";
        public string? ChecklistNotes { get; set; }
    }

    public class CompleteMaintenanceRequest
    {
        public string ResultSummary { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = "IT Support";
        public DateTime PerformedDate { get; set; } = DateTime.UtcNow;
    }
}

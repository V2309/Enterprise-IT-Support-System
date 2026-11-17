namespace DeskPilot.Server.Models.Enums
{
    public enum MaintenanceStatus
    {
        Planned,          // Đã lên lịch dự kiến
        InProgress,       // Đang tiến hành bảo trì
        Completed,        // Đã hoàn thành nghiệm thu
        Overdue,          // Quá hạn bảo trì
        Cancelled         // Đã hủy bỏ
    }
}

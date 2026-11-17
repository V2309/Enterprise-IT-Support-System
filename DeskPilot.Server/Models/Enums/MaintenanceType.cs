namespace DeskPilot.Server.Models.Enums
{
    public enum MaintenanceType
    {
        HardwareCleaning,    // Vệ sinh phần cứng, tra keo tản nhiệt, thổi bụi
        SoftwareUpdate,      // Cập nhật Windows Patch, Driver, phần mềm ứng dụng
        SecurityCheck,       // Quét virus, kiểm tra tường lửa, rà soát lỗ hổng
        BackupData,          // Sao lưu dữ liệu quan trọng, kiểm tra phân vùng ổ cứng
        PrinterService,      // Bảo dưỡng máy in, vệ sinh drum, nạp mực
        NetworkInspection,   // Kiểm tra đường truyền mạng, cáp LAN, switch, Wi-Fi
        FullInspection,      // Kiểm tra tổng thể định kỳ
        Other                // Khác
    }
}

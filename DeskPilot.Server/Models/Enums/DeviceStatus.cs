namespace DeskPilot.Server.Models.Enums
{
    public enum DeviceStatus
    {
        InUse,            // Đang sử dụng
        InStock,          // Trong kho / Sẵn sàng cấp phát
        Maintenance,      // Đang bảo trì / Sửa chữa
        Broken,           // Hỏng hóc
        Disposed          // Đã thanh lý
    }
}

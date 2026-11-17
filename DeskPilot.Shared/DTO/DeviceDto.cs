using System;

namespace DeskPilot.Shared.DTO
{
    public class DeviceDto
    {
        public Guid Id { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "Computer";
        public string? SerialNumber { get; set; }
        public string? Model { get; set; }
        public string? Manufacturer { get; set; }
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public string? Department { get; set; }
        public string? AssignedTo { get; set; }
        public string Status { get; set; } = "InUse";
        public string? Specifications { get; set; }
        public string? Notes { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string DeviceTypeDisplay => DeviceType switch
        {
            "Computer" => "Máy tính bàn",
            "Laptop" => "Laptop",
            "Printer" => "Máy in",
            "Monitor" => "Màn hình",
            "NetworkDevice" => "Thiết bị mạng",
            "Peripheral" => "Ngoại vi",
            "Server" => "Máy chủ",
            _ => DeviceType
        };

        public string StatusDisplay => Status switch
        {
            "InUse" => "Đang sử dụng",
            "InStock" => "Trong kho",
            "Maintenance" => "Bảo trì",
            "Broken" => "Hỏng hóc",
            "Disposed" => "Đã thanh lý",
            _ => Status
        };

        public string StatusBgColor => Status switch
        {
            "InUse" => "#ecfdf5",
            "InStock" => "#eff6ff",
            "Maintenance" => "#fffbeb",
            "Broken" => "#fef2f2",
            _ => "#f1f5f9"
        };

        public string StatusFgColor => Status switch
        {
            "InUse" => "#059669",
            "InStock" => "#2563eb",
            "Maintenance" => "#d97706",
            "Broken" => "#dc2626",
            _ => "#64748b"
        };

        public string StatusBorderColor => Status switch
        {
            "InUse" => "#a7f3d0",
            "InStock" => "#bfdbfe",
            "Maintenance" => "#fde68a",
            "Broken" => "#fecaca",
            _ => "#e2e8f0"
        };

        public string StatusDotColor => Status switch
        {
            "InUse" => "#10b981",
            "InStock" => "#3b82f6",
            "Maintenance" => "#f59e0b",
            "Broken" => "#ef4444",
            _ => "#94a3b8"
        };
    }

    public class DeviceUpsertRequest
    {
        public string DeviceCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "Computer";
        public string? SerialNumber { get; set; }
        public string? Model { get; set; }
        public string? Manufacturer { get; set; }
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public string? Department { get; set; }
        public string? AssignedTo { get; set; }
        public string Status { get; set; } = "InUse";
        public string? Specifications { get; set; }
        public string? Notes { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
    }
}

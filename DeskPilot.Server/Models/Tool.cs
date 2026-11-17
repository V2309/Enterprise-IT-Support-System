using System;
using DeskPilot.Server.Models.Enums;

namespace DeskPilot.Server.Models
{
    public class Tool
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;              // Mã công cụ / Slug (ví dụ: FLUSH-DNS, RESTART-SPOOLER)
        public string Name { get; set; } = string.Empty;              // Tên công cụ hiển thị
        public ToolCategory Category { get; set; } = ToolCategory.System;
        public string Description { get; set; } = string.Empty;       // Mô tả tác dụng
        public string CommandOrScript { get; set; } = string.Empty;   // Lệnh / Script thực thi (PowerShell, CMD, Batch...)
        public string? DefaultParameters { get; set; }                // Tham số dòng lệnh mặc định
        public string Version { get; set; } = "1.0.0";                // Phiên bản công cụ
        public string Author { get; set; } = "IT Admin";              // Tác giả / Người tạo
        public bool IsActive { get; set; } = true;                    // Kích hoạt
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System;

namespace DeskPilot.Shared.DTO
{
    public class ToolDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "System";
        public string Description { get; set; } = string.Empty;
        public string CommandOrScript { get; set; } = string.Empty;
        public string? DefaultParameters { get; set; }
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = "IT Admin";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string CategoryDisplay => Category switch
        {
            "System" => "Hệ thống",
            "Network" => "Mạng & DNS",
            "Printer" => "Máy in",
            "Maintenance" => "Bảo trì / Dọn rác",
            "Security" => "Bảo mật",
            "Diagnostic" => "Chẩn đoán",
            _ => Category
        };

        public string StatusDisplay => IsActive ? "Hoạt động" : "Tạm tắt";
        public string StatusBgColor => IsActive ? "#f0fdf4" : "#f8fafc";
        public string StatusFgColor => IsActive ? "#16a34a" : "#64748b";
        public string StatusBorderColor => IsActive ? "#bbf7d0" : "#e2e8f0";
        public string StatusDotColor => IsActive ? "#22c55e" : "#94a3b8";
    }

    public class ToolUpsertRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "System";
        public string Description { get; set; } = string.Empty;
        public string CommandOrScript { get; set; } = string.Empty;
        public string? DefaultParameters { get; set; }
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = "IT Admin";
        public bool IsActive { get; set; } = true;
    }
}

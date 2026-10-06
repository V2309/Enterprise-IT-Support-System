using System;

namespace DeskPilot.Shared
{
    // Bảng lưu trữ thông tin phiếu yêu cầu hỗ trợ kỹ thuật gửi từ Client
    public class TicketDto
    {
        public int Id { get; set; }                             // Mã phiếu tự tăng (Primary Key)
        public string MachineName { get; set; } = string.Empty; // Máy gửi yêu cầu
        public string UserName { get; set; } = string.Empty;    // Nhân viên báo sự cố
        public string Description { get; set; } = string.Empty; // Nội dung chi tiết lỗi
        public string Status { get; set; } = "Open";            // Trạng thái: Open, InProgress, Resolved
       
        private DateTime _createdAt = DateTime.UtcNow;

        // Lưu chuẩn UTC xuống DB, nhưng khi lấy ra giao diện hiển thị sẽ chuyển sang Giờ Việt Nam (+7)
        public DateTime CreatedAt
        {
            get => _createdAt.Kind == DateTimeKind.Utc ? _createdAt.ToLocalTime() : _createdAt;
            set => _createdAt = value;
        }
    }

    // Bảng lưu lại nhật ký lệnh IT đã bấm điều khiển xuống máy Client (Audit Log)
    public class ActionLogDto
    {
        public int Id { get; set; }                             // Mã nhật ký tự tăng (Primary Key)
        public string TargetMachine { get; set; } = string.Empty;// Tên máy nhận lệnh
        public string CommandName { get; set; } = string.Empty;  // Tên kịch bản (FixPrint, FlushDns)
        public string ExecutedBy { get; set; } = "IT Admin";     // Người thực thi lệnh
        public DateTime _executedAt  = DateTime.UtcNow; 
        
        // Lưu chuẩn UTC xuống DB, nhưng khi lấy ra giao diện hiển thị sẽ chuyển sang Giờ Việt Nam (+7)
        public DateTime ExecutedAt
        {
            get => _executedAt.Kind == DateTimeKind.Utc ? _executedAt.ToLocalTime() : _executedAt;
            set => _executedAt = value;
        }

    }
}
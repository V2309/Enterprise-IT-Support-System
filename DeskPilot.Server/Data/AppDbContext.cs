using Microsoft.EntityFrameworkCore;
using DeskPilot.Shared;
using System;

namespace DeskPilot.Server.Data
{
    // Lớp DbContext quản lý kết nối và các bảng PostgreSQL
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Bảng lưu danh sách thiết bị và trạng thái mới nhất
        public DbSet<MachineStatusEntity> Machines => Set<MachineStatusEntity>();

        // Bảng lưu danh sách ticket hỗ trợ
        public DbSet<TicketDto> Tickets => Set<TicketDto>();

        // Bảng lưu lịch sử thực thi lệnh (Audit Log)
        public DbSet<ActionLogDto> ActionLogs => Set<ActionLogDto>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Dùng MachineName làm khóa chính cho bảng Machines
            modelBuilder.Entity<MachineStatusEntity>().HasKey(m => m.MachineName);
        }
    }

    // Entity ánh xạ bảng Machines trong database
    public class MachineStatusEntity
    {
        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public double RamUsagePercent { get; set; }
        public double FreeDiskSpaceGB { get; set; }
        public bool IsOnline { get; set; }
        public string SpoolerStatus { get; set; } = "Running";
        public string StatusMessage { get; set; } = "Bình thường";
        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}
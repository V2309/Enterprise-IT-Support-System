using DeskPilot.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskPilot.Server.Data
{
    // Lớp DbContext quản lý kết nối và các bảng PostgreSQL
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Bảng lưu danh sách thiết bị và trạng thái mới nhất
        public DbSet<Machine> Machines => Set<Machine>();

        // Bảng lưu danh sách ticket hỗ trợ
        public DbSet<Ticket> Tickets => Set<Ticket>();

        // Bảng lưu lịch sử thực thi lệnh (Audit Log)
        public DbSet<ActionLog> ActionLogs => Set<ActionLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tự động nạp cấu hình Fluent API từ Data/Configuration
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
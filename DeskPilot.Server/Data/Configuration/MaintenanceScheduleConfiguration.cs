using DeskPilot.Server.Models;
using DeskPilot.Server.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskPilot.Server.Data.Configuration
{
    public class MaintenanceScheduleConfiguration : IEntityTypeConfiguration<MaintenanceSchedule>
    {
        public void Configure(EntityTypeBuilder<MaintenanceSchedule> builder)
        {
            builder.ToTable("maintenance_schedules");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(m => m.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.Department)
                .HasMaxLength(100);

            builder.Property(m => m.MaintenanceType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(m => m.Frequency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(m => m.ScheduledDate)
                .IsRequired();

            builder.Property(m => m.AssignedTechnician)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("IT Support");

            builder.Property(m => m.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(m => m.ChecklistNotes)
                .HasMaxLength(2000);

            builder.Property(m => m.ResultSummary)
                .HasMaxLength(2000);

            builder.Property(m => m.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(m => m.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Khóa ngoại liên kết thiết bị (SetNull nếu thiết bị bị xóa)
            builder.HasOne(m => m.Device)
                .WithMany(d => d.MaintenanceSchedules)
                .HasForeignKey(m => m.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(m => m.Status);
            builder.HasIndex(m => m.NextDueDate);
            builder.HasIndex(m => m.DeviceId);
            builder.HasIndex(m => m.Department);
        }
    }
}

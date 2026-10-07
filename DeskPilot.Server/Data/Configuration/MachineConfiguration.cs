using DeskPilot.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskPilot.Server.Data.Configuration
{
    public class MachineConfiguration : IEntityTypeConfiguration<Machine>
    {
        public void Configure(EntityTypeBuilder<Machine> builder)
        {
            builder.ToTable("machines");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(m => m.MachineName)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(m => m.MachineName)
                .IsUnique();

            builder.Property(m => m.UserName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.CpuUsagePercent)
                .HasDefaultValue(0.0);

            builder.Property(m => m.RamUsagePercent)
                .HasDefaultValue(0.0);

            builder.Property(m => m.TotalRamGB)
                .HasDefaultValue(0.0);

            builder.Property(m => m.UsedRamGB)
                .HasDefaultValue(0.0);

            builder.Property(m => m.TotalDiskSpaceGB)
                .HasDefaultValue(0.0);

            builder.Property(m => m.FreeDiskSpaceGB)
                .HasDefaultValue(0.0);

            builder.Property(m => m.DiskUsagePercent)
                .HasDefaultValue(0.0);

            builder.Property(m => m.IsOnline)
                .HasDefaultValue(false);

            builder.Property(m => m.SpoolerStatus)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue("Running");

            builder.Property(m => m.StatusMessage)
                .IsRequired()
                .HasMaxLength(255)
                .HasDefaultValue("Bình thường");

            builder.Property(m => m.LastSeen)
                .HasDefaultValueSql("NOW()");

            builder.Property(m => m.CreatedAt)
                .HasDefaultValueSql("NOW()");

            builder.HasIndex(m => new { m.IsOnline, m.LastSeen });
        }
    }
}

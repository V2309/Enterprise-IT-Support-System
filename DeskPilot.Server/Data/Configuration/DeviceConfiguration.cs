using DeskPilot.Server.Models;
using DeskPilot.Server.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskPilot.Server.Data.Configuration
{
    public class DeviceConfiguration : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> builder)
        {
            builder.ToTable("devices");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(d => d.DeviceCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(d => d.DeviceCode)
                .IsUnique();

            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(d => d.DeviceType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(d => d.SerialNumber)
                .HasMaxLength(100);

            builder.Property(d => d.Model)
                .HasMaxLength(100);

            builder.Property(d => d.Manufacturer)
                .HasMaxLength(100);

            builder.Property(d => d.IpAddress)
                .HasMaxLength(45);

            builder.Property(d => d.MacAddress)
                .HasMaxLength(30);

            builder.Property(d => d.Department)
                .HasMaxLength(100);

            builder.Property(d => d.AssignedTo)
                .HasMaxLength(100);

            builder.Property(d => d.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(d => d.Specifications)
                .HasMaxLength(1000);

            builder.Property(d => d.Notes)
                .HasMaxLength(1000);

            builder.Property(d => d.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(d => d.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.HasIndex(d => d.Department);
            builder.HasIndex(d => d.Status);
            builder.HasIndex(d => d.DeviceType);
        }
    }
}

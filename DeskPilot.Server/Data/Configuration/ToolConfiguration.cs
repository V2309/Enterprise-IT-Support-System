using DeskPilot.Server.Models;
using DeskPilot.Server.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskPilot.Server.Data.Configuration
{
    public class ToolConfiguration : IEntityTypeConfiguration<Tool>
    {
        public void Configure(EntityTypeBuilder<Tool> builder)
        {
            builder.ToTable("tools");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(t => t.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(t => t.Code)
                .IsUnique();

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(t => t.Category)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.Property(t => t.CommandOrScript)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(t => t.DefaultParameters)
                .HasMaxLength(500);

            builder.Property(t => t.Version)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue("1.0.0");

            builder.Property(t => t.Author)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("IT Admin");

            builder.Property(t => t.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(t => t.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(t => t.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.HasIndex(t => t.Category);
            builder.HasIndex(t => t.IsActive);
        }
    }
}

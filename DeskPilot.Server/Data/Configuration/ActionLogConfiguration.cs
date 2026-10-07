using DeskPilot.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskPilot.Server.Data.Configuration
{
    public class ActionLogConfiguration : IEntityTypeConfiguration<ActionLog>
    {
        public void Configure(EntityTypeBuilder<ActionLog> builder)
        {
            builder.ToTable("action_logs");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.TargetMachine)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.CommandName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(a => a.ExecutedBy)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("IT Admin");

            builder.Property(a => a.ExecutedAt)
                .HasDefaultValueSql("NOW()");

            builder.HasIndex(a => a.ExecutedAt);
        }
    }
}

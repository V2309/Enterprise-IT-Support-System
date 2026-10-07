using DeskPilot.Server.Models;
using DeskPilot.Server.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskPilot.Server.Data.Configuration
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("tickets");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.MachineName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.UserName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Description)
                .IsRequired();

            builder.Property(t => t.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(TicketStatus.Open);

            builder.Property(t => t.CreatedAt)
                .HasDefaultValueSql("NOW()");

            builder.Property(t => t.ResolvedAt);

            builder.HasIndex(t => t.MachineName);

            builder.HasIndex(t => t.Status);
        }
    }
}

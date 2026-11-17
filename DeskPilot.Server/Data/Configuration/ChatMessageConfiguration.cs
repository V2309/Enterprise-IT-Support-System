using DeskPilot.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskPilot.Server.Data.Configuration
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("chat_messages");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.MachineName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.SenderName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Message)
                .IsRequired(false)
                .HasMaxLength(2000);

            builder.Property(c => c.ImageUrl)
                .IsRequired(false)
                .HasMaxLength(1000);

            builder.Property(c => c.IsFromAdmin)
                .IsRequired();

            builder.Property(c => c.SentAt)
                .HasDefaultValueSql("NOW()");

            // Index để truy vấn lịch sử chat theo máy và thời gian
            builder.HasIndex(c => c.MachineName);
            builder.HasIndex(c => c.SentAt);
        }
    }
}

using System;

namespace DeskPilot.Server.Models
{
    /// <summary>
    /// Entity lưu tin nhắn chat giữa IT Admin và nhân viên.
    /// </summary>
    public class ChatMessage
    {
        public int Id { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsFromAdmin { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}

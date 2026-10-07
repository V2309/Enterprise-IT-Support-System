using System;

namespace DeskPilot.Server.Models
{
    public class ActionLog
    {
        public int Id { get; set; }
        public string TargetMachine { get; set; } = string.Empty;
        public string CommandName { get; set; } = string.Empty;
        public string ExecutedBy { get; set; } = "IT Admin";
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }
}

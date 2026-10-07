using System;

namespace DeskPilot.Server.Models
{
    public class Machine
    {
        public Guid Id { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public double CpuUsagePercent { get; set; }
        public double RamUsagePercent { get; set; }
        public double TotalRamGB { get; set; }
        public double UsedRamGB { get; set; }
        public double TotalDiskSpaceGB { get; set; }
        public double FreeDiskSpaceGB { get; set; }
        public double DiskUsagePercent { get; set; }
        public bool IsOnline { get; set; }
        public string SpoolerStatus { get; set; } = "Running";
        public string StatusMessage { get; set; } = "Bình thường";
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

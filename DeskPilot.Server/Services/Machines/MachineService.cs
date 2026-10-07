using DeskPilot.Server.Data;
using DeskPilot.Server.Models;
using DeskPilot.Shared.DTO;
using Microsoft.EntityFrameworkCore;

namespace DeskPilot.Server.Services
{
    public class MachineService : IMachineService
    {
        private readonly AppDbContext _db;

        public MachineService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<MachineStatusDto>> GetAllAsync(CancellationToken ct = default)
        {
            var machines = await _db.Machines
                .AsNoTracking()
                .OrderBy(m => m.MachineName)
                .ToListAsync(ct);

            return machines.Select(ToDto).ToList();
        }

        public async Task SaveSnapshotsAsync(IReadOnlyCollection<MachineStatusDto> snapshots, CancellationToken ct = default)
        {
            if (snapshots.Count == 0) return;

            var names = snapshots.Select(s => s.MachineName).ToList();
            var existing = await _db.Machines
                .Where(m => names.Contains(m.MachineName))
                .ToDictionaryAsync(m => m.MachineName, StringComparer.OrdinalIgnoreCase, ct);

            foreach (var s in snapshots)
            {
                if (!existing.TryGetValue(s.MachineName, out var machine))
                {
                    machine = new Machine { MachineName = s.MachineName, CreatedAt = DateTime.UtcNow };
                    _db.Machines.Add(machine);
                    existing[s.MachineName] = machine;
                }

                machine.UserName = s.UserName;
                machine.CpuUsagePercent = s.CpuUsagePercent;
                machine.RamUsagePercent = s.RamUsagePercent;
                machine.TotalRamGB = s.TotalRamGB;
                machine.UsedRamGB = s.UsedRamGB;
                machine.TotalDiskSpaceGB = s.TotalDiskSpaceGB;
                machine.FreeDiskSpaceGB = s.FreeDiskSpaceGB;
                machine.DiskUsagePercent = s.DiskUsagePercent;
                machine.SpoolerStatus = s.SpoolerStatus;
                machine.StatusMessage = s.StatusMessage;
                machine.IsOnline = s.IsOnline;
                machine.LastSeen = DateTime.SpecifyKind(s.LastSeen, DateTimeKind.Utc);
            }

            await _db.SaveChangesAsync(ct);
        }

        public Task MarkAllOfflineAsync(CancellationToken ct = default) =>
            _db.Machines
                .Where(m => m.IsOnline)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsOnline, false), ct);

        private static MachineStatusDto ToDto(Machine m) => new()
        {
            MachineName = m.MachineName,
            UserName = m.UserName,
            CpuUsagePercent = m.CpuUsagePercent,
            RamUsagePercent = m.RamUsagePercent,
            TotalRamGB = m.TotalRamGB,
            UsedRamGB = m.UsedRamGB,
            TotalDiskSpaceGB = m.TotalDiskSpaceGB,
            FreeDiskSpaceGB = m.FreeDiskSpaceGB,
            DiskUsagePercent = m.DiskUsagePercent,
            IsOnline = m.IsOnline,
            SpoolerStatus = m.SpoolerStatus,
            StatusMessage = m.StatusMessage,
            LastSeen = m.LastSeen
        };
    }
}

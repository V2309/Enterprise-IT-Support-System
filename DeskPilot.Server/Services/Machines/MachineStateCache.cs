using System.Collections.Concurrent;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Server.Services
{
    // Bộ nhớ đệm trạng thái máy trạm (Singleton).
    // Heartbeat chỉ cập nhật RAM, việc ghi xuống PostgreSQL được gom lại theo chu kỳ bởi HeartbeatMonitorService
    // => giảm từ ~1 UPDATE / 3 giây / máy xuống còn 1 batch / 30 giây cho toàn bộ máy.
    public class MachineStateCache
    {
        private readonly ConcurrentDictionary<string, MachineStatusDto> _machines = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _dirty = new(StringComparer.OrdinalIgnoreCase);

        // Nạp dữ liệu ban đầu từ DB khi Server khởi động (tất cả coi như offline cho tới khi có heartbeat)
        public void Seed(IEnumerable<MachineStatusDto> machines)
        {
            foreach (var m in machines)
            {
                var copy = Copy(m);
                copy.IsOnline = false;
                _machines[copy.MachineName] = copy;
            }
        }

        // Ghi nhận heartbeat, trả về bản snapshot để broadcast
        public MachineStatusDto Upsert(MachineStatusDto status)
        {
            var snapshot = Copy(status);
            snapshot.IsOnline = true;
            snapshot.LastSeen = DateTime.UtcNow;

            _machines[snapshot.MachineName] = snapshot;
            _dirty[snapshot.MachineName] = 0;
            return Copy(snapshot);
        }

        public bool IsOnline(string machineName) =>
            _machines.TryGetValue(machineName, out var m) && m.IsOnline;

        public List<MachineStatusDto> GetAll() =>
            _machines.Values
                .OrderBy(m => m.MachineName, StringComparer.OrdinalIgnoreCase)
                .Select(Copy)
                .ToList();

        // Đánh dấu offline các máy quá hạn heartbeat, trả về danh sách tên máy vừa offline
        public List<string> MarkTimedOut(TimeSpan timeout)
        {
            var threshold = DateTime.UtcNow - timeout;
            var result = new List<string>();

            foreach (var (name, current) in _machines)
            {
                if (!current.IsOnline || current.LastSeen >= threshold) continue;

                var offline = Copy(current);
                offline.IsOnline = false;

                // TryUpdate đảm bảo không ghi đè heartbeat mới vừa đến trong lúc quét
                if (_machines.TryUpdate(name, offline, current))
                {
                    _dirty[name] = 0;
                    result.Add(name);
                }
            }

            return result;
        }

        // Lấy (và xóa cờ) các bản ghi cần lưu xuống DB
        public List<MachineStatusDto> TakeDirty()
        {
            var result = new List<MachineStatusDto>();
            foreach (var name in _dirty.Keys)
            {
                if (_dirty.TryRemove(name, out _) && _machines.TryGetValue(name, out var m))
                {
                    result.Add(Copy(m));
                }
            }
            return result;
        }

        // Đánh dấu lại khi lưu DB thất bại để lần sau thử lại
        public void MarkDirty(IEnumerable<string> machineNames)
        {
            foreach (var name in machineNames) _dirty[name] = 0;
        }

        private static MachineStatusDto Copy(MachineStatusDto s) => new()
        {
            MachineName = s.MachineName,
            UserName = s.UserName,
            CpuUsagePercent = s.CpuUsagePercent,
            RamUsagePercent = s.RamUsagePercent,
            TotalRamGB = s.TotalRamGB,
            UsedRamGB = s.UsedRamGB,
            TotalDiskSpaceGB = s.TotalDiskSpaceGB,
            FreeDiskSpaceGB = s.FreeDiskSpaceGB,
            DiskUsagePercent = s.DiskUsagePercent,
            IsOnline = s.IsOnline,
            SpoolerStatus = s.SpoolerStatus,
            StatusMessage = s.StatusMessage,
            LastSeen = s.LastSeen
        };
    }
}

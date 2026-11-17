using DeskPilot.Shared.DTO;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DeskPilot.Admin.ViewModels
{
    public partial class DashboardViewModel
    {
        private readonly ConcurrentDictionary<string, DateTime> _lastAlertTimes = new();
        private readonly ConcurrentDictionary<string, List<MetricPoint>> _machineHistories = new();

        public ObservableCollection<MachineStatusDto> Machines { get; set; } = new();
        public ObservableCollection<MachineStatusDto> FilteredMachines { get; set; } = new();

        public int OnlineCount => Machines.Count(m => m.IsOnline);
        public int OfflineCount => Machines.Count(m => !m.IsOnline);
        public int TotalMachinesCount => Machines.Count;

        public event Action<string>? OnMetricsHistoryUpdated;

        public List<MetricPoint> GetMetricsHistory(string machineName, double fallbackRam = 0.0, double fallbackDisk = 0.0)
        {
            var history = _machineHistories.GetOrAdd(machineName, _ => new List<MetricPoint>());
            lock (history)
            {
                if (history.Count == 0 && (fallbackRam > 0 || fallbackDisk > 0))
                {
                    var machine = Machines.FirstOrDefault(m => m.MachineName.Equals(machineName, StringComparison.OrdinalIgnoreCase));
                    history.Add(new MetricPoint
                    {
                        Timestamp = DateTime.Now,
                        RamUsagePercent = fallbackRam,
                        DiskUsagePercent = fallbackDisk,
                        CpuUsagePercent = machine?.CpuUsagePercent ?? 0.0,
                        FreeDiskSpaceGB = machine?.FreeDiskSpaceGB ?? 0.0
                    });
                }
                return new List<MetricPoint>(history);
            }
        }

        public void AppendMetricPoint(MachineStatusDto status)
        {
            var history = _machineHistories.GetOrAdd(status.MachineName, _ => new List<MetricPoint>());
            lock (history)
            {
                double diskPercent = status.DiskUsagePercent > 0 ? status.DiskUsagePercent :
                    (status.TotalDiskSpaceGB > 0 ? Math.Round((status.TotalDiskSpaceGB - status.FreeDiskSpaceGB) / status.TotalDiskSpaceGB * 100, 1) : 0.0);

                history.Add(new MetricPoint
                {
                    Timestamp = DateTime.Now,
                    RamUsagePercent = status.RamUsagePercent,
                    DiskUsagePercent = diskPercent,
                    CpuUsagePercent = status.CpuUsagePercent,
                    FreeDiskSpaceGB = status.FreeDiskSpaceGB
                });

                if (history.Count > 30)
                {
                    history.RemoveAt(0);
                }
            }
            OnMetricsHistoryUpdated?.Invoke(status.MachineName);
        }

        private static void ApplyStatus(MachineStatusDto target, MachineStatusDto source)
        {
            target.UserName = source.UserName;
            target.RamUsagePercent = source.RamUsagePercent;
            target.CpuUsagePercent = source.CpuUsagePercent;
            target.TotalRamGB = source.TotalRamGB;
            target.UsedRamGB = source.UsedRamGB;
            target.TotalDiskSpaceGB = source.TotalDiskSpaceGB;
            target.FreeDiskSpaceGB = source.FreeDiskSpaceGB;
            target.DiskUsagePercent = source.DiskUsagePercent;
            target.SpoolerStatus = source.SpoolerStatus;
            target.StatusMessage = source.StatusMessage;
            target.IsOnline = source.IsOnline;
            target.LastSeen = source.LastSeen;
        }

        private MachineStatusDto? FindMachine(string machineName) =>
            Machines.FirstOrDefault(m => m.MachineName.Equals(machineName, StringComparison.OrdinalIgnoreCase));

        private void UpdateCounts()
        {
            OnPropertyChanged(nameof(OnlineCount));
            OnPropertyChanged(nameof(OfflineCount));
            OnPropertyChanged(nameof(TotalMachinesCount));
        }

        public void FilterMachines()
        {
            FilteredMachines.Clear();
            var query = SearchText.Trim().ToLowerInvariant();

            foreach (var m in Machines)
            {
                if (string.IsNullOrEmpty(query) ||
                    m.MachineName.ToLowerInvariant().Contains(query) ||
                    m.UserName.ToLowerInvariant().Contains(query))
                {
                    FilteredMachines.Add(m);
                }
            }
        }

        private void CheckAndAlertResourceThreshold(MachineStatusDto status)
        {
            bool isHighRam = status.RamUsagePercent >= 85;
            bool isLowDisk = status.FreeDiskSpaceGB <= 5;

            if (!isHighRam && !isLowDisk) return;

            if (_lastAlertTimes.TryGetValue(status.MachineName, out var lastTime)
                && (DateTime.Now - lastTime).TotalMinutes < 2)
            {
                return;
            }

            _lastAlertTimes[status.MachineName] = DateTime.Now;

            string alertTitle = $"[CẢNH BÁO TÀI NGUYÊN] Máy: {status.MachineName}";
            string alertContent;

            if (isHighRam && isLowDisk)
            {
                alertContent = $"RAM quá tải ({status.RamUsagePercent:F1}%) VÀ Ổ C sắp đầy (còn {status.FreeDiskSpaceGB:F1} GB)!";
            }
            else if (isHighRam)
            {
                alertContent = $"RAM đang chạm ngưỡng nguy hiểm: {status.RamUsagePercent:F1}%!";
            }
            else
            {
                alertContent = $"Dung lượng ổ C sắp cạn: còn {status.FreeDiskSpaceGB:F1} GB!";
            }

            new ToastContentBuilder()
                .AddText(alertTitle)
                .AddText(alertContent)
                .Show();
        }
    }
}

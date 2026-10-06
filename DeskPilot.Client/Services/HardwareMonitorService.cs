using System;
using System.IO;
using System.ServiceProcess;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Client.Services
{
    public class HardwareMonitorService
    {
        public MachineStatusDto GetCurrentStatus()
        {
            var status = new MachineStatusDto
            {
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                IsOnline = true,
                LastSeen = DateTime.Now
            };

            // 1. Lấy thông tin dung lượng ổ C
            try
            {
                var driveC = new DriveInfo("C");
                if (driveC.IsReady)
                {
                    double total = Math.Round(driveC.TotalSize / (1024.0 * 1024 * 1024), 0);
                    double free = Math.Round(driveC.AvailableFreeSpace / (1024.0 * 1024 * 1024), 0);
                    double used = total - free;
                    status.TotalDiskSpaceGB = total;
                    status.FreeDiskSpaceGB = free;
                    status.DiskUsagePercent = total > 0 ? Math.Round((used / total) * 100, 0) : 0;
                }
            }
            catch
            {
                status.FreeDiskSpaceGB = -1;
            }

            // 2. Lấy RAM chi tiết từ Windows API
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    status.RamUsagePercent = memStatus.dwMemoryLoad;
                    status.TotalRamGB = Math.Round(memStatus.ullTotalPhys / (1024.0 * 1024 * 1024), 1);
                    status.UsedRamGB = Math.Round((memStatus.ullTotalPhys - memStatus.ullAvailPhys) / (1024.0 * 1024 * 1024), 1);
                }
            }
            catch { }

            // 3. Đo tải CPU
            status.CpuUsagePercent = GetCpuUsage();

            // 4. Kiểm tra trạng thái dịch vụ Print Spooler (Máy in)
            try
            {
                using var spooler = new ServiceController("Spooler");
                status.SpoolerStatus = spooler.Status.ToString();
            }
            catch
            {
                status.SpoolerStatus = "Unknown";
            }

            return status;
        }

        private static ulong _prevIdleTime;
        private static ulong _prevKernelTime;
        private static ulong _prevUserTime;

        private double GetCpuUsage()
        {
            try
            {
                if (GetSystemTimes(out var idle, out var kernel, out var user))
                {
                    ulong currentIdle = ((ulong)idle.dwHighDateTime << 32) | (uint)idle.dwLowDateTime;
                    ulong currentKernel = ((ulong)kernel.dwHighDateTime << 32) | (uint)kernel.dwLowDateTime;
                    ulong currentUser = ((ulong)user.dwHighDateTime << 32) | (uint)user.dwLowDateTime;

                    if (_prevKernelTime != 0)
                    {
                        ulong idleDiff = currentIdle - _prevIdleTime;
                        ulong kernelDiff = currentKernel - _prevKernelTime;
                        ulong userDiff = currentUser - _prevUserTime;
                        ulong totalDiff = kernelDiff + userDiff;

                        if (totalDiff > 0)
                        {
                            double cpu = (1.0 - ((double)idleDiff / totalDiff)) * 100.0;
                            _prevIdleTime = currentIdle;
                            _prevKernelTime = currentKernel;
                            _prevUserTime = currentUser;
                            return Math.Max(0, Math.Min(100, Math.Round(cpu, 0)));
                        }
                    }

                    _prevIdleTime = currentIdle;
                    _prevKernelTime = currentKernel;
                    _prevUserTime = currentUser;
                }
            }
            catch { }
            return 25.0; // fallback
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime, out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime, out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(this); }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] MEMORYSTATUSEX lpBuffer);
    }
}
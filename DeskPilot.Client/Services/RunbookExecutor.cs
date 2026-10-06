using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace DeskPilot.Client.Services
{
    // Lớp chuyên chịu trách nhiệm chạy các kịch bản sửa lỗi hệ thống trên máy Client
    public class RunbookExecutor
    {
        // Khởi động lại dịch vụ Windows Print Spooler (sửa lỗi kẹt lệnh in)
        public async Task<string> RestartPrintSpoolerAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var controller = new ServiceController("Spooler");

                    // 1. Dừng dịch vụ nếu đang chạy
                    if (controller.Status == ServiceControllerStatus.Running)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }

                    // 2. Bật lại dịch vụ
                    controller.Start();
                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));

                    return "Đã khởi động lại Print Spooler thành công!";
                }
                catch (Exception ex)
                {
                    return $"Lỗi khi sửa dịch vụ in: {ex.Message}";
                }
            });
        }

        // Xóa bộ đệm DNS (Flush DNS) bằng lệnh cmd/PowerShell
        public async Task<string> FlushDnsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "ipconfig",
                        Arguments = "/flushdns",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(processInfo);
                    process?.WaitForExit(5000);
                    return "Đã xóa bộ nhớ đệm DNS thành công!";
                }
                catch (Exception ex)
                {
                    return $"Lỗi khi Flush DNS: {ex.Message}";
                }
            });
        }
    }
}
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;
using DeskPilot.Shared;

namespace DeskPilot.Client.Services
{
    // Kết quả thực thi một kịch bản sửa lỗi
    public record CommandResult(bool IsSuccess, string Message);

    // Lớp DUY NHẤT chịu trách nhiệm chạy các kịch bản sửa lỗi hệ thống trên máy Client
    // (dùng chung cho lệnh từ xa của Admin và nút "Tự chẩn đoán" trên giao diện)
    public class RunbookExecutor
    {
        private const int ERROR_ACCESS_DENIED = 5;

        // Thực thi lệnh theo tên (chấp nhận alias như "FixPrint", "FlushDNS")
        public Task<CommandResult> ExecuteAsync(string commandName)
        {
            return RemoteCommands.Normalize(commandName) switch
            {
                RemoteCommands.RestartSpooler => RestartPrintSpoolerAsync(),
                RemoteCommands.FlushDns => FlushDnsAsync(),
                _ => Task.FromResult(new CommandResult(false, $"Không nhận diện được lệnh: {commandName}"))
            };
        }

        // Khởi động lại dịch vụ Windows Print Spooler (sửa lỗi kẹt lệnh in)
        public Task<CommandResult> RestartPrintSpoolerAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    using var controller = new ServiceController("Spooler");

                    // 1. Dừng dịch vụ nếu đang chạy
                    if (controller.Status != ServiceControllerStatus.Stopped)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }

                    // 2. Bật lại dịch vụ
                    controller.Start();
                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));

                    return new CommandResult(true, "Đã khởi động lại Print Spooler thành công.");
                }
                catch (InvalidOperationException ex) when (ex.InnerException is Win32Exception { NativeErrorCode: ERROR_ACCESS_DENIED })
                {
                    return new CommandResult(false, "Không đủ quyền khởi động lại Print Spooler (cần chạy DeskPilot với quyền Administrator).");
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    return new CommandResult(false, "Quá thời gian chờ dịch vụ Print Spooler phản hồi.");
                }
                catch (Exception ex)
                {
                    return new CommandResult(false, $"Lỗi khi sửa dịch vụ in: {ex.Message}");
                }
            });
        }

        // Xóa bộ đệm DNS (Flush DNS)
        public Task<CommandResult> FlushDnsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "ipconfig",
                        Arguments = "/flushdns",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(processInfo);
                    if (process == null)
                    {
                        return new CommandResult(false, "Không khởi chạy được ipconfig.");
                    }

                    if (!process.WaitForExit(5000))
                    {
                        try { process.Kill(); } catch { /* bỏ qua */ }
                        return new CommandResult(false, "Lệnh Flush DNS chạy quá thời gian.");
                    }

                    return process.ExitCode == 0
                        ? new CommandResult(true, "Đã xóa bộ nhớ đệm DNS thành công.")
                        : new CommandResult(false, $"Flush DNS thất bại (mã lỗi {process.ExitCode}).");
                }
                catch (Exception ex)
                {
                    return new CommandResult(false, $"Lỗi khi Flush DNS: {ex.Message}");
                }
            });
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Client.Services
{
    public class SignalRClientService
    {
        private HubConnection? _hubConnection;
        private readonly string _serverUrl;

        // Sự kiện thông báo trạng thái kết nối
        public event Action<string>? OnStatusChanged;

        // Sự kiện kích hoạt khi Admin gửi lệnh xuống
        public event Func<string, Task>? OnCommandReceived;

        public SignalRClientService()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _serverUrl = config["ServerSettings:HubUrl"] ?? "http://localhost:5000/supportHub";
        }

        private readonly SemaphoreSlim _connectLock = new(1, 1);

        public async Task StartConnectionAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_serverUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            // Lắng nghe lệnh từ IT Admin gửi xuống qua Hub
            _hubConnection.On<string, string>("ExecuteCommand", async (targetMachine, commandName) =>
            {
                // Kiểm tra xem lệnh có phải gửi đích danh cho máy này không
                if (targetMachine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                {
                    bool isSuccess = false;
                    string message = "";

                    try
                    {
                        // Khớp cả 2 tên "FixPrint" hoặc "RestartSpooler"
                        if (commandName.Equals("FixPrint", StringComparison.OrdinalIgnoreCase) || 
                            commandName.Equals("RestartSpooler", StringComparison.OrdinalIgnoreCase))
                        {
                            isSuccess = await RestartPrintSpoolerInternalAsync();
                            message = isSuccess ? "Đã khởi động lại Print Spooler thành công." : "Không thể khởi động lại dịch vụ in (Cần quyền Admin).";
                        }
                        else if (commandName.Equals("FlushDNS", StringComparison.OrdinalIgnoreCase))
                        {
                            isSuccess = await FlushDnsInternalAsync();
                            message = isSuccess ? "Đã xoá bộ nhớ đệm DNS thành công." : "Lỗi khi chạy lệnh Flush DNS.";
                        }
                        else
                        {
                            isSuccess = false;
                            message = $"Không nhận diện được lệnh: {commandName}";
                        }
                    }
                    catch (Exception ex)
                    {
                        isSuccess = false;
                        message = $"Lỗi ngoại lệ: {ex.Message}";
                    }

                    // Gửi báo cáo kết quả thực thi ngược về Server
                    try
                    {
                        await _hubConnection.InvokeAsync("ReportCommandResult", Environment.MachineName, commandName, isSuccess, message);
                    }
                    catch { }
                }
            });

            _hubConnection.Reconnecting += error =>
            {
                OnStatusChanged?.Invoke("Mất kết nối, đang thử lại...");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                OnStatusChanged?.Invoke("Đã kết nối lại Server");
                return Task.CompletedTask;
            };

            _hubConnection.Closed += async error =>
            {
                OnStatusChanged?.Invoke("Ngắt kết nối Server, đang kết nối lại...");
                await Task.Delay(2000);
                _ = ConnectWithRetryAsync();
            };

            // Bắt đầu kết nối với vòng lặp thử lại chạy ngầm (không chặn UI)
            _ = ConnectWithRetryAsync();
        }

        // Vòng lặp tự động kết nối lại cho đến khi Server sẵn sàng
        private async Task ConnectWithRetryAsync()
        {
            while (_hubConnection != null && _hubConnection.State != HubConnectionState.Connected)
            {
                try
                {
                    OnStatusChanged?.Invoke("Đang kết nối Server...");
                    await _hubConnection.StartAsync();
                    OnStatusChanged?.Invoke("Đã kết nối Server");
                    break;
                }
                catch
                {
                    OnStatusChanged?.Invoke("Chờ Server khởi động, thử lại sau 2s...");
                    await Task.Delay(2000);
                }
            }
        }

        // Đảm bảo kết nối đã sẵn sàng trước khi gửi dữ liệu
        public async Task<bool> EnsureConnectedAsync()
        {
            if (_hubConnection == null) return false;
            if (_hubConnection.State == HubConnectionState.Connected) return true;

            await _connectLock.WaitAsync();
            try
            {
                if (_hubConnection.State == HubConnectionState.Connected) return true;
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    OnStatusChanged?.Invoke("Đã kết nối Server");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR Client] Thử kết nối lại thất bại: {ex.Message}");
            }
            finally
            {
                _connectLock.Release();
            }

            return _hubConnection.State == HubConnectionState.Connected;
        }

        // Gửi yêu cầu hỗ trợ lên Server (có tự động kết nối lại nếu chưa kịp kết nối)
        public async Task SendTicketAsync(string description)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                await EnsureConnectedAsync();
            }

            if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Chưa kết nối được tới Server SignalR! Vui lòng kiểm tra Server đang chạy.");
            }

            try
            {
                await _hubConnection.InvokeAsync("CreateTicket",
                    Environment.MachineName,
                    Environment.UserName,
                    description);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi gửi lên Server: {ex.Message}", ex);
            }
        }

        public async Task SendHeartbeatAsync(MachineStatusDto status)
        {
            if (_hubConnection == null) return;

            // Nếu đang mất kết nối, kích hoạt kết nối lại ngầm
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                _ = EnsureConnectedAsync();
                return;
            }

            if (_hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("SendHeartbeat", status);
                }
                catch { }
            }
        }

        // --- CÁC HÀM XỬ LÝ HỆ THỐNG CỤC BỘ DÀNH CHO REMOTE ACTIONS ---

        // 1. Khởi động lại dịch vụ Print Spooler
        private static async Task<bool> RestartPrintSpoolerInternalAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Restart-Service spooler -Force\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(8000);
                    return p?.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            });
        }

        // 2. Làm mới DNS Cache
        private static async Task<bool> FlushDnsInternalAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("ipconfig", "/flushdns")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var process = Process.Start(psi);
                    process?.WaitForExit(5000);
                    return process?.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
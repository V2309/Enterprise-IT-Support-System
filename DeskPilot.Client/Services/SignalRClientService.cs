using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using DeskPilot.Shared;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Client.Services
{
    public class SignalRClientService
    {
        private HubConnection? _hubConnection;
        private readonly string _serverUrl;
        private readonly string _apiKey;
        private readonly RunbookExecutor _runbookExecutor;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private int _retryLoopRunning;

        // Sự kiện thông báo trạng thái kết nối
        public event Action<string>? OnStatusChanged;

        // Sự kiện báo cho UI sau khi đã thực thi xong một lệnh từ Admin (tên lệnh, kết quả)
        public event Action<string, CommandResult>? OnRemoteCommandExecuted;

        public SignalRClientService(RunbookExecutor runbookExecutor)
        {
            _runbookExecutor = runbookExecutor;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables("DESKPILOT_")
                .Build();

            _serverUrl = config["ServerSettings:HubUrl"] ?? "http://localhost:5000/supportHub";
            _apiKey = config["ServerSettings:ApiKey"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Debug.WriteLine("[SignalR Client] Thiếu ServerSettings:ApiKey - Server sẽ từ chối kết nối.");
            }
        }

        public Task StartConnectionAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_serverUrl, options =>
                {
                    options.Headers[HubHeaders.ApiKey] = _apiKey;
                    options.Headers[HubHeaders.MachineName] = Environment.MachineName;
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            // Lắng nghe lệnh từ IT Admin gửi xuống (Server chỉ gửi vào nhóm riêng của máy này)
            _hubConnection.On<string, string>("ExecuteCommand", async (targetMachine, commandName) =>
            {
                // Kiểm tra thêm một lớp phòng trường hợp gửi nhầm
                if (!targetMachine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return;

                var result = await _runbookExecutor.ExecuteAsync(commandName);

                OnRemoteCommandExecuted?.Invoke(commandName, result);

                // Gửi báo cáo kết quả thực thi ngược về Server
                try
                {
                    await _hubConnection.InvokeAsync("ReportCommandResult", Environment.MachineName, commandName, result.IsSuccess, result.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SignalR Client] Không gửi được kết quả lệnh: {ex.Message}");
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

            // Chỉ kích hoạt khi auto-reconnect đã bỏ cuộc => chuyển sang vòng lặp thử lại vô hạn
            _hubConnection.Closed += error =>
            {
                OnStatusChanged?.Invoke("Ngắt kết nối Server, đang kết nối lại...");
                _ = ConnectWithRetryAsync();
                return Task.CompletedTask;
            };

            // Bắt đầu kết nối với vòng lặp thử lại chạy ngầm (không chặn UI)
            _ = ConnectWithRetryAsync();
            return Task.CompletedTask;
        }

        // Vòng lặp tự động kết nối lại cho đến khi Server sẵn sàng (chỉ 1 vòng lặp chạy tại một thời điểm)
        private async Task ConnectWithRetryAsync()
        {
            if (Interlocked.Exchange(ref _retryLoopRunning, 1) == 1) return;

            try
            {
                while (_hubConnection != null && _hubConnection.State != HubConnectionState.Connected)
                {
                    OnStatusChanged?.Invoke("Đang kết nối Server...");
                    if (await TryStartAsync())
                    {
                        OnStatusChanged?.Invoke("Đã kết nối Server");
                        break;
                    }

                    OnStatusChanged?.Invoke("Chờ Server khởi động, thử lại sau 2s...");
                    await Task.Delay(2000);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _retryLoopRunning, 0);
            }
        }

        // StartAsync được bảo vệ bởi lock để tránh gọi đồng thời (HubConnection sẽ ném lỗi nếu Start 2 lần)
        private async Task<bool> TryStartAsync()
        {
            if (_hubConnection == null) return false;

            await _connectLock.WaitAsync();
            try
            {
                if (_hubConnection.State == HubConnectionState.Connected) return true;
                if (_hubConnection.State != HubConnectionState.Disconnected) return false;

                await _hubConnection.StartAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR Client] Kết nối thất bại: {ex.Message}");
                return false;
            }
            finally
            {
                _connectLock.Release();
            }
        }

        // Gửi yêu cầu hỗ trợ lên Server (có tự động kết nối lại nếu chưa kịp kết nối)
        public async Task SendTicketAsync(string description)
        {
            if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
            {
                await TryStartAsync();
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
            if (_hubConnection?.State != HubConnectionState.Connected) return;

            try
            {
                // SendAsync: fire-and-forget, không cần chờ Server trả kết quả => nhẹ hơn InvokeAsync
                await _hubConnection.SendAsync("SendHeartbeat", status);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR Client] Gửi heartbeat thất bại: {ex.Message}");
            }
        }
    }
}
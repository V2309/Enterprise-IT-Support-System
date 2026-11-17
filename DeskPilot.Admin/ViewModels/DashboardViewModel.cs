using DeskPilot.Admin.Services;
using DeskPilot.Shared;
using DeskPilot.Shared.DTO;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DeskPilot.Admin.ViewModels
{
    public class IncidentFeedItem
    {
        public string TimeStr { get; set; } = DateTime.Now.ToString("HH:mm");
        public string MachineName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Tag { get; set; } = "Hệ thống";
        public string TagBackground { get; set; } = "#eff6ff";
        public string TagForeground { get; set; } = "#2563eb";
        public string DotColor { get; set; } = "#2563eb";
        public string IconSymbol { get; set; } = "Info20";
    }

    public class MetricPoint
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string TimeLabel => Timestamp.ToString("HH:mm:ss");
        public double RamUsagePercent { get; set; }
        public double DiskUsagePercent { get; set; }
        public double CpuUsagePercent { get; set; }
        public double FreeDiskSpaceGB { get; set; }
    }

    /// <summary>
    /// ViewModel trung tâm (Core) quản lý kết nối SignalR, điều hướng Tab và khởi tạo hệ thống.
    /// Các logic theo từng module được tách thành các file partial:
    /// - DashboardViewModel.Machines.cs    (Giám sát máy trạm & thông số tài nguyên)
    /// - DashboardViewModel.Tickets.cs     (Quản lý sự cố & Live feed)
    /// - DashboardViewModel.Actions.cs     (Lệnh điều khiển từ xa & Action logs)
    /// - DashboardViewModel.Chat.cs        (Kênh trò chuyện thời gian thực)
    /// - DashboardViewModel.Devices.cs     (Quản lý thiết bị CNTT)
    /// - DashboardViewModel.Tools.cs       (Quản lý công cụ kỹ thuật)
    /// - DashboardViewModel.Maintenance.cs (Kế hoạch & lịch bảo trì định kỳ)
    /// </summary>
    public partial class DashboardViewModel : INotifyPropertyChanged
    {
        private HubConnection? _hubConnection;
        private readonly string _serverUrl;
        private readonly string _apiKey;
        private readonly AdminApiClient _apiClient;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private int _retryLoopRunning;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // ================= ĐIỀU HƯỚNG SPA & TABS =================
        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set
            {
                if (_currentView != value)
                {
                    _currentView = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _currentTab = "Dashboard";
        public string CurrentTab
        {
            get => _currentTab;
            set
            {
                if (_currentTab != value)
                {
                    _currentTab = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDashboardActive));
                    OnPropertyChanged(nameof(IsTicketsActive));
                    OnPropertyChanged(nameof(IsLogsActive));
                    OnPropertyChanged(nameof(IsChatActive));
                    OnPropertyChanged(nameof(IsDevicesActive));
                    OnPropertyChanged(nameof(IsToolsActive));
                    OnPropertyChanged(nameof(IsMaintenanceActive));
                }
            }
        }

        public bool IsDashboardActive => CurrentTab == "Dashboard";
        public bool IsTicketsActive => CurrentTab == "Tickets";
        public bool IsLogsActive => CurrentTab == "Logs";
        public bool IsChatActive => CurrentTab == "Chat";
        public bool IsDevicesActive => CurrentTab == "Devices";
        public bool IsToolsActive => CurrentTab == "Tools";
        public bool IsMaintenanceActive => CurrentTab == "Maintenance";

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterMachines();
                }
            }
        }

        private string _selectedDepartment = "Tất cả phòng ban";
        public string SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (_selectedDepartment != value)
                {
                    _selectedDepartment = value;
                    OnPropertyChanged();
                }
            }
        }

        // ================= CONSTRUCTOR =================
        public DashboardViewModel()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables("DESKPILOT_")
                .Build();

            _serverUrl = config["ServerSettings:HubUrl"] ?? "http://localhost:5000/supportHub";
            _apiKey = config["ServerSettings:ApiKey"] ?? string.Empty;
            _apiClient = new AdminApiClient(_serverUrl, _apiKey);

            Machines.CollectionChanged += (s, e) =>
            {
                FilterMachines();
                UpdateCounts();
            };
        }

        // ================= SIGNALR REAL-TIME LISTENERS =================
        public async Task StartListeningAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_serverUrl, options =>
                {
                    options.Headers[HubHeaders.ApiKey] = _apiKey;
                    options.Headers[HubHeaders.OperatorName] = Environment.UserName;
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            // 1. Nhận thông số phần cứng từ máy Client
            _hubConnection.On<MachineStatusDto>("ReceiveMachineStatus", (status) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var existingMachine = FindMachine(status.MachineName);

                    if (existingMachine != null)
                    {
                        ApplyStatus(existingMachine, status);
                    }
                    else
                    {
                        Machines.Add(status);
                    }

                    AppendMetricPoint(status);
                    UpdateCounts();
                    CheckAndAlertResourceThreshold(status);
                });
            });

            // 2. Nhận Ticket mới phát sinh
            _hubConnection.On<TicketDto>("ReceiveNewTicket", (ticket) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Tickets.Insert(0, ticket);
                    System.Media.SystemSounds.Exclamation.Play();

                    AddIncident(ticket.MachineName, ticket.UserName, ticket.Description, "Sự cố", "#ef4444", "#fdf2f8", "#db2777");

                    new ToastContentBuilder()
                        .AddText($"[TICKET MỚI] Từ {ticket.MachineName} ({ticket.UserName})")
                        .AddText(ticket.Description)
                        .Show();
                });
            });

            // 3. Nhận sự kiện Ticket được đóng
            _hubConnection.On<int>("TicketResolved", (resolvedTicketId) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var itemToRemove = Tickets.FirstOrDefault(t => t.Id == resolvedTicketId);
                    if (itemToRemove != null)
                    {
                        AddIncident(itemToRemove.MachineName, itemToRemove.UserName, $"Đã xử lý xong: {itemToRemove.Description}", "Hoàn tất", "#10b981", "#dcfce7", "#16a34a");
                        Tickets.Remove(itemToRemove);
                    }
                });
            });

            // 4. Nhận sự kiện máy Offline
            _hubConnection.On<string>("MachineWentOffline", (machineName) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var machine = Machines.FirstOrDefault(m =>
                        m.MachineName.Equals(machineName, StringComparison.OrdinalIgnoreCase));
                    if (machine != null)
                    {
                        machine.IsOnline = false;
                        UpdateCounts();
                        AddIncident(machineName, machine.UserName, "Mất kết nối (Offline)", "Mất mạng", "#ef4444", "#fee2e2", "#dc2626");
                    }
                });
            });

            // 5. Nhận kết quả thực thi lệnh từ máy trạm
            _hubConnection.On<string, string, bool, string>("ReceiveCommandFeedback", (machineName, commandName, isSuccess, message) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dotColor = isSuccess ? "#10b981" : "#ef4444";
                    var tagBg = isSuccess ? "#dcfce7" : "#fee2e2";
                    var tagFg = isSuccess ? "#16a34a" : "#dc2626";
                    var tagText = isSuccess ? "Thành công" : "Thất bại";

                    AddIncident(machineName, "", $"{commandName}: {message}", tagText, dotColor, tagBg, tagFg);

                    var statusText = isSuccess ? "THÀNH CÔNG" : "THẤT BẠI";

                    if (isSuccess)
                    {
                        _ = DeskPilot.Admin.Services.ModalDialogService.ShowSuccessAsync(
                            "Thực Thi Thành Công",
                            $"Máy trạm: {machineName}\nThao tác: {commandName}\nKết quả: {statusText}\nChi tiết: {message}");
                    }
                    else
                    {
                        _ = DeskPilot.Admin.Services.ModalDialogService.ShowWarningAsync(
                            "Kết Quả Thực Thi",
                            $"Máy trạm: {machineName}\nThao tác: {commandName}\nKết quả: {statusText}\nChi tiết: {message}");
                    }
                });
            });

            // 6. Nhận ActionLog mới
            _hubConnection.On<ActionLogDto>("ReceiveNewActionLog", (newLog) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ActionLogs.Insert(0, newLog);
                    if (ActionLogs.Count > 50)
                    {
                        ActionLogs.RemoveAt(ActionLogs.Count - 1);
                    }
                });
            });

            // 7. Nhận tin nhắn chat
            _hubConnection.On<ChatMessageDto>("ReceiveChatMessage", (chatMsg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    bool isCurrentActive = SelectedConversation != null &&
                        SelectedConversation.MachineName.Equals(chatMsg.MachineName, StringComparison.OrdinalIgnoreCase);

                    if (isCurrentActive)
                    {
                        CurrentChatMessages.Add(chatMsg);
                    }

                    var displayMsg = !string.IsNullOrWhiteSpace(chatMsg.Message)
                        ? chatMsg.Message
                        : (!string.IsNullOrEmpty(chatMsg.ImageUrl) ? "[Hình ảnh]" : string.Empty);

                    var existingConv = ChatConversations.FirstOrDefault(c =>
                        c.MachineName.Equals(chatMsg.MachineName, StringComparison.OrdinalIgnoreCase));

                    if (existingConv != null)
                    {
                        existingConv.LastMessage = displayMsg;
                        existingConv.LastMessageAt = chatMsg.SentAt;
                        if (!chatMsg.IsFromAdmin) existingConv.UserName = chatMsg.SenderName;

                        if (!chatMsg.IsFromAdmin && !isCurrentActive)
                        {
                            existingConv.UnreadCount++;
                        }
                        else if (isCurrentActive)
                        {
                            existingConv.UnreadCount = 0;
                        }

                        ChatConversations.Remove(existingConv);
                        ChatConversations.Insert(0, existingConv);
                    }
                    else
                    {
                        var isOnline = FindMachine(chatMsg.MachineName)?.IsOnline ?? false;
                        var newConv = new ChatConversationDto
                        {
                            MachineName = chatMsg.MachineName,
                            UserName = !chatMsg.IsFromAdmin ? chatMsg.SenderName : "",
                            LastMessage = displayMsg,
                            LastMessageAt = chatMsg.SentAt,
                            IsOnline = isOnline,
                            UnreadCount = (!chatMsg.IsFromAdmin && !isCurrentActive) ? 1 : 0
                        };
                        ChatConversations.Insert(0, newConv);
                    }

                    if (!chatMsg.IsFromAdmin)
                    {
                        System.Media.SystemSounds.Asterisk.Play();
                        AddIncident(chatMsg.MachineName, chatMsg.SenderName, $"Chat: {displayMsg}", "Tin nhắn", "#3b82f6", "#eff6ff", "#2563eb");
                        new ToastContentBuilder()
                            .AddText($"[CHAT] Từ {chatMsg.MachineName} ({chatMsg.SenderName})")
                            .AddText(displayMsg)
                            .Show();
                    }
                });
            });

            _hubConnection.Reconnecting += error =>
            {
                System.Diagnostics.Debug.WriteLine("[Admin SignalR] Mất kết nối, đang thử lại...");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += async connectionId =>
            {
                System.Diagnostics.Debug.WriteLine("[Admin SignalR] Đã kết nối lại Server!");
                await FetchInitialDataAsync();
            };

            _hubConnection.Closed += error =>
            {
                System.Diagnostics.Debug.WriteLine("[Admin SignalR] Kết nối đóng, đang chờ thử lại...");
                _ = ConnectWithRetryAsync();
                return Task.CompletedTask;
            };

            _ = ConnectWithRetryAsync();
            await Task.CompletedTask;
        }

        private async Task ConnectWithRetryAsync()
        {
            if (Interlocked.Exchange(ref _retryLoopRunning, 1) == 1) return;

            try
            {
                while (_hubConnection != null && _hubConnection.State != HubConnectionState.Connected)
                {
                    if (await TryStartAsync())
                    {
                        await FetchInitialDataAsync();
                        break;
                    }
                    await Task.Delay(2000);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _retryLoopRunning, 0);
            }
        }

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
                System.Diagnostics.Debug.WriteLine($"[Admin SignalR] Kết nối thất bại: {ex.Message}");
                return false;
            }
            finally
            {
                _connectLock.Release();
            }
        }

        private async Task FetchInitialDataAsync()
        {
            try
            {
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    var machines = await _hubConnection.InvokeAsync<List<MachineStatusDto>>("GetMachines");
                    var oldTickets = await _hubConnection.InvokeAsync<List<TicketDto>>("GetActiveTickets");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var m in machines)
                        {
                            var existing = FindMachine(m.MachineName);
                            if (existing != null) ApplyStatus(existing, m);
                            else Machines.Add(m);
                        }
                        UpdateCounts();

                        Tickets.Clear();
                        foreach (var ticket in oldTickets)
                        {
                            Tickets.Add(ticket);
                        }
                    });

                    await LoadActionLogsAsync();
                    await LoadChatConversationsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR Error] Lỗi kết nối hoặc tải dữ liệu: {ex.Message}");
            }
        }
    }
}

using DeskPilot.Shared;
using DeskPilot.Shared.DTO;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public class DashboardViewModel : INotifyPropertyChanged
    {
        private HubConnection? _hubConnection;
        private const string ServerUrl = "http://localhost:5000/supportHub";
        private readonly ConcurrentDictionary<string, DateTime> _lastAlertTimes = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Điều hướng SPA (Website-like): Lưu view hiện tại đang hiển thị
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
                }
            }
        }

        public bool IsDashboardActive => CurrentTab == "Dashboard";
        public bool IsTicketsActive => CurrentTab == "Tickets";
        public bool IsLogsActive => CurrentTab == "Logs";

        // Tìm kiếm toàn cục từ Header
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

        // Lọc theo phòng ban từ Sidebar
        private string _selectedDepartment = "Kế toán (12)";
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

        public ObservableCollection<MachineStatusDto> Machines { get; set; } = new();
        public ObservableCollection<MachineStatusDto> FilteredMachines { get; set; } = new();
        public ObservableCollection<TicketDto> Tickets { get; set; } = new();
        public ObservableCollection<ActionLogDto> ActionLogs { get; set; } = new();
        public ObservableCollection<IncidentFeedItem> LiveIncidents { get; set; } = new();

        public int OnlineCount => Machines.Count(m => m.IsOnline);
        public int OfflineCount => Machines.Count(m => !m.IsOnline);
        public int TotalMachinesCount => Machines.Count;

        public DashboardViewModel()
        {
            Machines.CollectionChanged += (s, e) =>
            {
                FilterMachines();
                UpdateCounts();
            };

            // Dữ liệu mẫu ban đầu cho Live Incident Feed (giống screenshot của bạn)
            SeedInitialIncidents();
        }

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

        private void SeedInitialIncidents()
        {
            LiveIncidents.Add(new IncidentFeedItem
            {
                TimeStr = DateTime.Now.AddMinutes(-5).ToString("HH:mm"),
                MachineName = "PC-KETOAN-05",
                UserName = "Lý Thị K",
                Title = "Máy in bị kẹt (Printer Spooler Error)",
                Tag = "Máy in",
                TagBackground = "#fdf2f8",
                TagForeground = "#db2777",
                DotColor = "#ef4444",
                IconSymbol = "Warning20"
            });
            LiveIncidents.Add(new IncidentFeedItem
            {
                TimeStr = DateTime.Now.AddMinutes(-12).ToString("HH:mm"),
                MachineName = "PC-KETOAN-08",
                UserName = "Nguyễn Thị H",
                Title = "Máy chậm, CPU cao (92%)",
                Tag = "Hiệu suất",
                TagBackground = "#fffbeb",
                TagForeground = "#d97706",
                DotColor = "#f59e0b",
                IconSymbol = "Warning20"
            });
            LiveIncidents.Add(new IncidentFeedItem
            {
                TimeStr = DateTime.Now.AddMinutes(-25).ToString("HH:mm"),
                MachineName = "PC-KETOAN-03",
                UserName = "Lê Văn C",
                Title = "Đã khởi chạy Runbook: Flush DNS",
                Tag = "Tự động",
                TagBackground = "#eff6ff",
                TagForeground = "#2563eb",
                DotColor = "#3b82f6",
                IconSymbol = "Info20"
            });
            LiveIncidents.Add(new IncidentFeedItem
            {
                TimeStr = DateTime.Now.AddMinutes(-40).ToString("HH:mm"),
                MachineName = "PC-KETOAN-05",
                UserName = "Hoàng Văn E",
                Title = "Mất mạng",
                Tag = "Kết nối",
                TagBackground = "#fee2e2",
                TagForeground = "#dc2626",
                DotColor = "#ef4444",
                IconSymbol = "Dismiss20"
            });
            LiveIncidents.Add(new IncidentFeedItem
            {
                TimeStr = DateTime.Now.AddMinutes(-55).ToString("HH:mm"),
                MachineName = "PC-KETOAN-02",
                UserName = "Trần Thị B",
                Title = "Đã sửa lỗi kẹt máy in thành công",
                Tag = "Hoàn tất",
                TagBackground = "#dcfce7",
                TagForeground = "#16a34a",
                DotColor = "#10b981",
                IconSymbol = "Checkmark20"
            });
        }

        public void AddIncident(string machine, string user, string title, string tag, string dotColor, string tagBg, string tagFg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LiveIncidents.Insert(0, new IncidentFeedItem
                {
                    TimeStr = DateTime.Now.ToString("HH:mm"),
                    MachineName = machine,
                    UserName = user,
                    Title = title,
                    Tag = tag,
                    TagBackground = tagBg,
                    TagForeground = tagFg,
                    DotColor = dotColor
                });

                if (LiveIncidents.Count > 30)
                {
                    LiveIncidents.RemoveAt(LiveIncidents.Count - 1);
                }
            });
        }

        public async Task StartListeningAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(ServerUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            // 1. Lắng nghe cập nhật phần cứng từ máy Client
            _hubConnection.On<MachineStatusDto>("ReceiveMachineStatus", (status) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var existingMachine = Machines.FirstOrDefault(m =>
                        m.MachineName.Equals(status.MachineName, StringComparison.OrdinalIgnoreCase));

                    if (existingMachine != null)
                    {
                        existingMachine.RamUsagePercent = status.RamUsagePercent;
                        existingMachine.CpuUsagePercent = status.CpuUsagePercent;
                        existingMachine.FreeDiskSpaceGB = status.FreeDiskSpaceGB;
                        existingMachine.TotalDiskSpaceGB = status.TotalDiskSpaceGB;
                        existingMachine.TotalRamGB = status.TotalRamGB;
                        existingMachine.UsedRamGB = status.UsedRamGB;
                        existingMachine.IsOnline = true;
                        existingMachine.LastSeen = status.LastSeen;
                        existingMachine.SpoolerStatus = status.SpoolerStatus;
                    }
                    else
                    {
                        Machines.Add(status);
                    }

                    UpdateCounts();
                    CheckAndAlertResourceThreshold(status);
                });
            });

            // 2. Lắng nghe Ticket mới phát sinh theo thời gian thực
            _hubConnection.On<TicketDto>("ReceiveNewTicket", (ticket) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Tickets.Insert(0, ticket);
                    System.Media.SystemSounds.Exclamation.Play();

                    // Thêm vào dòng thời gian Live Feed
                    AddIncident(ticket.MachineName, ticket.UserName, ticket.Description, "Sự cố", "#ef4444", "#fdf2f8", "#db2777");

                    // Bắn thông báo Windows
                    new ToastContentBuilder()
                        .AddText($"[TICKET MỚI] Từ {ticket.MachineName} ({ticket.UserName})")
                        .AddText(ticket.Description)
                        .Show();
                });
            });

            // Lắng nghe khi có Ticket được đóng
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

            // Lắng nghe máy Offline
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

            // Lắng nghe kết quả thực thi lệnh gửi về từ máy Client
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

            // Lắng nghe bản ghi ActionLog mới theo thời gian thực (khi thực thi lệnh hoặc đóng ticket)
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

            _hubConnection.Closed += async error =>
            {
                System.Diagnostics.Debug.WriteLine("[Admin SignalR] Kết nối đóng, đang chờ thử lại...");
                await Task.Delay(2000);
                _ = ConnectWithRetryAsync();
            };

            // Bắt đầu vòng lặp thử kết nối chạy ngầm cho đến khi Server sẵn sàng
            _ = ConnectWithRetryAsync();
        }

        private async Task ConnectWithRetryAsync()
        {
            while (_hubConnection != null && _hubConnection.State != HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    await FetchInitialDataAsync();
                    break;
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }

        private async Task FetchInitialDataAsync()
        {
            try
            {
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    var oldTickets = await _hubConnection.InvokeAsync<List<TicketDto>>("GetActiveTickets");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Tickets.Clear();
                        foreach (var ticket in oldTickets)
                        {
                            Tickets.Add(ticket);
                        }
                    });

                    await LoadActionLogsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR Error] Lỗi kết nối hoặc tải dữ liệu: {ex.Message}");
            }
        }

        // Phát lệnh từ xa xuống máy chỉ định
        public async Task SendCommandAsync(string targetMachine, string commandName)
        {
            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("SendCommandToClient", targetMachine, commandName);
                AddIncident(targetMachine, "", $"Đã gửi lệnh: {commandName}", "Đang chạy", "#3b82f6", "#eff6ff", "#2563eb");

                await Task.Delay(500);
                await LoadActionLogsAsync();
            }
            else
            {
                _ = DeskPilot.Admin.Services.ModalDialogService.ShowWarningAsync(
                    "Thông Báo Kết Nối",
                    "Chưa kết nối được với Server! Đang tự động kết nối lại...");
                _ = ConnectWithRetryAsync();
            }
        }

        // IT bấm hoàn tất Ticket
        public async Task ResolveTicketAsync(int ticketId)
        {
            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("ResolveTicket", ticketId);
                }
                catch (Exception ex)
                {
                    _ = DeskPilot.Admin.Services.ModalDialogService.ShowErrorAsync(
                        "Lỗi Đóng Phiếu",
                        $"Không thể cập nhật trạng thái phiếu: {ex.Message}");
                }
            }
        }

        // Tải lịch sử 20 lệnh can thiệp gần nhất từ PostgreSQL
        public async Task LoadActionLogsAsync()
        {
            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    var logs = await _hubConnection.InvokeAsync<List<ActionLogDto>>("GetRecentActionLogs");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ActionLogs.Clear();
                        foreach (var log in logs)
                        {
                            ActionLogs.Add(log);
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi tải ActionLogs: {ex.Message}");
                }
            }
        }

        // Kiểm tra ngưỡng nguy hiểm và bắn Toast cảnh báo
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
                .AddAppLogoOverride(
                    new Uri("https://cdn-icons-png.flaticon.com/512/564/564619.png"),
                    ToastGenericAppLogoCrop.Circle)
                .Show();
        }
    }
}
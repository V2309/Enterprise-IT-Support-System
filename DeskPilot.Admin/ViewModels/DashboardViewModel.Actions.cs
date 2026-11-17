using DeskPilot.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace DeskPilot.Admin.ViewModels
{
    public partial class DashboardViewModel
    {
        public ObservableCollection<ActionLogDto> ActionLogs { get; set; } = new();

        // Phát lệnh từ xa xuống máy chỉ định
        public async Task SendCommandAsync(string targetMachine, string commandName)
        {
            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("SendCommandToClient", targetMachine, commandName);
                    AddIncident(targetMachine, "", $"Đã gửi lệnh: {commandName}", "Đang chạy", "#3b82f6", "#eff6ff", "#2563eb");
                }
                catch (Exception ex)
                {
                    _ = DeskPilot.Admin.Services.ModalDialogService.ShowWarningAsync(
                        "Không Thể Gửi Lệnh",
                        CleanHubError(ex));
                }
            }
            else
            {
                _ = DeskPilot.Admin.Services.ModalDialogService.ShowWarningAsync(
                    "Thông Báo Kết Nối",
                    "Chưa kết nối được với Server! Đang tự động kết nối lại...");
                _ = ConnectWithRetryAsync();
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

        private static string CleanHubError(Exception ex)
        {
            const string marker = "HubException: ";
            var idx = ex.Message.IndexOf(marker, StringComparison.Ordinal);
            return idx >= 0 ? ex.Message[(idx + marker.Length)..] : ex.Message;
        }
    }
}

using DeskPilot.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace DeskPilot.Admin.ViewModels
{
    public partial class DashboardViewModel
    {
        public ObservableCollection<TicketDto> Tickets { get; set; } = new();
        public ObservableCollection<IncidentFeedItem> LiveIncidents { get; set; } = new();

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
                        $"Không thể cập nhật trạng thái phiếu: {CleanHubError(ex)}");
                }
            }
        }
    }
}

using DeskPilot.Shared.DTO;
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
        public ObservableCollection<ChatConversationDto> ChatConversations { get; set; } = new();
        public ObservableCollection<ChatMessageDto> CurrentChatMessages { get; set; } = new();

        private ChatConversationDto? _selectedConversation;
        public ChatConversationDto? SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                if (_selectedConversation != value)
                {
                    _selectedConversation = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedConversation));
                    if (value != null)
                    {
                        value.UnreadCount = 0;
                        _ = LoadChatHistoryAsync(value.MachineName);
                    }
                }
            }
        }

        public bool HasSelectedConversation => SelectedConversation != null;

        public async Task LoadChatConversationsAsync()
        {
            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    var convs = await _hubConnection.InvokeAsync<List<ChatConversationDto>>("GetChatConversations");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ChatConversations.Clear();
                        foreach (var c in convs)
                        {
                            ChatConversations.Add(c);
                        }

                        if (SelectedConversation == null && ChatConversations.Count > 0)
                        {
                            SelectedConversation = ChatConversations[0];
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Chat Error] Tải danh sách hội thoại: {ex.Message}");
                }
            }
        }

        public async Task LoadChatHistoryAsync(string machineName)
        {
            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    var history = await _hubConnection.InvokeAsync<List<ChatMessageDto>>("GetChatHistory", machineName, 50);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentChatMessages.Clear();
                        foreach (var m in history)
                        {
                            CurrentChatMessages.Add(m);
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Chat Error] Tải lịch sử chat {machineName}: {ex.Message}");
                }
            }
        }

        public async Task<string?> UploadChatImageStreamAsync(System.IO.Stream stream, string fileName)
        {
            var (success, error, url) = await _apiClient.UploadChatImageStreamAsync(stream, fileName);
            if (!success || string.IsNullOrEmpty(url))
            {
                await DeskPilot.Admin.Services.ModalDialogService.ShowErrorAsync(
                    "Lỗi Tải Ảnh",
                    $"Không thể tải ảnh lên ImageKit: {error ?? "Không xác định"}");
                return null;
            }
            return url;
        }

        public async Task<string?> UploadChatImageAsync(string filePath)
        {
            var (success, error, url) = await _apiClient.UploadChatImageAsync(filePath);
            if (!success || string.IsNullOrEmpty(url))
            {
                await DeskPilot.Admin.Services.ModalDialogService.ShowErrorAsync(
                    "Lỗi Tải Ảnh",
                    $"Không thể tải ảnh lên ImageKit: {error ?? "Không xác định"}");
                return null;
            }
            return url;
        }

        public async Task SendChatMessageAsync(string message, string? imageUrl = null)
        {
            if (SelectedConversation == null) return;

            message = message.Trim();
            imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

            if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(imageUrl)) return;

            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("ReplyChatMessage", SelectedConversation.MachineName, message, imageUrl);
                }
                catch (Exception ex)
                {
                    _ = DeskPilot.Admin.Services.ModalDialogService.ShowErrorAsync(
                        "Lỗi Gửi Tin Nhắn",
                        $"Không thể gửi tin nhắn tới {SelectedConversation.MachineName}: {ex.Message}");
                }
            }
            else
            {
                _ = DeskPilot.Admin.Services.ModalDialogService.ShowWarningAsync(
                    "Mất Kết Nối",
                    "Chưa kết nối được với Server. Vui lòng thử lại sau.");
            }
        }
    }
}

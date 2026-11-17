using DeskPilot.Admin.ViewModels;
using DeskPilot.Shared.DTO;
using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DeskPilot.Admin.Views
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (ViewModel != null)
                {
                    ViewModel.CurrentChatMessages.CollectionChanged += CurrentChatMessages_CollectionChanged;
                    _ = ViewModel.LoadChatConversationsAsync();
                }
            };

            Unloaded += (s, e) =>
            {
                if (ViewModel != null)
                {
                    ViewModel.CurrentChatMessages.CollectionChanged -= CurrentChatMessages_CollectionChanged;
                }
            };
        }

        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

        private void CurrentChatMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                AdminChatScrollViewer?.ScrollToEnd();
            });
        }

        // Tải lại danh sách cuộc trò chuyện
        private async void BtnRefreshConversations_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ViewModel.LoadChatConversationsAsync();
            }
        }

        // Lọc danh sách hội thoại theo từ khóa tìm kiếm
        private void TxtSearchConv_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = TxtSearchConv.Text.Trim().ToLowerInvariant();
            TxtSearchPlaceholder.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;

            if (ViewModel == null) return;

            // Nếu ô tìm kiếm rỗng thì hiển thị tất cả
            if (string.IsNullOrEmpty(query))
            {
                ConversationsItemsControl.ItemsSource = ViewModel.ChatConversations;
            }
            else
            {
                ConversationsItemsControl.ItemsSource = ViewModel.ChatConversations
                    .Where(c => c.MachineName.ToLowerInvariant().Contains(query) ||
                                c.UserName.ToLowerInvariant().Contains(query) ||
                                c.LastMessage.ToLowerInvariant().Contains(query))
                    .ToList();
            }
        }

        // Khi IT bấm vào một máy trạm trong danh sách bên trái
        private void ConversationItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string machineName && ViewModel != null)
            {
                var conv = ViewModel.ChatConversations.FirstOrDefault(c =>
                    c.MachineName.Equals(machineName, StringComparison.OrdinalIgnoreCase));

                if (conv != null)
                {
                    ViewModel.SelectedConversation = conv;
                }
            }
        }

        // Placeholder của ô nhập tin nhắn Admin
        private void TxtAdminChatInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtAdminChatPlaceholder != null)
            {
                TxtAdminChatPlaceholder.Visibility = string.IsNullOrEmpty(TxtAdminChatInput.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        // Nhấn Enter để gửi (Shift+Enter để xuống dòng)
        private void TxtAdminChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                SendAdminMessage();
            }
        }

        private string? _pendingChatImageFilePath;
        private BitmapSource? _pendingChatBitmapSource;

        private void TxtAdminChatInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ViewModel?.SelectedConversation == null) return;

                // 1. Dán ảnh trực tiếp từ Clipboard
                if (Clipboard.ContainsImage())
                {
                    e.Handled = true;
                    var bitmap = Clipboard.GetImage();
                    if (bitmap != null)
                    {
                        StageAdminBitmapAttachment(bitmap);
                    }
                    return;
                }

                // 2. Dán file ảnh vừa copy từ File Explorer
                if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    foreach (string? file in files)
                    {
                        if (string.IsNullOrEmpty(file)) continue;
                        var ext = System.IO.Path.GetExtension(file)?.ToLowerInvariant();
                        if (ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp")
                        {
                            e.Handled = true;
                            StageAdminImageAttachment(file);
                            return;
                        }
                    }
                }
            }
        }

        private void StageAdminImageAttachment(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;
            _pendingChatImageFilePath = filePath;
            _pendingChatBitmapSource = null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                ImgAdminAttachmentThumbnail.Source = bitmap;
                TxtAdminAttachmentName.Text = System.IO.Path.GetFileName(filePath);
                AdminAttachmentPreviewPanel.Visibility = Visibility.Visible;
                TxtAdminChatInput.Focus();
            }
            catch { }
        }

        private void StageAdminBitmapAttachment(BitmapSource bitmapSource)
        {
            _pendingChatBitmapSource = bitmapSource;
            _pendingChatImageFilePath = null;

            ImgAdminAttachmentThumbnail.Source = bitmapSource;
            TxtAdminAttachmentName.Text = $"Ảnh chụp ({DateTime.Now:HH:mm:ss})";
            AdminAttachmentPreviewPanel.Visibility = Visibility.Visible;
            TxtAdminChatInput.Focus();
        }

        private void BtnRemoveAdminAttachment_Click(object sender, RoutedEventArgs e)
        {
            ClearAdminPendingAttachment();
        }

        private void ClearAdminPendingAttachment()
        {
            _pendingChatImageFilePath = null;
            _pendingChatBitmapSource = null;
            ImgAdminAttachmentThumbnail.Source = null;
            AdminAttachmentPreviewPanel.Visibility = Visibility.Collapsed;
        }

        private async void BtnAdminAttachImage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.SelectedConversation == null)
            {
                await Services.ModalDialogService.ShowWarningAsync(
                    "Chưa Chọn Máy",
                    "Vui lòng chọn một cuộc trò chuyện máy trạm trước khi gửi ảnh.");
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn hình ảnh gửi nhân viên",
                Filter = "Hình ảnh (*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp"
            };

            if (openFileDialog.ShowDialog() != true) return;
            StageAdminImageAttachment(openFileDialog.FileName);
        }

        // Bấm vào ảnh để mở ảnh to ở giữa màn hình (Lightbox modal)
        private void ChatImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    var preview = new Chat.ImagePreviewWindow(url)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    preview.ShowDialog();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Admin Chat] Không thể mở ảnh {url}: {ex.Message}");
                }
            }
        }

        private void BtnAdminSend_Click(object sender, RoutedEventArgs e)
        {
            SendAdminMessage();
        }

        private async void SendAdminMessage()
        {
            string text = TxtAdminChatInput.Text.Trim();
            bool hasPendingImage = _pendingChatBitmapSource != null || !string.IsNullOrEmpty(_pendingChatImageFilePath);

            if (string.IsNullOrEmpty(text) && !hasPendingImage) return;
            if (ViewModel?.SelectedConversation == null) return;

            TxtAdminChatInput.Clear();
            var pendingBitmap = _pendingChatBitmapSource;
            var pendingFilePath = _pendingChatImageFilePath;
            ClearAdminPendingAttachment();

            BtnAdminAttachImage.IsEnabled = false;
            BtnAdminSend.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                string? imageUrl = null;
                if (pendingBitmap != null)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(pendingBitmap));
                    using var stream = new System.IO.MemoryStream();
                    encoder.Save(stream);
                    stream.Position = 0;

                    string fileName = $"clipboard_admin_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
                    imageUrl = await ViewModel.UploadChatImageStreamAsync(stream, fileName);
                }
                else if (!string.IsNullOrEmpty(pendingFilePath))
                {
                    imageUrl = await ViewModel.UploadChatImageAsync(pendingFilePath);
                }

                await ViewModel.SendChatMessageAsync(text, imageUrl);
            }
            catch (Exception ex)
            {
                await Services.ModalDialogService.ShowErrorAsync(
                    "Lỗi Gửi Tin Nhắn",
                    $"Không thể gửi tin nhắn/hình ảnh: {ex.Message}");
            }
            finally
            {
                BtnAdminAttachImage.IsEnabled = true;
                BtnAdminSend.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        // Nút tắt: Khởi động Print Spooler ngay trong phiên chat
        private async void BtnQuickSpooler_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.SelectedConversation == null) return;

            string target = ViewModel.SelectedConversation.MachineName;
            bool confirm = await Services.ModalDialogService.ShowConfirmAsync(
                "Khởi Động Lại Spooler",
                $"Bạn có chắc chắn muốn gửi lệnh khởi động Print Spooler tới {target}?");

            if (confirm)
            {
                await ViewModel.SendCommandAsync(target, DeskPilot.Shared.RemoteCommands.RestartSpooler);
            }
        }

        // Nút tắt: Flush DNS ngay trong phiên chat
        private async void BtnQuickFlushDns_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.SelectedConversation == null) return;

            string target = ViewModel.SelectedConversation.MachineName;
            bool confirm = await Services.ModalDialogService.ShowConfirmAsync(
                "Xóa Bộ Nhớ Đệm DNS",
                $"Bạn có chắc chắn muốn gửi lệnh Flush DNS tới {target}?");

            if (confirm)
            {
                await ViewModel.SendCommandAsync(target, DeskPilot.Shared.RemoteCommands.FlushDns);
            }
        }
    }
}

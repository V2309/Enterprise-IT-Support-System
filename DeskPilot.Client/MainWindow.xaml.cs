using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DeskPilot.Client.Services;
using DeskPilot.Shared.DTO;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;

namespace DeskPilot.Client
{
    public partial class MainWindow : Window
    {
        private readonly HardwareMonitorService _monitorService = new();
        private readonly RunbookExecutor _runbookExecutor = new();
        private readonly SignalRClientService _signalRService;
        private readonly DispatcherTimer _timer = new();
        private bool _isExplicitExit = false;

        public MainWindow()
        {
            InitializeComponent();
            PositionCenterScreen();

            _signalRService = new SignalRClientService(_runbookExecutor);

            // Hiển thị thông báo (không chặn UI) khi IT gửi lệnh khắc phục từ xa
            _signalRService.OnRemoteCommandExecuted += OnRemoteCommandExecuted;
            _signalRService.OnChatMessageReceived += OnChatMessageReceived;

            _signalRService.OnStatusChanged += (status) =>
            {
                if (status == "Đã kết nối Server")
                {
                    Dispatcher.InvokeAsync(async () =>
                    {
                        if (ChatOverlayView.Visibility == Visibility.Visible && ClientChatMessagesStack.Children.Count == 0)
                        {
                            await LoadChatHistoryAsync();
                        }
                    });
                }
            };

            Loaded += async (s, e) =>
            {
                await _signalRService.StartConnectionAsync();
            };

            UpdateHardwareInfo();

            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += async (s, e) => await TickHandlerAsync();
            _timer.Start();
        }

        // Cho phép người dùng kéo thả di chuyển cửa sổ bất kỳ đâu
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Nút Cài đặt
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("DeskPilot Client v1.0\nHệ thống hỗ trợ kỹ thuật và tự chẩn đoán máy trạm.", 
                            "Cài đặt & Giới thiệu", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Thông báo kết quả lệnh sửa lỗi được IT gửi xuống từ xa qua SignalR
        private void OnRemoteCommandExecuted(string commandName, CommandResult result)
        {
            Dispatcher.BeginInvoke(() =>
            {
                MyNotifyIcon.ShowBalloonTip(
                    "IT Support vừa chạy lệnh khắc phục",
                    result.Message,
                    result.IsSuccess ? BalloonIcon.Info : BalloonIcon.Warning);
            });
        }

        // 1-Click: Báo lỗi Kẹt máy in
        private async void BtnReportPrinter_Click(object sender, RoutedEventArgs e)
        {
            await SendQuickTicketAsync("Kẹt máy in", "Lỗi máy in, không in được tài liệu");
        }

        // 1-Click: Báo lỗi Mất mạng
        private async void BtnReportNetwork_Click(object sender, RoutedEventArgs e)
        {
            await SendQuickTicketAsync("Mất mạng", "Không có kết nối Internet / mạng nội bộ");
        }

        // 1-Click: Báo lỗi Máy chậm
        private async void BtnReportSlow_Click(object sender, RoutedEventArgs e)
        {
            await SendQuickTicketAsync("Máy chậm", "Hiệu suất máy tính thấp, ứng dụng bị đơ giật");
        }

        private async Task SendQuickTicketAsync(string title, string description)
        {
            try
            {
                await _signalRService.SendTicketAsync($"[{title}] {description}");
                MessageBox.Show($"Đã gửi thông báo lỗi '{title}' tới IT thành công!\nKỹ thuật viên sẽ hỗ trợ bạn sớm nhất.",
                                "Gửi Yêu Cầu Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể gửi phiếu hỗ trợ: {ex.Message}", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Gửi Ticket từ ô nhập tùy chỉnh
        private async void BtnSendTicket_Click(object sender, RoutedEventArgs e)
        {
            var issueText = TxtTicketIssue.Text.Trim();
            if (string.IsNullOrEmpty(issueText))
            {
                MessageBox.Show("Vui lòng nhập mô tả sự cố trước khi gửi!", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _signalRService.SendTicketAsync(issueText);
                TxtTicketIssue.Clear();
                MessageBox.Show("Đã gửi yêu cầu hỗ trợ đến IT thành công!", "Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể gửi phiếu: {ex.Message}", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Hiển thị/ẩn placeholder khi gõ văn bản
        private void TxtTicketIssue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtTicketPlaceholder != null)
            {
                TxtTicketPlaceholder.Visibility = string.IsNullOrEmpty(TxtTicketIssue.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        // Nhấn phím Enter trong ô input để gửi ngay
        private void TxtTicketIssue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSendTicket_Click(sender, e);
            }
        }

        // Nút: Tự chẩn đoán & Sửa nhanh
        private async void BtnAutoFix_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null) button.IsEnabled = false;

            try
            {
                var printTask = _runbookExecutor.RestartPrintSpoolerAsync();
                var dnsTask = _runbookExecutor.FlushDnsAsync();
                var results = await Task.WhenAll(printTask, dnsTask);

                static string Line(CommandResult r) => $"{(r.IsSuccess ? "✓" : "✗")} {r.Message}";
                bool allOk = Array.TrueForAll(results, r => r.IsSuccess);

                MessageBox.Show("Kết quả tự động kiểm tra và chạy Runbook sửa lỗi:\n" +
                                Line(results[0]) + "\n" +
                                Line(results[1]),
                                "Tự Chẩn Đoán & Sửa Nhanh", MessageBoxButton.OK,
                                allOk ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi trong quá trình tự sửa: {ex.Message}", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        private async Task TickHandlerAsync()
        {
            var status = _monitorService.GetCurrentStatus();
            UpdateUi(status);
            await _signalRService.SendHeartbeatAsync(status);
        }

        private void UpdateHardwareInfo()
        {
            var status = _monitorService.GetCurrentStatus();
            UpdateUi(status);
        }

        // Cập nhật các chỉ số đồ họa vòng tròn và chữ
        private void UpdateUi(MachineStatusDto status)
        {
            TxtMachineName.Text = status.MachineName;
            TxtUserName.Text = status.UserName;

            // 1. RAM
            ArcRam.Data = CreateArcGeometry(status.RamUsagePercent);
            TxtRamPercent.Text = $"{Math.Round(status.RamUsagePercent)}%";
            TxtRamDetails.Text = $"{status.UsedRamGB:0.0} / {status.TotalRamGB:0.0} GB";

            // 2. CPU
            ArcCpu.Data = CreateArcGeometry(status.CpuUsagePercent);
            TxtCpuPercent.Text = $"{Math.Round(status.CpuUsagePercent)}%";
            TxtCpuDetails.Text = $"{Math.Round(status.CpuUsagePercent)}% / 100%";

            // 3. Ổ C
            ArcDisk.Data = CreateArcGeometry(status.DiskUsagePercent);
            TxtDiskPercent.Text = $"{Math.Round(status.DiskUsagePercent)}%";
            double usedDisk = Math.Max(0, status.TotalDiskSpaceGB - status.FreeDiskSpaceGB);
            TxtDiskDetails.Text = $"{usedDisk:0} / {status.TotalDiskSpaceGB:0} GB";

            // Tổng quan sức khỏe
            if (status.RamUsagePercent >= 90 || status.FreeDiskSpaceGB < 10 || status.CpuUsagePercent >= 95)
            {
                TxtOverallStatus.Text = "● Cần kiểm tra";
                TxtOverallStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
            else
            {
                TxtOverallStatus.Text = "● Hoạt động tốt";
                TxtOverallStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
        }

        // Vẽ cung tròn phần trăm (Circular Gauge)
        private static Geometry CreateArcGeometry(double percentage, double radius = 28, double strokeThickness = 7)
        {
            percentage = Math.Max(0.5, Math.Min(99.5, percentage));
            double angle = (percentage / 100.0) * 360.0;
            double startAngle = -90; // Bắt đầu ở vị trí 12 giờ
            double endAngle = startAngle + angle;

            double center = 35.0; // 70px / 2
            Point startPoint = PointOnCircle(center, center, radius, startAngle);
            Point endPoint = PointOnCircle(center, center, radius, endAngle);
            bool isLargeArc = angle > 180.0;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(startPoint, false, false);
                ctx.ArcTo(endPoint, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true, false);
            }
            geometry.Freeze();
            return geometry;
        }

        private static Point PointOnCircle(double cx, double cy, double r, double angleDegrees)
        {
            double rad = angleDegrees * Math.PI / 180.0;
            return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }

        private void PositionCenterScreen()
        {
            var workingArea = SystemParameters.WorkArea;
            Left = workingArea.Left + (workingArea.Width - Width) / 2;
            Top = workingArea.Top + (workingArea.Height - Height) / 2;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Ẩn xuống System Tray
            this.Hide();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void MyNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowAppWindow();
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            ShowAppWindow();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            _isExplicitExit = true;
            MyNotifyIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void ShowAppWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        #region CHAT WITH IT SUPPORT LOGIC

        private bool _isChatHistoryLoaded = false;
        private readonly System.Collections.Generic.HashSet<int> _renderedChatIds = new();

        private async void BtnToggleChat_Click(object sender, RoutedEventArgs e)
        {
            if (ChatOverlayView.Visibility == Visibility.Visible)
            {
                CloseChatView();
            }
            else
            {
                await OpenChatViewAsync();
            }
        }

        private void BtnBackToDashboard_Click(object sender, RoutedEventArgs e)
        {
            CloseChatView();
        }

        private async Task OpenChatViewAsync()
        {
            MainDashboardView.Visibility = Visibility.Collapsed;
            ChatOverlayView.Visibility = Visibility.Visible;
            BadgeNewMessage.Visibility = Visibility.Collapsed;
            TxtClientChatInput.Focus();

            if (!_isChatHistoryLoaded || ClientChatMessagesStack.Children.Count == 0)
            {
                await LoadChatHistoryAsync();
            }
            else
            {
                ClientChatScrollViewer.ScrollToEnd();
            }
        }

        private async Task LoadChatHistoryAsync()
        {
            try
            {
                var history = await _signalRService.GetChatHistoryAsync();
                if (history != null && history.Count > 0)
                {
                    _isChatHistoryLoaded = true;
                    foreach (var m in history)
                    {
                        AddChatMessageBubble(m);
                    }
                    ClientChatScrollViewer.ScrollToEnd();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat History Error] {ex.Message}");
            }
        }

        private void CloseChatView()
        {
            ChatOverlayView.Visibility = Visibility.Collapsed;
            MainDashboardView.Visibility = Visibility.Visible;
        }

        private void TxtClientChatInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtClientChatPlaceholder != null)
            {
                TxtClientChatPlaceholder.Visibility = string.IsNullOrEmpty(TxtClientChatInput.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void TxtClientChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSendClientChat_Click(sender, e);
            }
        }

        private string? _pendingChatImageFilePath;
        private BitmapSource? _pendingChatBitmapSource;

        private void TxtClientChatInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // 1. Dán ảnh trực tiếp từ Clipboard (chụp màn hình PrtScn, Snipping Tool, Zalo...)
                if (Clipboard.ContainsImage())
                {
                    e.Handled = true;
                    var bitmap = Clipboard.GetImage();
                    if (bitmap != null)
                    {
                        StageClientBitmapAttachment(bitmap);
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
                            StageClientImageAttachment(file);
                            return;
                        }
                    }
                }
            }
        }

        private void StageClientImageAttachment(string filePath)
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

                ImgClientAttachmentThumbnail.Source = bitmap;
                TxtClientAttachmentName.Text = System.IO.Path.GetFileName(filePath);
                ClientAttachmentPreviewPanel.Visibility = Visibility.Visible;
                TxtClientChatInput.Focus();
            }
            catch { }
        }

        private void StageClientBitmapAttachment(BitmapSource bitmapSource)
        {
            _pendingChatBitmapSource = bitmapSource;
            _pendingChatImageFilePath = null;

            ImgClientAttachmentThumbnail.Source = bitmapSource;
            TxtClientAttachmentName.Text = $"Ảnh chụp màn hình ({DateTime.Now:HH:mm:ss})";
            ClientAttachmentPreviewPanel.Visibility = Visibility.Visible;
            TxtClientChatInput.Focus();
        }

        private void BtnRemoveClientAttachment_Click(object sender, RoutedEventArgs e)
        {
            ClearClientPendingAttachment();
        }

        private void ClearClientPendingAttachment()
        {
            _pendingChatImageFilePath = null;
            _pendingChatBitmapSource = null;
            ImgClientAttachmentThumbnail.Source = null;
            ClientAttachmentPreviewPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnAttachClientChatImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn hình ảnh gửi IT Support",
                Filter = "Hình ảnh (*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp"
            };

            if (openFileDialog.ShowDialog() != true) return;
            StageClientImageAttachment(openFileDialog.FileName);
        }

        private async void BtnSendClientChat_Click(object sender, RoutedEventArgs e)
        {
            var text = TxtClientChatInput.Text.Trim();
            bool hasPendingImage = _pendingChatBitmapSource != null || !string.IsNullOrEmpty(_pendingChatImageFilePath);

            if (string.IsNullOrEmpty(text) && !hasPendingImage) return;

            TxtClientChatInput.Clear();
            var pendingBitmap = _pendingChatBitmapSource;
            var pendingFilePath = _pendingChatImageFilePath;
            ClearClientPendingAttachment();

            BtnSendClientChat.IsEnabled = false;
            BtnAttachClientChatImage.IsEnabled = false;
            Cursor = Cursors.Wait;

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

                    var fileName = $"clipboard_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
                    imageUrl = await _signalRService.UploadImageStreamAsync(stream, fileName);
                }
                else if (!string.IsNullOrEmpty(pendingFilePath))
                {
                    imageUrl = await _signalRService.UploadImageAsync(pendingFilePath);
                }

                await _signalRService.SendChatMessageAsync(text, imageUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể gửi tin nhắn/hình ảnh: {ex.Message}", "Lỗi Gửi Tin Nhắn", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSendClientChat.IsEnabled = true;
                BtnAttachClientChatImage.IsEnabled = true;
                Cursor = Cursors.Arrow;
            }
        }

        private void OnChatMessageReceived(ChatMessageDto msg)
        {
            Dispatcher.BeginInvoke(() =>
            {
                AddChatMessageBubble(msg);

                if (ChatOverlayView.Visibility != Visibility.Visible)
                {
                    BadgeNewMessage.Visibility = Visibility.Visible;

                    // Chỉ hiển thị BalloonTip và phát âm thanh khi IT gửi tin nhắn đến
                    if (msg.IsFromAdmin)
                    {
                        try
                        {
                            System.Media.SystemSounds.Asterisk.Play();
                        }
                        catch { }

                        var notifyText = !string.IsNullOrWhiteSpace(msg.Message) && msg.Message != "[Hình ảnh]"
                            ? msg.Message
                            : "Đã gửi cho bạn một hình ảnh.";

                        MyNotifyIcon.ShowBalloonTip(
                            $"IT Support ({msg.SenderName})",
                            notifyText,
                            BalloonIcon.Info);
                    }
                }
                else
                {
                    ClientChatScrollViewer.ScrollToEnd();
                }
            });
        }

        private void AddChatMessageBubble(ChatMessageDto msg)
        {
            if (msg.Id > 0 && !_renderedChatIds.Add(msg.Id))
            {
                return; // Đã vẽ rồi, tránh trùng lặp
            }

            bool isMe = !msg.IsFromAdmin;

            var container = new Grid
            {
                Margin = isMe ? new Thickness(0, 4, 10, 4) : new Thickness(0, 4, 0, 4),
                HorizontalAlignment = isMe ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var bubble = new Border
            {
                CornerRadius = isMe ? new CornerRadius(14, 14, 2, 14) : new CornerRadius(14, 14, 14, 2),
                Background = isMe
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563eb"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f1f5f9")),
                Padding = new Thickness(12, 8, 12, 8),
                MaxWidth = 420
            };

            var contentStack = new StackPanel();

            // Tên người gửi + Giờ
            var headerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 3)
            };

            var senderText = new TextBlock
            {
                Text = isMe ? "Bạn" : $"IT Support ({msg.SenderName})",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = isMe
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bfdbfe"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                Margin = new Thickness(0, 0, 8, 0)
            };

            var timeText = new TextBlock
            {
                Text = msg.TimeDisplay,
                FontSize = 10,
                Foreground = isMe
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bfdbfe"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8"))
            };

            headerStack.Children.Add(senderText);
            headerStack.Children.Add(timeText);
            contentStack.Children.Add(headerStack);

            // 1. Nếu có hình ảnh đính kèm từ ImageKit
            if (!string.IsNullOrEmpty(msg.ImageUrl))
            {
                try
                {
                    var imgBorder = new Border
                    {
                        CornerRadius = new CornerRadius(8),
                        ClipToBounds = true,
                        Margin = new Thickness(0, 2, 0, 4),
                        Cursor = Cursors.Hand
                    };

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(msg.ImageUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    var img = new Image
                    {
                        Source = bitmap,
                        MaxHeight = 220,
                        MaxWidth = 280,
                        Stretch = Stretch.Uniform,
                        ToolTip = "Bấm để mở ảnh xem kích thước gốc"
                    };

                    imgBorder.Child = img;
                    imgBorder.MouseLeftButtonDown += (s, e) =>
                    {
                        try
                        {
                            var preview = new Views.ImagePreviewWindow(msg.ImageUrl)
                            {
                                Owner = this
                            };
                            preview.ShowDialog();
                        }
                        catch { }
                    };

                    contentStack.Children.Add(imgBorder);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Client Chat] Không thể load ảnh {msg.ImageUrl}: {ex.Message}");
                }
            }

            // 2. Nội dung văn bản (nếu có và không phải placeholder "[Hình ảnh]")
            if (!string.IsNullOrWhiteSpace(msg.Message) && (msg.Message != "[Hình ảnh]" || string.IsNullOrEmpty(msg.ImageUrl)))
            {
                var bodyText = new TextBlock
                {
                    Text = msg.Message,
                    FontSize = 12.5,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = isMe
                        ? Brushes.White
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f172a"))
                };
                contentStack.Children.Add(bodyText);
            }

            bubble.Child = contentStack;
            container.Children.Add(bubble);

            ClientChatMessagesStack.Children.Add(container);
            ClientChatScrollViewer.ScrollToEnd();
        }

        #endregion
    }
}
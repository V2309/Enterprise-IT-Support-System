using DeskPilot.Admin.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DeskPilot.Admin.Views
{
    public partial class ChatView : UserControl
    {
        // 3 bộ gợi ý câu hỏi để luân chuyển khi bấm "Refresh Prompts"
        private readonly List<(string P1, string P2, string P3, string P4)> _promptSets = new()
        {
            // Bộ 1: Theo mẫu thiết kế gốc
            (
                "Write a to-do list for a personal project or task",
                "Generate an email to reply to a job offer",
                "Summarise this article or text for me in one paragraph",
                "How does AI work in a technical capacity"
            ),
            // Bộ 2: Trợ lý IT Support & Quản trị DeskPilot
            (
                "Kiểm tra danh sách toàn bộ máy trạm đang mất kết nối (Offline)",
                "Khởi động lại Print Spooler cho các máy đang báo lỗi in ấn",
                "Tổng hợp các phiếu hỗ trợ (Ticket) sự cố còn tồn đọng hôm nay",
                "Phân tích tài nguyên RAM/Ổ C của các máy có nguy cơ quá tải"
            ),
            // Bộ 3: Tự động hóa & Khắc phục sự cố mạng
            (
                "Chạy kịch bản Flush DNS cho máy trạm báo lỗi kết nối mạng nội bộ",
                "Tạo kịch bản dọn dẹp bộ nhớ đệm và file tạm %temp% trên ổ C",
                "Xuất danh sách 20 lệnh can thiệp IT Action Logs gần nhất",
                "Hướng dẫn khắc phục sự cố dịch vụ in ấn bị dừng (Spooler Stopped)"
            )
        };

        private int _currentPromptSetIndex = 0;

        public ChatView()
        {
            InitializeComponent();
            UpdatePromptCards();
        }

        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

        // Cập nhật nội dung 4 thẻ gợi ý
        private void UpdatePromptCards()
        {
            var currentSet = _promptSets[_currentPromptSetIndex];
            TxtPrompt1.Text = currentSet.P1;
            TxtPrompt2.Text = currentSet.P2;
            TxtPrompt3.Text = currentSet.P3;
            TxtPrompt4.Text = currentSet.P4;
        }

        // Bấm nút Refresh Prompts: xoay vòng qua các bộ câu hỏi
        private void BtnRefreshPrompts_Click(object sender, RoutedEventArgs e)
        {
            _currentPromptSetIndex = (_currentPromptSetIndex + 1) % _promptSets.Count;
            UpdatePromptCards();
        }

        // Xử lý khi bấm vào 1 trong 4 Card gợi ý
        private void PromptCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                string selectedPrompt = tag switch
                {
                    "1" => TxtPrompt1.Text,
                    "2" => TxtPrompt2.Text,
                    "3" => TxtPrompt3.Text,
                    "4" => TxtPrompt4.Text,
                    _ => ""
                };

                if (!string.IsNullOrWhiteSpace(selectedPrompt))
                {
                    TxtMainInput.Text = selectedPrompt;
                    TxtPlaceholder.Visibility = Visibility.Collapsed;
                    TxtMainInput.CaretIndex = TxtMainInput.Text.Length;
                    TxtMainInput.Focus();
                }
            }
        }

        // Cập nhật bộ đếm ký tự và ẩn/hiện Placeholder
        private void TxtMainInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int length = TxtMainInput.Text.Length;
            TxtCharCounter.Text = $"{length}/1000";
            TxtPlaceholder.Visibility = length == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Nhấn Enter để gửi tin nhắn (Shift+Enter để xuống dòng)
        private void TxtMainInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                SendFromLanding();
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e) => SendFromLanding();

        private void SendFromLanding()
        {
            string prompt = TxtMainInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt)) return;

            TxtMainInput.Text = string.Empty;
            StartActiveChat(prompt);
        }

        // Chuyển sang giao diện trò chuyện đang hoạt động
        private void StartActiveChat(string firstPrompt)
        {
            LandingContainer.Visibility = Visibility.Collapsed;
            ActiveChatContainer.Visibility = Visibility.Visible;
            ChatMessagesStack.Children.Clear();

            AddUserMessage(firstPrompt);
            _ = GenerateAiResponseAsync(firstPrompt);
        }

        // Quay lại màn hình Chào mừng (Landing Screen) ban đầu
        private void BtnNewChat_Click(object sender, RoutedEventArgs e)
        {
            ActiveChatContainer.Visibility = Visibility.Collapsed;
            LandingContainer.Visibility = Visibility.Visible;
            TxtMainInput.Text = string.Empty;
            TxtMainInput.Focus();
        }

        private void TxtActiveChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                SendFromActiveChat();
            }
        }

        private void BtnActiveSend_Click(object sender, RoutedEventArgs e) => SendFromActiveChat();

        private void SendFromActiveChat()
        {
            string prompt = TxtActiveChatInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt)) return;

            TxtActiveChatInput.Text = string.Empty;
            AddUserMessage(prompt);
            _ = GenerateAiResponseAsync(prompt);
        }

        // Thêm bong bóng tin nhắn người dùng
        private void AddUserMessage(string message)
        {
            var bubble = new Border
            {
                Background = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#7c3aed"),
                    (Color)ColorConverter.ConvertFromString("#6366f1"),
                    new Point(0, 0), new Point(1, 1)),
                CornerRadius = new CornerRadius(16, 16, 2, 16),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(60, 8, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 680
            };

            var text = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };

            bubble.Child = text;
            ChatMessagesStack.Children.Add(bubble);
            ScrollToBottom();
        }

        // Giả lập phản hồi AI thông minh dựa trên ngữ cảnh hệ thống DeskPilot
        private async Task GenerateAiResponseAsync(string userPrompt)
        {
            // Hiển thị chỉ báo "Đang xử lý..."
            var loadingBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e2e8f0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16, 16, 16, 2),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 8, 60, 8),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var loadingText = new TextBlock
            {
                Text = "⚡ DeskPilot AI đang phân tích yêu cầu...",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
                FontStyle = FontStyles.Italic
            };
            loadingBorder.Child = loadingText;
            ChatMessagesStack.Children.Add(loadingBorder);
            ScrollToBottom();

            await Task.Delay(800); // Tạo cảm giác AI đang suy nghĩ

            ChatMessagesStack.Children.Remove(loadingBorder);

            // Sinh câu trả lời dựa trên nội dung prompt và dữ liệu thực tế
            string reply = ProcessAiResponse(userPrompt);

            var replyBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e2e8f0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16, 16, 16, 2),
                Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(0, 8, 60, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 720
            };
            replyBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 2,
                Opacity = 0.05,
                Color = (Color)ColorConverter.ConvertFromString("#0f172a")
            };

            var stack = new StackPanel();

            // Header AI badge
            var badge = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var iconBorder = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Margin = new Thickness(0, 0, 8, 0),
                Background = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#7c3aed"),
                    (Color)ColorConverter.ConvertFromString("#6366f1"),
                    new Point(0, 0), new Point(1, 1))
            };
            badge.Children.Add(iconBorder);
            badge.Children.Add(new TextBlock
            {
                Text = "DeskPilot Assistant",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4338ca")),
                VerticalAlignment = VerticalAlignment.Center
            });

            stack.Children.Add(badge);
            stack.Children.Add(new TextBlock
            {
                Text = reply,
                FontSize = 13.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 21
            });

            replyBorder.Child = stack;
            ChatMessagesStack.Children.Add(replyBorder);
            ScrollToBottom();
        }

        private string ProcessAiResponse(string prompt)
        {
            string pLower = prompt.ToLower();

            if (pLower.Contains("offline") || pLower.Contains("mất kết nối"))
            {
                int offlineCount = 0;
                if (ViewModel != null)
                {
                    foreach (var m in ViewModel.Machines)
                    {
                        if (!m.IsOnline) offlineCount++;
                    }
                }
                return $"Tôi đã quét tình trạng mạng trên toàn hệ thống DeskPilot:\n\n" +
                       $"• Hiện đang có {offlineCount} máy trạm ở trạng thái Ngoại tuyến (Offline).\n" +
                       $"• Các máy này chưa gửi nhịp tim trong vòng 10 giây qua.\n\n" +
                       $"💡 Gợi ý: Bạn có thể vào tab 'Tổng quan' để kiểm tra địa chỉ máy trạm cụ thể hoặc liên hệ người phụ trách phòng ban.";
            }

            if (pLower.Contains("ticket") || pLower.Contains("phiếu"))
            {
                int ticketCount = ViewModel?.Tickets.Count ?? 0;
                return $"Báo cáo phiếu hỗ trợ kỹ thuật:\n\n" +
                       $"• Hiện có {ticketCount} Ticket đang chờ xử lý từ các phòng ban.\n" +
                       $"• Bạn có thể chuyển sang tab 'Danh sách Ticket' để xem chi tiết hoặc bấm 'Hoàn tất' sau khi đã giải quyết xong sự cố.";
            }

            if (pLower.Contains("spooler") || pLower.Contains("máy in") || pLower.Contains("in ấn"))
            {
                return $"Hướng dẫn xử lý sự cố Print Spooler:\n\n" +
                       $"1. Để khởi động lại dịch vụ in từ xa: Chọn máy trạm tại tab 'Tổng quan' và bấm nút 'Khởi động Spooler'.\n" +
                       $"2. Hệ thống sẽ phát tín hiệu qua SignalR xuống Client và chạy lệnh PowerShell 'Restart-Service spooler -Force'.\n" +
                       $"3. Máy trạm sẽ báo cáo kết quả thực thi về Dashboard ngay lập tức.";
            }

            if (pLower.Contains("ram") || pLower.Contains("ổ c") || pLower.Contains("tài nguyên"))
            {
                return $"Phân tích tài nguyên phần cứng DeskPilot:\n\n" +
                       $"• Ngưỡng cảnh báo RAM: Trên 85% sẽ phát chuông và cảnh báo Toast đỏ.\n" +
                       $"• Ngưỡng cảnh báo ổ đĩa: Dung lượng trống ổ C dưới 5GB sẽ được đánh dấu nguy cơ đầy đĩa.\n" +
                       $"• Bạn có thể dùng tính năng 'Quản lý Runbook' để thực thi lệnh dọn dẹp file rác trên các máy trạm này.";
            }

            return $"Xin chào! Tôi là trợ lý ảo hỗ trợ quản trị hệ thống DeskPilot.\n\n" +
                   $"Tôi có thể giúp bạn:\n" +
                   $"• Giám sát tình trạng phần cứng máy trạm (CPU, RAM, Ổ cứng, Spooler)\n" +
                   $"• Tra cứu và điều phối các Ticket báo sự cố của nhân viên\n" +
                   $"• Hướng dẫn kích hoạt các kịch bản Runbook điều khiển từ xa (Flush DNS, Fix Print Spooler)\n\n" +
                   $"Hãy đặt câu hỏi hoặc chọn một trong các gợi ý trên màn hình để bắt đầu!";
        }

        private void ScrollToBottom()
        {
            ChatMessagesScrollViewer?.ScrollToEnd();
        }

        private async void BtnAttachment_Click(object sender, RoutedEventArgs e)
        {
            await Services.ModalDialogService.ShowInfoAsync("Đính Kèm Tệp", "Tính năng đính kèm tệp cấu hình / log sẽ sẵn sàng trong bản cập nhật kế tiếp.");
        }

        private async void BtnUseImage_Click(object sender, RoutedEventArgs e)
        {
            await Services.ModalDialogService.ShowInfoAsync("Phân Tích Hình Ảnh", "Tính năng phân tích ảnh chụp màn hình lỗi bằng AI sẽ sẵn sàng trong bản cập nhật kế tiếp.");
        }
    }
}

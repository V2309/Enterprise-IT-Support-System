using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeskPilot.Client.Services;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Client
{
    public partial class MainWindow : Window
    {
        private readonly HardwareMonitorService _monitorService = new();
        private readonly SignalRClientService _signalRService = new();
        private readonly RunbookExecutor _runbookExecutor = new();
        private readonly DispatcherTimer _timer = new();
        private bool _isExplicitExit = false;

        public MainWindow()
        {
            InitializeComponent();
            PositionCenterScreen();

            // Đăng ký nhận lệnh từ Admin
            _signalRService.OnCommandReceived += HandleRemoteCommandAsync;

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

        // Xử lý các lệnh sửa lỗi được IT gửi xuống từ xa qua SignalR
        private async Task HandleRemoteCommandAsync(string commandName)
        {
            string messageResult = string.Empty;

            if (commandName == "FixPrint")
            {
                messageResult = await _runbookExecutor.RestartPrintSpoolerAsync();
            }
            else if (commandName == "FlushDns")
            {
                messageResult = await _runbookExecutor.FlushDnsAsync();
            }

            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"IT Support vừa gửi lệnh khắc phục:\n{messageResult}",
                                "DeskPilot Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
            try
            {
                var printTask = _runbookExecutor.RestartPrintSpoolerAsync();
                var dnsTask = _runbookExecutor.FlushDnsAsync();
                await Task.WhenAll(printTask, dnsTask);

                MessageBox.Show("Đã hoàn tất tự động kiểm tra và chạy Runbook sửa lỗi:\n" +
                                "✓ Khởi động lại dịch vụ máy in (Print Spooler)\n" +
                                "✓ Xóa sạch bộ đệm phân giải tên miền (Flush DNS)",
                                "Tự Chẩn Đoán & Sửa Nhanh", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi trong quá trình tự sửa: {ex.Message}", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}
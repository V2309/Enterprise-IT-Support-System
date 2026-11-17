using DeskPilot.Admin.ViewModels;
using DeskPilot.Shared.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DeskPilot.Admin.Views
{
    public partial class DashboardView : UserControl
    {
        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

        private MachineStatusDto? _selectedMachine;
        private bool _showRam = true;
        private bool _showDisk = true;

        public DashboardView()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (ViewModel != null)
                {
                    ViewModel.OnMetricsHistoryUpdated -= OnMetricsUpdated;
                    ViewModel.OnMetricsHistoryUpdated += OnMetricsUpdated;
                }
            };

            Unloaded += (s, e) =>
            {
                if (ViewModel != null)
                {
                    ViewModel.OnMetricsHistoryUpdated -= OnMetricsUpdated;
                }
            };
        }

        // Tự động cập nhật biểu đồ theo thời gian thực nếu modal của máy đó đang mở
        private void OnMetricsUpdated(string machineName)
        {
            if (MetricsModalBackdrop.Visibility == Visibility.Visible &&
                _selectedMachine != null &&
                _selectedMachine.MachineName.Equals(machineName, StringComparison.OrdinalIgnoreCase))
            {
                Dispatcher.Invoke(() =>
                {
                    RefreshMetricsModalData();
                });
            }
        }

        #region MỞ VÀ CẬP NHẬT MODAL BIỂU ĐỒ LỊCH SỬ NỔI
        private void BtnViewMetricsHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MachineStatusDto machine)
            {
                _selectedMachine = machine;
                _showRam = true;
                _showDisk = true;
                UpdateFilterButtonsStyle();

                RefreshMetricsModalData();

                MetricsModalBackdrop.Visibility = Visibility.Visible;
            }
        }

        private void RefreshMetricsModalData()
        {
            if (_selectedMachine == null || ViewModel == null) return;

            // 1. Cập nhật Tiêu đề và trạng thái
            TxtModalMachineName.Text = $"Máy: {_selectedMachine.MachineName}";
            TxtModalMachineSub.Text = $"Nhân viên: {_selectedMachine.UserName} • Dung lượng RAM: {_selectedMachine.TotalRamGB:0.#} GB • Ổ C: {_selectedMachine.TotalDiskSpaceGB:0.#} GB";

            if (_selectedMachine.IsOnline)
            {
                ModalOnlineBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ecfdf5"));
                ModalOnlineBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d1fae5"));
                TxtModalStatus.Text = "Trực tuyến";
                TxtModalStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
            }
            else
            {
                ModalOnlineBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fef2f2"));
                ModalOnlineBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fee2e2"));
                TxtModalStatus.Text = "Ngoại tuyến";
                TxtModalStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
            }

            // 2. Lấy dữ liệu chuỗi lịch sử
            var history = ViewModel.GetMetricsHistory(_selectedMachine.MachineName, _selectedMachine.RamUsagePercent, _selectedMachine.DiskUsagePercent);

            if (history.Count == 0) return;

            // 3. Tính toán các chỉ số KPI
            var last = history.Last();
            double peakRam = history.Max(p => p.RamUsagePercent);
            double avgRam = history.Average(p => p.RamUsagePercent);

            TxtModalCurrentRam.Text = $"{last.RamUsagePercent:0.0}%";
            TxtModalPeakRam.Text = $"{peakRam:0.0}%";
            TxtModalAvgRam.Text = $"{avgRam:0.0}%";
            TxtModalDiskStatus.Text = $"{last.DiskUsagePercent:0.0}% (Trống {last.FreeDiskSpaceGB:0.0} GB)";

            // Đổi màu cảnh báo nếu RAM cao
            TxtModalCurrentRam.Foreground = last.RamUsagePercent >= 85
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981"));

            // 4. Vẽ lại Line Chart
            RenderChart(history);

            // 5. Cập nhật bảng mẫu đo gần nhất
            PopulateRecentSamples(history);
        }

        private void PopulateRecentSamples(List<MetricPoint> history)
        {
            RecentSamplesStack.Children.Clear();

            // Hiển thị tối đa 5 mốc đo gần nhất theo thứ tự mới nhất lên đầu
            var recent5 = history.TakeLast(5).Reverse().ToList();

            foreach (var pt in recent5)
            {
                var row = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };

                // Thời gian
                var timeBlock = new TextBlock
                {
                    Text = pt.TimeLabel,
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
                    Width = 70
                };
                DockPanel.SetDock(timeBlock, Dock.Left);
                row.Children.Add(timeBlock);

                // Chi tiết RAM
                var ramBlock = new TextBlock
                {
                    Text = $"RAM: {pt.RamUsagePercent:0.0}%",
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = pt.RamUsagePercent >= 85
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
                    Width = 110
                };
                DockPanel.SetDock(ramBlock, Dock.Left);
                row.Children.Add(ramBlock);

                // Chi tiết Ổ đĩa
                var diskBlock = new TextBlock
                {
                    Text = $"Ổ C: {pt.DiskUsagePercent:0.0}% (Còn {pt.FreeDiskSpaceGB:0.0} GB)",
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4338ca")),
                    Width = 170
                };
                DockPanel.SetDock(diskBlock, Dock.Left);
                row.Children.Add(diskBlock);

                // CPU
                var cpuBlock = new TextBlock
                {
                    Text = $"CPU: {pt.CpuUsagePercent:0.0}%",
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f172a")),
                    Width = 100
                };
                DockPanel.SetDock(cpuBlock, Dock.Left);
                row.Children.Add(cpuBlock);

                // Badge trạng thái
                var badgeBdr = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 2, 8, 2),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                var badgeTxt = new TextBlock { FontSize = 10.5, FontWeight = FontWeights.SemiBold };

                if (pt.RamUsagePercent >= 85)
                {
                    badgeBdr.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fef2f2"));
                    badgeTxt.Text = "Cảnh báo tải cao";
                    badgeTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
                }
                else
                {
                    badgeBdr.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ecfdf5"));
                    badgeTxt.Text = "Ổn định";
                    badgeTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981"));
                }

                badgeBdr.Child = badgeTxt;
                DockPanel.SetDock(badgeBdr, Dock.Right);
                row.Children.Add(badgeBdr);

                RecentSamplesStack.Children.Add(row);
            }
        }
        #endregion

        #region THUẬT TOÁN VẼ ĐỒ THỊ ĐƯỜNG (LINE CHART ENGINE)
        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_selectedMachine != null && ViewModel != null)
            {
                var history = ViewModel.GetMetricsHistory(_selectedMachine.MachineName);
                RenderChart(history);
            }
        }

        private void RenderChart(List<MetricPoint> points)
        {
            ChartCanvas.Children.Clear();
            if (points == null || points.Count == 0) return;

            // Nếu chỉ mới có 1 điểm dữ liệu thời gian thực, tạo thêm 1 điểm lùi về trước 5 giây để vẽ đường biểu diễn
            if (points.Count == 1)
            {
                var single = points[0];
                points = new List<MetricPoint>
                {
                    new MetricPoint
                    {
                        Timestamp = single.Timestamp.AddSeconds(-5),
                        RamUsagePercent = single.RamUsagePercent,
                        DiskUsagePercent = single.DiskUsagePercent,
                        CpuUsagePercent = single.CpuUsagePercent,
                        FreeDiskSpaceGB = single.FreeDiskSpaceGB
                    },
                    single
                };
            }

            double w = ChartCanvas.ActualWidth;
            double h = ChartCanvas.ActualHeight;
            if (w <= 0 || double.IsNaN(w)) w = 840;
            if (h <= 0 || double.IsNaN(h)) h = 220;

            double padLeft = 46;
            double padRight = 20;
            double padTop = 18;
            double padBottom = 26;

            double plotW = Math.Max(10, w - padLeft - padRight);
            double plotH = Math.Max(10, h - padTop - padBottom);

            // 1. Vẽ các đường lưới trục Y và nhãn % (0%, 25%, 50%, 75%, 100%)
            for (int i = 0; i <= 4; i++)
            {
                double pct = i * 25.0;
                double y = padTop + plotH - (pct / 100.0) * plotH;

                var gridLine = new Line
                {
                    X1 = padLeft,
                    Y1 = y,
                    X2 = w - padRight,
                    Y2 = y,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(i == 0 ? "#cbd5e1" : "#e2e8f0")),
                    StrokeThickness = i == 0 ? 1.5 : 1.0
                };
                if (i > 0)
                {
                    gridLine.StrokeDashArray = new DoubleCollection { 4, 4 };
                }
                ChartCanvas.Children.Add(gridLine);

                var yLabel = new TextBlock
                {
                    Text = $"{pct:0}%",
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                    TextAlignment = TextAlignment.Right,
                    Width = 36
                };
                Canvas.SetLeft(yLabel, 4);
                Canvas.SetTop(yLabel, y - 7);
                ChartCanvas.Children.Add(yLabel);
            }

            // 2. Đường cảnh báo đỏ 85%
            double warningY = padTop + plotH - (85.0 / 100.0) * plotH;
            var alertLine = new Line
            {
                X1 = padLeft,
                Y1 = warningY,
                X2 = w - padRight,
                Y2 = warningY,
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fca5a5")),
                StrokeThickness = 1.2,
                StrokeDashArray = new DoubleCollection { 3, 3 }
            };
            ChartCanvas.Children.Add(alertLine);

            // 3. Tính toán tọa độ X, Y cho từng điểm
            int n = points.Count;
            double stepX = plotW / (n - 1);

            PointCollection ramPoints = new();
            PointCollection diskPoints = new();

            for (int i = 0; i < n; i++)
            {
                double x = padLeft + i * stepX;
                double ramY = padTop + plotH - (Math.Clamp(points[i].RamUsagePercent, 0, 100) / 100.0) * plotH;
                double diskY = padTop + plotH - (Math.Clamp(points[i].DiskUsagePercent, 0, 100) / 100.0) * plotH;

                ramPoints.Add(new Point(x, ramY));
                diskPoints.Add(new Point(x, diskY));

                // Nhãn thời gian trục hoành X (cách khoảng 3-4 điểm một nhãn)
                if (i == 0 || i == n - 1 || i % Math.Max(1, n / 5) == 0)
                {
                    var xLabel = new TextBlock
                    {
                        Text = points[i].TimeLabel,
                        FontSize = 9.5,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
                        TextAlignment = TextAlignment.Center,
                        Width = 60
                    };
                    Canvas.SetLeft(xLabel, x - 30);
                    Canvas.SetTop(xLabel, padTop + plotH + 6);
                    ChartCanvas.Children.Add(xLabel);
                }
            }

            double baseY = padTop + plotH;

            // 4. Vẽ đường Disk (nếu bật _showDisk)
            if (_showDisk)
            {
                DrawLineSeries(diskPoints, padLeft, baseY, "#6366f1", "#186366f1", "Disk", points, p => $"{p.DiskUsagePercent:0.1}% (Còn {p.FreeDiskSpaceGB:0.1} GB)");
            }

            // 5. Vẽ đường RAM (nếu bật _showRam)
            if (_showRam)
            {
                DrawLineSeries(ramPoints, padLeft, baseY, "#10b981", "#2510b981", "RAM", points, p => $"{p.RamUsagePercent:0.1}%");
            }
        }

        private void DrawLineSeries(PointCollection pts, double startX, double baseY, string strokeHex, string fillHex, string name, List<MetricPoint> points, Func<MetricPoint, string> infoSelector)
        {
            var strokeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(strokeHex));
            var fillBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(fillHex),
                Colors.Transparent,
                new Point(0, 0), new Point(0, 1));

            // Vùng đổ bóng mờ dưới đường (Area Fill)
            var areaPoints = new PointCollection(pts)
            {
                new Point(pts.Last().X, baseY),
                new Point(startX, baseY)
            };

            var areaPolygon = new Polygon
            {
                Points = areaPoints,
                Fill = fillBrush
            };
            ChartCanvas.Children.Add(areaPolygon);

            // Đường Line nét chính
            var polyline = new Polyline
            {
                Points = pts,
                Stroke = strokeBrush,
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round
            };
            ChartCanvas.Children.Add(polyline);

            // Các chấm tròn dữ liệu tương tác tại mỗi điểm
            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                var dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.White,
                    Stroke = strokeBrush,
                    StrokeThickness = 2,
                    Cursor = Cursors.Hand,
                    ToolTip = $"[{name}] {infoSelector(points[i])} lúc {points[i].TimeLabel}"
                };

                Canvas.SetLeft(dot, pt.X - 3);
                Canvas.SetTop(dot, pt.Y - 3);
                ChartCanvas.Children.Add(dot);
            }
        }
        #endregion

        #region BỘ LỌC TABS & ĐÓNG MODAL
        private void BtnFilterAll_Click(object sender, RoutedEventArgs e)
        {
            _showRam = true;
            _showDisk = true;
            UpdateFilterButtonsStyle();
            if (_selectedMachine != null && ViewModel != null)
            {
                RenderChart(ViewModel.GetMetricsHistory(_selectedMachine.MachineName));
            }
        }

        private void BtnFilterRam_Click(object sender, RoutedEventArgs e)
        {
            _showRam = true;
            _showDisk = false;
            UpdateFilterButtonsStyle();
            if (_selectedMachine != null && ViewModel != null)
            {
                RenderChart(ViewModel.GetMetricsHistory(_selectedMachine.MachineName));
            }
        }

        private void BtnFilterDisk_Click(object sender, RoutedEventArgs e)
        {
            _showRam = false;
            _showDisk = true;
            UpdateFilterButtonsStyle();
            if (_selectedMachine != null && ViewModel != null)
            {
                RenderChart(ViewModel.GetMetricsHistory(_selectedMachine.MachineName));
            }
        }

        private void UpdateFilterButtonsStyle()
        {
            var activeBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffffff"));
            var activeFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f172a"));
            var inactiveBg = Brushes.Transparent;
            var inactiveFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b"));

            bool isAll = _showRam && _showDisk;
            bool isRam = _showRam && !_showDisk;
            bool isDisk = !_showRam && _showDisk;

            BtnFilterAll.Background = isAll ? activeBg : inactiveBg;
            BtnFilterAll.Foreground = isAll ? activeFg : inactiveFg;

            BtnFilterRam.Background = isRam ? activeBg : inactiveBg;
            BtnFilterRam.Foreground = isRam ? activeFg : inactiveFg;

            BtnFilterDisk.Background = isDisk ? activeBg : inactiveBg;
            BtnFilterDisk.Foreground = isDisk ? activeFg : inactiveFg;
        }

        private void BtnCloseMetricsModal_Click(object sender, RoutedEventArgs e)
        {
            MetricsModalBackdrop.Visibility = Visibility.Collapsed;
        }

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == MetricsModalBackdrop)
            {
                MetricsModalBackdrop.Visibility = Visibility.Collapsed;
            }
        }

        private void ModalCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Ngăn sự kiện nổi bọt để không đóng modal khi bấm trong khung card
        }
        #endregion

        #region QUICK ACTION HANDLERS
        private async void BtnFixSpooler_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MachineStatusDto machine && ViewModel != null)
            {
                await ViewModel.SendCommandAsync(machine.MachineName, "FixPrint");
            }
        }

        private async void BtnFlushDns_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MachineStatusDto machine && ViewModel != null)
            {
                await ViewModel.SendCommandAsync(machine.MachineName, "FlushDns");
            }
        }

        private async void BtnRemoteShell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MachineStatusDto machine)
            {
                await DeskPilot.Admin.Services.ModalDialogService.ShowInfoAsync(
                    "Remote Shell",
                    $"Đang khởi tạo kết nối phiên điều khiển Remote Terminal tới máy trạm {machine.MachineName} ({machine.UserName})...");
            }
        }
        #endregion
    }
}

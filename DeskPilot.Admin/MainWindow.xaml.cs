using DeskPilot.Admin.Services;
using DeskPilot.Admin.ViewModels;
using DeskPilot.Admin.Views;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DeskPilot.Admin
{
    public partial class MainWindow : Window
    {
        public DashboardViewModel ViewModel { get; } = new();

        // Cache các View để chuyển tab tức thì và giữ nguyên trạng thái cuộn
        private DashboardView? _dashboardView;
        private TicketsView? _ticketsView;
        private ActionLogsView? _actionLogsView;

        private TaskCompletionSource<bool>? _modalTcs;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;

            // Đăng ký Modal Dialog Service toàn hệ thống
            ModalDialogService.Register(this);

            // Khởi tạo các View dùng chung trong SPA
            _dashboardView = new DashboardView { DataContext = ViewModel };
            _ticketsView = new TicketsView { DataContext = ViewModel };
            _actionLogsView = new ActionLogsView { DataContext = ViewModel };

            // Mặc định hiển thị tab Tổng quan
            ViewModel.CurrentView = _dashboardView;
            ViewModel.CurrentTab = "Dashboard";

            Loaded += async (s, e) =>
            {
                await ViewModel.StartListeningAsync();
            };
        }

        // Điều hướng sang Tab Tổng quan (Dàn máy + Live Feed)
        public void NavigateToOverview()
        {
            _dashboardView ??= new DashboardView { DataContext = ViewModel };
            ViewModel.CurrentTab = "Dashboard";
            ViewModel.CurrentView = _dashboardView;
        }

        // Điều hướng sang Tab Danh sách Ticket
        public void NavigateToTickets()
        {
            _ticketsView ??= new TicketsView { DataContext = ViewModel };
            ViewModel.CurrentTab = "Tickets";
            ViewModel.CurrentView = _ticketsView;
        }

        // Điều hướng sang Tab Quản lý Runbook / Action Logs
        public void NavigateToLogs()
        {
            _actionLogsView ??= new ActionLogsView { DataContext = ViewModel };
            ViewModel.CurrentTab = "Logs";
            ViewModel.CurrentView = _actionLogsView;
        }

        private void NavOverview_Click(object sender, RoutedEventArgs e) => NavigateToOverview();
        private void NavTickets_Click(object sender, RoutedEventArgs e) => NavigateToTickets();
        private void NavLogs_Click(object sender, RoutedEventArgs e) => NavigateToLogs();

        private async void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            await ModalDialogService.ShowInfoAsync(
                "Cài Đặt Hệ Thống",
                "DeskPilot IT Command Center v1.0\nHệ thống giám sát và điều khiển máy trạm thời gian thực.\nPhát triển bởi đội ngũ kỹ thuật IT.");
        }

        // Lọc theo phòng ban trên Sidebar
        private void DeptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string deptName)
            {
                ViewModel.SelectedDepartment = deptName;
                // Nếu đang ở tab khác thì tự chuyển về Tổng quan để xem dàn máy phòng đó
                if (ViewModel.CurrentTab != "Dashboard")
                {
                    NavigateToOverview();
                }
            }
        }

        #region IN-APP MODAL OVERLAY LOGIC
        public Task<bool> ShowModalAsync(string title, string message, ModalType type, string confirmText, string cancelText)
        {
            _modalTcs = new TaskCompletionSource<bool>();

            ModalTitle.Text = title;
            ModalMessage.Text = message;
            ModalBtnConfirm.Content = confirmText;
            ModalBtnCancel.Content = cancelText;

            switch (type)
            {
                case ModalType.Success:
                    ModalIconBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ecfdf5"));
                    ModalIconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981"));
                    ModalIconPath.Data = Geometry.Parse("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z");
                    ModalBtnConfirm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981"));
                    ModalBtnCancel.Visibility = Visibility.Collapsed;
                    ModalBtnConfirm.Width = 200;
                    break;

                case ModalType.Warning:
                    ModalIconBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fffbeb"));
                    ModalIconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b"));
                    ModalIconPath.Data = Geometry.Parse("M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z");
                    ModalBtnConfirm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b"));
                    ModalBtnCancel.Visibility = Visibility.Collapsed;
                    ModalBtnConfirm.Width = 200;
                    break;

                case ModalType.Error:
                    ModalIconBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fef2f2"));
                    ModalIconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
                    ModalIconPath.Data = Geometry.Parse("M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z");
                    ModalBtnConfirm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
                    ModalBtnCancel.Visibility = Visibility.Collapsed;
                    ModalBtnConfirm.Width = 200;
                    break;

                case ModalType.Confirm:
                    ModalIconBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#eff6ff"));
                    ModalIconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563eb"));
                    ModalIconPath.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 16h-2v-2h2v2zm1.07-7.75l-.9.92C12.45 11.9 12 12.5 12 14h-2v-.5c0-1.1.45-2.1 1.17-2.83l1.24-1.26c.37-.36.59-.86.59-1.41 0-1.1-.9-2-2-2s-2 .9-2 2H7c0-2.76 2.24-5 5-5s5 2.24 5 5c0 1.04-.42 1.99-1.07 2.75z");
                    ModalBtnConfirm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563eb"));
                    ModalBtnCancel.Visibility = Visibility.Visible;
                    ModalBtnCancel.Width = 140;
                    ModalBtnConfirm.Width = 140;
                    break;

                default: // Info
                    ModalIconBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#eff6ff"));
                    ModalIconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563eb"));
                    ModalIconPath.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z");
                    ModalBtnConfirm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563eb"));
                    ModalBtnCancel.Visibility = Visibility.Collapsed;
                    ModalBtnConfirm.Width = 200;
                    break;
            }

            ModalBackdrop.Visibility = Visibility.Visible;
            return _modalTcs.Task;
        }

        private void ModalBtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ModalBackdrop.Visibility = Visibility.Collapsed;
            _modalTcs?.TrySetResult(true);
        }

        private void ModalBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ModalBackdrop.Visibility = Visibility.Collapsed;
            _modalTcs?.TrySetResult(false);
        }
        #endregion
    }
}
using System.Windows;
using System.Windows.Controls;
using DeskPilot.Admin.ViewModels;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Admin.Views
{
    public partial class DashboardView : UserControl
    {
        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

        public DashboardView()
        {
            InitializeComponent();
        }

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
    }
}

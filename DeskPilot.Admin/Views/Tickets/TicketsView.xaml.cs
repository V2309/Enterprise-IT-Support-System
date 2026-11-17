using System.Windows;
using System.Windows.Controls;
using DeskPilot.Admin.ViewModels;
using DeskPilot.Shared;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Admin.Views
{
    public partial class TicketsView : UserControl
    {
        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

        public TicketsView()
        {
            InitializeComponent();
        }

        private async void BtnResolveTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TicketDto ticket && ViewModel != null)
            {
                var confirm = await DeskPilot.Admin.Services.ModalDialogService.ShowConfirmAsync(
                    "Xác Nhận Đóng Phiếu",
                    $"Bạn có chắc chắn đã xử lý xong sự cố cho máy {ticket.MachineName} ({ticket.UserName})?",
                    "Đóng Phiếu", "Hủy");

                if (confirm)
                {
                    await ViewModel.ResolveTicketAsync(ticket.Id);
                }
            }
        }
    }
}

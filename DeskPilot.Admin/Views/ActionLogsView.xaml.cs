using System.Windows;
using System.Windows.Controls;
using DeskPilot.Admin.ViewModels;

namespace DeskPilot.Admin.Views
{
    public partial class ActionLogsView : UserControl
    {
        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

        public ActionLogsView()
        {
            InitializeComponent();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ViewModel.LoadActionLogsAsync();
            }
        }
    }
}

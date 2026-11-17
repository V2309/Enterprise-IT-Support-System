using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DeskPilot.Admin.Services;
using DeskPilot.Admin.ViewModels;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Admin.Views
{
    public partial class MaintenanceView : UserControl
    {
        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;
        private Guid? _editingScheduleId;
        private Guid? _completingScheduleId;

        public MaintenanceView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (ViewModel != null)
                {
                    await ViewModel.LoadMaintenanceSchedulesAsync();
                    if (ViewModel.Devices.Count == 0)
                    {
                        await ViewModel.LoadDevicesAsync();
                    }
                    ModalDevice.ItemsSource = ViewModel.Devices;
                }
            };
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            await ViewModel.LoadMaintenanceSchedulesAsync();
        }

        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            string? dept = (CmbDepartment.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (dept == "Tất cả phòng ban") dept = null;

            string? status = ((CmbStatus.SelectedItem as ComboBoxItem)?.Tag ?? (CmbStatus.SelectedItem as ComboBoxItem)?.Content)?.ToString();
            if (status == "Tất cả trạng thái") status = null;

            bool? isOverdue = ChkOverdueOnly.IsChecked == true ? true : null;
            string? search = string.IsNullOrWhiteSpace(TxtSearch.Text) ? null : TxtSearch.Text.Trim();

            await ViewModel.LoadMaintenanceSchedulesAsync(status, dept, null, search, isOverdue);
        }

        private void BtnAddMaintenance_Click(object sender, RoutedEventArgs e)
        {
            _editingScheduleId = null;
            TxtModalTitle.Text = "Lập Kế Hoạch Bảo Trì Mới";
            ModalTitle.Text = "";
            ModalChecklist.Text = "";
            ModalTechnician.Text = "IT Support";
            ModalScheduledDate.SelectedDate = DateTime.Now.AddDays(7);
            ModalDevice.SelectedItem = null;

            SelectCombo(ModalDepartment, "Kế toán");
            SelectCombo(ModalType, "HardwareCleaning");
            SelectCombo(ModalFrequency, "Quarterly");
            SelectCombo(ModalStatus, "Planned");

            MaintenanceModalBackdrop.Visibility = Visibility.Visible;
        }

        private void BtnEditRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MaintenanceScheduleDto item)
            {
                _editingScheduleId = item.Id;
                TxtModalTitle.Text = $"Cập Nhật Kế Hoạch: {item.Title}";
                ModalTitle.Text = item.Title;
                ModalChecklist.Text = item.ChecklistNotes ?? "";
                ModalTechnician.Text = item.AssignedTechnician;
                ModalScheduledDate.SelectedDate = item.ScheduledDate;

                if (item.DeviceId.HasValue && ViewModel != null)
                {
                    ModalDevice.SelectedItem = ViewModel.Devices.FirstOrDefault(d => d.Id == item.DeviceId.Value);
                }
                else
                {
                    ModalDevice.SelectedItem = null;
                }

                SelectCombo(ModalDepartment, item.Department ?? "Kế toán");
                SelectCombo(ModalType, item.MaintenanceType);
                SelectCombo(ModalFrequency, item.Frequency);
                SelectCombo(ModalStatus, item.Status);

                MaintenanceModalBackdrop.Visibility = Visibility.Visible;
            }
        }

        private async void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            if (sender is Button btn && btn.DataContext is MaintenanceScheduleDto item)
            {
                bool ok = await ModalDialogService.ShowConfirmAsync(
                    "Xác Nhận Xóa Kế Hoạch",
                    $"Bạn có chắc chắn muốn xóa kế hoạch bảo trì '{item.Title}' không?",
                    "Xóa", "Hủy");

                if (ok)
                {
                    var (success, err) = await ViewModel.DeleteMaintenanceScheduleAsync(item.Id);
                    if (success)
                    {
                        await ModalDialogService.ShowSuccessAsync("Thành công", $"Đã xóa kế hoạch bảo trì.");
                    }
                    else
                    {
                        await ModalDialogService.ShowErrorAsync("Lỗi", err ?? "Không thể xóa kế hoạch.");
                    }
                }
            }
        }

        private void BtnCompleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MaintenanceScheduleDto item)
            {
                _completingScheduleId = item.Id;
                TxtCompleteSubtitle.Text = $"Nghiệm thu: {item.Title}";
                CompletePerformedBy.Text = item.AssignedTechnician;
                CompleteResultSummary.Text = item.ResultSummary ?? "";
                CompleteModalBackdrop.Visibility = Visibility.Visible;
            }
        }

        private void ModalBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            MaintenanceModalBackdrop.Visibility = Visibility.Collapsed;
        }

        private async void ModalBtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            string title = ModalTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                await ModalDialogService.ShowWarningAsync("Thiếu thông tin", "Vui lòng nhập Tiêu đề kế hoạch bảo trì.");
                return;
            }

            var selectedDevice = ModalDevice.SelectedItem as DeviceDto;

            var req = new MaintenanceScheduleUpsertRequest
            {
                Title = title,
                DeviceId = selectedDevice?.Id,
                Department = (ModalDepartment.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                MaintenanceType = ((ModalType.SelectedItem as ComboBoxItem)?.Tag ?? (ModalType.SelectedItem as ComboBoxItem)?.Content)?.ToString() ?? "FullInspection",
                Frequency = ((ModalFrequency.SelectedItem as ComboBoxItem)?.Tag ?? (ModalFrequency.SelectedItem as ComboBoxItem)?.Content)?.ToString() ?? "Quarterly",
                ScheduledDate = ModalScheduledDate.SelectedDate ?? DateTime.UtcNow.AddDays(7),
                AssignedTechnician = string.IsNullOrWhiteSpace(ModalTechnician.Text) ? "IT Support" : ModalTechnician.Text.Trim(),
                Status = ((ModalStatus.SelectedItem as ComboBoxItem)?.Tag ?? (ModalStatus.SelectedItem as ComboBoxItem)?.Content)?.ToString() ?? "Planned",
                ChecklistNotes = string.IsNullOrWhiteSpace(ModalChecklist.Text) ? null : ModalChecklist.Text.Trim()
            };

            var (success, err) = await ViewModel.SaveMaintenanceScheduleAsync(_editingScheduleId, req);
            if (success)
            {
                MaintenanceModalBackdrop.Visibility = Visibility.Collapsed;
                await ModalDialogService.ShowSuccessAsync("Thành công", _editingScheduleId.HasValue ? "Đã cập nhật kế hoạch bảo trì." : "Đã tạo kế hoạch bảo trì mới.");
            }
            else
            {
                await ModalDialogService.ShowErrorAsync("Lỗi", err ?? "Không thể lưu kế hoạch bảo trì.");
            }
        }

        private void CompleteBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            CompleteModalBackdrop.Visibility = Visibility.Collapsed;
        }

        private async void CompleteBtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null || !_completingScheduleId.HasValue) return;

            string summary = CompleteResultSummary.Text.Trim();
            if (string.IsNullOrWhiteSpace(summary))
            {
                await ModalDialogService.ShowWarningAsync("Thiếu thông tin", "Vui lòng nhập kết quả nghiệm thu / đánh giá sau bảo trì.");
                return;
            }

            var req = new CompleteMaintenanceRequest
            {
                ResultSummary = summary,
                PerformedBy = CompletePerformedBy.Text.Trim(),
                PerformedDate = DateTime.UtcNow
            };

            var (success, err) = await ViewModel.CompleteMaintenanceScheduleAsync(_completingScheduleId.Value, req);
            if (success)
            {
                CompleteModalBackdrop.Visibility = Visibility.Collapsed;
                await ModalDialogService.ShowSuccessAsync("Thành công", "Đã nghiệm thu và hoàn tất kế hoạch bảo trì.");
            }
            else
            {
                await ModalDialogService.ShowErrorAsync("Lỗi", err ?? "Không thể lưu nghiệm thu.");
            }
        }

        private static void SelectCombo(ComboBox cmb, string target)
        {
            foreach (var item in cmb.Items)
            {
                if (item is ComboBoxItem cbi)
                {
                    var tagVal = cbi.Tag?.ToString();
                    var contentVal = cbi.Content?.ToString();
                    if (string.Equals(tagVal, target, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(contentVal, target, StringComparison.OrdinalIgnoreCase))
                    {
                        cmb.SelectedItem = cbi;
                        return;
                    }
                }
            }
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        }
    }
}

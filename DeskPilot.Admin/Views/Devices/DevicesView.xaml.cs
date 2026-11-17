using System;
using System.Windows;
using System.Windows.Controls;
using DeskPilot.Admin.Services;
using DeskPilot.Admin.ViewModels;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Admin.Views
{
    public partial class DevicesView : UserControl
    {
        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;
        private Guid? _editingDeviceId;

        public DevicesView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (ViewModel != null)
                {
                    await ViewModel.LoadDevicesAsync();
                }
            };
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            await ViewModel.LoadDevicesAsync();
        }

        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            string? dept = (CmbDepartment.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (dept == "Tất cả phòng ban") dept = null;

            string? type = (CmbType.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (type == "Tất cả loại") type = null;

            string? status = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (status == "Tất cả trạng thái") status = null;

            string? search = string.IsNullOrWhiteSpace(TxtSearch.Text) ? null : TxtSearch.Text.Trim();

            await ViewModel.LoadDevicesAsync(dept, status, type, search);
        }

        private void BtnAddDevice_Click(object sender, RoutedEventArgs e)
        {
            _editingDeviceId = null;
            TxtModalTitle.Text = "Thêm Thiết Bị Mới";
            ModalDeviceCode.Text = "";
            ModalName.Text = "";
            ModalAssignedTo.Text = "";
            ModalSerial.Text = "";
            ModalIp.Text = "";
            ModalSpecs.Text = "";

            SelectCombo(ModalType, "Computer");
            SelectCombo(ModalDepartment, "Kế toán");
            SelectCombo(ModalStatus, "InUse");

            DeviceModalBackdrop.Visibility = Visibility.Visible;
        }

        private void BtnEditRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DeviceDto device)
            {
                _editingDeviceId = device.Id;
                TxtModalTitle.Text = $"Cập Nhật Thiết Bị: {device.DeviceCode}";
                ModalDeviceCode.Text = device.DeviceCode;
                ModalName.Text = device.Name;
                ModalAssignedTo.Text = device.AssignedTo ?? "";
                ModalSerial.Text = device.SerialNumber ?? "";
                ModalIp.Text = device.IpAddress ?? "";
                ModalSpecs.Text = device.Specifications ?? "";

                SelectCombo(ModalType, device.DeviceType);
                SelectCombo(ModalDepartment, device.Department ?? "Kế toán");
                SelectCombo(ModalStatus, device.Status);

                DeviceModalBackdrop.Visibility = Visibility.Visible;
            }
        }

        private async void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            if (sender is Button btn && btn.DataContext is DeviceDto device)
            {
                bool ok = await ModalDialogService.ShowConfirmAsync(
                    "Xác Nhận Xóa Thiết Bị",
                    $"Bạn có chắc chắn muốn xóa thiết bị '{device.DeviceCode} - {device.Name}' không?",
                    "Xóa", "Hủy");

                if (ok)
                {
                    var (success, err) = await ViewModel.DeleteDeviceAsync(device.Id);
                    if (success)
                    {
                        await ModalDialogService.ShowSuccessAsync("Thành công", $"Đã xóa thiết bị '{device.DeviceCode}'.");
                    }
                    else
                    {
                        await ModalDialogService.ShowErrorAsync("Lỗi", err ?? "Không thể xóa thiết bị.");
                    }
                }
            }
        }

        private void ModalBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DeviceModalBackdrop.Visibility = Visibility.Collapsed;
        }

        private async void ModalBtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            string code = ModalDeviceCode.Text.Trim();
            string name = ModalName.Text.Trim();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                await ModalDialogService.ShowWarningAsync("Thiếu thông tin", "Vui lòng nhập Mã thiết bị và Tên thiết bị.");
                return;
            }

            var req = new DeviceUpsertRequest
            {
                DeviceCode = code,
                Name = name,
                DeviceType = (ModalType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Computer",
                Department = (ModalDepartment.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                AssignedTo = string.IsNullOrWhiteSpace(ModalAssignedTo.Text) ? null : ModalAssignedTo.Text.Trim(),
                Status = (ModalStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "InUse",
                SerialNumber = string.IsNullOrWhiteSpace(ModalSerial.Text) ? null : ModalSerial.Text.Trim(),
                IpAddress = string.IsNullOrWhiteSpace(ModalIp.Text) ? null : ModalIp.Text.Trim(),
                Specifications = string.IsNullOrWhiteSpace(ModalSpecs.Text) ? null : ModalSpecs.Text.Trim()
            };

            var (success, err) = await ViewModel.SaveDeviceAsync(_editingDeviceId, req);
            if (success)
            {
                DeviceModalBackdrop.Visibility = Visibility.Collapsed;
                await ModalDialogService.ShowSuccessAsync("Thành công", _editingDeviceId.HasValue ? "Đã cập nhật thông tin thiết bị." : "Đã thêm thiết bị mới thành công.");
            }
            else
            {
                await ModalDialogService.ShowErrorAsync("Lỗi", err ?? "Không thể lưu thiết bị.");
            }
        }

        private static void SelectCombo(ComboBox cmb, string target)
        {
            foreach (var item in cmb.Items)
            {
                if (item is ComboBoxItem cbi && string.Equals(cbi.Content?.ToString(), target, StringComparison.OrdinalIgnoreCase))
                {
                    cmb.SelectedItem = cbi;
                    return;
                }
            }
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using DeskPilot.Admin.Services;
using DeskPilot.Admin.ViewModels;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Admin.Views
{
    public partial class ToolsView : UserControl
    {
        private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;
        private Guid? _editingToolId;

        public ToolsView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (ViewModel != null)
                {
                    await ViewModel.LoadToolsAsync();
                }
            };
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            await ViewModel.LoadToolsAsync();
        }

        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            string? cat = (CmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (cat == "Tất cả danh mục") cat = null;

            bool? active = null;
            var statusStr = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (statusStr == "Đang kích hoạt") active = true;
            else if (statusStr == "Đã vô hiệu hóa") active = false;

            string? search = string.IsNullOrWhiteSpace(TxtSearch.Text) ? null : TxtSearch.Text.Trim();

            await ViewModel.LoadToolsAsync(cat, active, search);
        }

        private void BtnAddTool_Click(object sender, RoutedEventArgs e)
        {
            _editingToolId = null;
            TxtModalTitle.Text = "Thêm Công Cụ / Script Mới";
            ModalCode.Text = "";
            ModalName.Text = "";
            ModalDescription.Text = "";
            ModalCommand.Text = "";
            ModalParams.Text = "";
            ModalVersion.Text = "1.0.0";
            ModalAuthor.Text = "IT Admin";
            ModalIsActive.IsChecked = true;

            SelectCombo(ModalCategory, "System");

            ToolModalBackdrop.Visibility = Visibility.Visible;
        }

        private void BtnEditRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ToolDto tool)
            {
                _editingToolId = tool.Id;
                TxtModalTitle.Text = $"Cập Nhật Công Cụ: {tool.Code}";
                ModalCode.Text = tool.Code;
                ModalName.Text = tool.Name;
                ModalDescription.Text = tool.Description;
                ModalCommand.Text = tool.CommandOrScript;
                ModalParams.Text = tool.DefaultParameters ?? "";
                ModalVersion.Text = tool.Version;
                ModalAuthor.Text = tool.Author;
                ModalIsActive.IsChecked = tool.IsActive;

                SelectCombo(ModalCategory, tool.Category);

                ToolModalBackdrop.Visibility = Visibility.Visible;
            }
        }

        private async void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            if (sender is Button btn && btn.DataContext is ToolDto tool)
            {
                bool ok = await ModalDialogService.ShowConfirmAsync(
                    "Xác Nhận Xóa Công Cụ",
                    $"Bạn có chắc chắn muốn xóa công cụ '{tool.Code} - {tool.Name}' không?",
                    "Xóa", "Hủy");

                if (ok)
                {
                    var (success, err) = await ViewModel.DeleteToolAsync(tool.Id);
                    if (success)
                    {
                        await ModalDialogService.ShowSuccessAsync("Thành công", $"Đã xóa công cụ '{tool.Code}'.");
                    }
                    else
                    {
                        await ModalDialogService.ShowErrorAsync("Lỗi", err ?? "Không thể xóa công cụ.");
                    }
                }
            }
        }

        private void ModalBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ToolModalBackdrop.Visibility = Visibility.Collapsed;
        }

        private async void ModalBtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            string code = ModalCode.Text.Trim();
            string name = ModalName.Text.Trim();
            string command = ModalCommand.Text.Trim();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command))
            {
                await ModalDialogService.ShowWarningAsync("Thiếu thông tin", "Vui lòng nhập đầy đủ Mã công cụ, Tên công cụ và Lệnh/Script thực thi.");
                return;
            }

            var req = new ToolUpsertRequest
            {
                Code = code,
                Name = name,
                Category = (ModalCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "System",
                Description = ModalDescription.Text.Trim(),
                CommandOrScript = command,
                DefaultParameters = string.IsNullOrWhiteSpace(ModalParams.Text) ? null : ModalParams.Text.Trim(),
                Version = string.IsNullOrWhiteSpace(ModalVersion.Text) ? "1.0.0" : ModalVersion.Text.Trim(),
                Author = string.IsNullOrWhiteSpace(ModalAuthor.Text) ? "IT Admin" : ModalAuthor.Text.Trim(),
                IsActive = ModalIsActive.IsChecked ?? true
            };

            var (success, err) = await ViewModel.SaveToolAsync(_editingToolId, req);
            if (success)
            {
                ToolModalBackdrop.Visibility = Visibility.Collapsed;
                await ModalDialogService.ShowSuccessAsync("Thành công", _editingToolId.HasValue ? "Đã cập nhật công cụ." : "Đã tạo công cụ mới thành công.");
            }
            else
            {
                await ModalDialogService.ShowErrorAsync("Lỗi", err ?? "Không thể lưu công cụ.");
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

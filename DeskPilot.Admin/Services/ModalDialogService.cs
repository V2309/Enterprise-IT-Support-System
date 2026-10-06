using System;
using System.Threading.Tasks;

namespace DeskPilot.Admin.Services
{
    public enum ModalType
    {
        Info,
        Success,
        Warning,
        Error,
        Confirm
    }

    /// <summary>
    /// Service quản lý hiển thị Hộp thoại In-App Modal Overlay chuẩn Web/SPA cao cấp.
    /// Không bao giờ bật popup Win32 thô kệch.
    /// </summary>
    public static class ModalDialogService
    {
        private static MainWindow? _mainWindow;

        public static void Register(MainWindow window)
        {
            _mainWindow = window;
        }

        public static Task<bool> ShowAsync(string title, string message, ModalType type = ModalType.Info, string confirmText = "Đồng ý", string cancelText = "Hủy")
        {
            if (_mainWindow == null)
            {
                // Fallback nếu chưa khởi tạo
                return Task.FromResult(true);
            }

            return _mainWindow.Dispatcher.Invoke(() => _mainWindow.ShowModalAsync(title, message, type, confirmText, cancelText));
        }

        public static Task ShowInfoAsync(string title, string message) 
            => ShowAsync(title, message, ModalType.Info, "Đồng ý");

        public static Task ShowSuccessAsync(string title, string message) 
            => ShowAsync(title, message, ModalType.Success, "Đồng ý");

        public static Task ShowWarningAsync(string title, string message) 
            => ShowAsync(title, message, ModalType.Warning, "Đã hiểu");

        public static Task ShowErrorAsync(string title, string message) 
            => ShowAsync(title, message, ModalType.Error, "Đóng");

        public static Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Xác nhận", string cancelText = "Hủy") 
            => ShowAsync(title, message, ModalType.Confirm, confirmText, cancelText);
    }
}

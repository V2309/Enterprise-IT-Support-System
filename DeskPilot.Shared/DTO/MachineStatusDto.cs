using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeskPilot.Shared.DTO
{
    // Kế thừa INotifyPropertyChanged để WPF tự phát hiện khi giá trị thuộc tính thay đổi và cập nhật UI
    public class MachineStatusDto : INotifyPropertyChanged
    {
        private string _machineName = string.Empty;
        private string _userName = string.Empty;
        private double _ramUsagePercent;
        private double _freeDiskSpaceGB;
        private bool _isOnline;
        private string _spoolerStatus = "Running";
        private string _statusMessage = "Bình thường";
        private DateTime _lastSeen = DateTime.Now;

        // Tên định danh của máy trạm (ví dụ: PC-KETOAN-01)
        public string MachineName
        {
            get => _machineName;
            set => SetField(ref _machineName, value);
        }

        // Tên tài khoản Windows đang đăng nhập
        public string UserName
        {
            get => _userName;
            set => SetField(ref _userName, value);
        }

        // Phần trăm dung lượng RAM đang bị chiếm dụng
        public double RamUsagePercent
        {
            get => _ramUsagePercent;
            set => SetField(ref _ramUsagePercent, value);
        }

        private double _cpuUsagePercent;
        public double CpuUsagePercent
        {
            get => _cpuUsagePercent;
            set => SetField(ref _cpuUsagePercent, value);
        }

        private double _totalRamGB;
        public double TotalRamGB
        {
            get => _totalRamGB;
            set => SetField(ref _totalRamGB, value);
        }

        private double _usedRamGB;
        public double UsedRamGB
        {
            get => _usedRamGB;
            set => SetField(ref _usedRamGB, value);
        }

        private double _totalDiskSpaceGB;
        public double TotalDiskSpaceGB
        {
            get => _totalDiskSpaceGB;
            set => SetField(ref _totalDiskSpaceGB, value);
        }

        private double _diskUsagePercent;
        public double DiskUsagePercent
        {
            get => _diskUsagePercent;
            set => SetField(ref _diskUsagePercent, value);
        }

        // Dung lượng khả dụng còn lại của ổ C (GB)
        public double FreeDiskSpaceGB
        {
            get => _freeDiskSpaceGB;
            set => SetField(ref _freeDiskSpaceGB, value);
        }

        // Trạng thái kết nối máy (true: Online, false: Mất mạng/Tắt máy)
        public bool IsOnline
        {
            get => _isOnline;
            set => SetField(ref _isOnline, value);
        }

        // Tình trạng tiến trình in ấn (Running / Stopped)
        public string SpoolerStatus
        {
            get => _spoolerStatus;
            set => SetField(ref _spoolerStatus, value);
        }

        // Thông điệp trạng thái tóm tắt (Bình thường / Cảnh báo RAM / Đầy đĩa)
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        // Mốc thời gian gần nhất nhận được tín hiệu nhịp tim
        public DateTime LastSeen
        {
            get => _lastSeen;
            set => SetField(ref _lastSeen, value);
        }

        // Sự kiện thông báo thay đổi dữ liệu cho DataBinding của WPF
        public event PropertyChangedEventHandler? PropertyChanged;

        // Hàm trợ giúp cập nhật giá trị và phát tín hiệu thông báo
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Rút gọn cú pháp gán giá trị và kiểm tra trùng lặp
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
using System;

namespace DeskPilot.Shared.DTO
{
    /// <summary>
    /// Tin nhắn chat giữa IT Admin và nhân viên máy trạm.
    /// </summary>
    public class ChatMessageDto
    {
        public int Id { get; set; }

        /// <summary>Tên máy trạm (định danh cuộc hội thoại)</summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>Tên người gửi (Windows username hoặc tên IT operator)</summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>Nội dung tin nhắn</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Đường link ảnh đính kèm trên ImageKit (nếu có)</summary>
        public string? ImageUrl { get; set; }

        /// <summary>true = IT Admin gửi, false = nhân viên gửi</summary>
        public bool IsFromAdmin { get; set; }

        private DateTime _sentAt = DateTime.UtcNow;

        /// <summary>Thời gian gửi (tự chuyển UTC → Local khi hiển thị)</summary>
        public DateTime SentAt
        {
            get => _sentAt.Kind == DateTimeKind.Utc ? _sentAt.ToLocalTime() : _sentAt;
            set => _sentAt = value;
        }

        /// <summary>Hiển thị giờ:phút cho UI</summary>
        public string TimeDisplay => SentAt.ToString("HH:mm");
    }

    /// <summary>
    /// Tóm tắt cuộc hội thoại (dùng cho danh sách bên trái ChatView).
    /// </summary>
    public class ChatConversationDto : System.ComponentModel.INotifyPropertyChanged
    {
        private string _machineName = string.Empty;
        private string _userName = string.Empty;
        private string _lastMessage = string.Empty;
        private DateTime _lastMessageAt;
        private bool _isOnline;
        private int _unreadCount;

        public string MachineName
        {
            get => _machineName;
            set => SetField(ref _machineName, value);
        }

        public string UserName
        {
            get => _userName;
            set => SetField(ref _userName, value);
        }

        public string LastMessage
        {
            get => _lastMessage;
            set => SetField(ref _lastMessage, value);
        }

        public DateTime LastMessageAt
        {
            get => _lastMessageAt;
            set
            {
                if (SetField(ref _lastMessageAt, value))
                {
                    OnPropertyChanged(nameof(TimeDisplay));
                }
            }
        }

        public bool IsOnline
        {
            get => _isOnline;
            set => SetField(ref _isOnline, value);
        }

        public int UnreadCount
        {
            get => _unreadCount;
            set
            {
                if (SetField(ref _unreadCount, value))
                {
                    OnPropertyChanged(nameof(HasUnread));
                }
            }
        }

        public bool HasUnread => UnreadCount > 0;

        public string TimeDisplay => LastMessageAt.Kind == DateTimeKind.Utc
            ? LastMessageAt.ToLocalTime().ToString("HH:mm")
            : LastMessageAt.ToString("HH:mm");

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

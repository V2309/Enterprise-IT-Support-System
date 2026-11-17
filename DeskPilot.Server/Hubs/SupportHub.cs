using DeskPilot.Server.Security;
using DeskPilot.Server.Services;
using DeskPilot.Shared;
using DeskPilot.Shared.DTO;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeskPilot.Server.Hubs
{
    public class SupportHub : Hub
    {
        // Nhóm chứa toàn bộ kết nối của ứng dụng IT Admin
        public const string AdminsGroup = "admins";

        // Mỗi máy trạm có 1 nhóm riêng => lệnh chỉ gửi đúng tới máy đích
        public static string MachineGroup(string machineName) => $"machine:{machineName.ToUpperInvariant()}";

        private const string RoleKey = "role";
        private const string MachineKey = "machine";
        private const string OperatorKey = "operator";
        private const int MaxDescriptionLength = 2000;

        private readonly ITicketService _ticketService;
        private readonly IActionLogService _actionLogService;
        private readonly IChatService _chatService;
        private readonly MachineStateCache _machineCache;
        private readonly ApiKeyValidator _apiKeyValidator;
        private readonly ILogger<SupportHub> _logger;

        public SupportHub(
            ITicketService ticketService,
            IActionLogService actionLogService,
            IChatService chatService,
            MachineStateCache machineCache,
            ApiKeyValidator apiKeyValidator,
            ILogger<SupportHub> logger)
        {
            _ticketService = ticketService;
            _actionLogService = actionLogService;
            _chatService = chatService;
            _machineCache = machineCache;
            _apiKeyValidator = apiKeyValidator;
            _logger = logger;
        }

        // Xác định vai trò kết nối và đưa vào nhóm tương ứng
        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var role = http is null ? ClientRole.None : _apiKeyValidator.Resolve(http);

            if (role == ClientRole.Admin)
            {
                var operatorName = http!.Request.Headers[HubHeaders.OperatorName].ToString();
                Context.Items[RoleKey] = ClientRole.Admin;
                Context.Items[OperatorKey] = string.IsNullOrWhiteSpace(operatorName) ? "IT Admin" : operatorName.Trim();
                await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
            }
            else if (role == ClientRole.Agent)
            {
                var machineName = http!.Request.Headers[HubHeaders.MachineName].ToString().Trim();
                if (string.IsNullOrEmpty(machineName) || machineName.Length > 100)
                {
                    Context.Abort();
                    return;
                }

                Context.Items[RoleKey] = ClientRole.Agent;
                Context.Items[MachineKey] = machineName;
                await Groups.AddToGroupAsync(Context.ConnectionId, MachineGroup(machineName));
            }
            else
            {
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
        }

        // ===================== AGENT (máy trạm) =====================

        // 1. Nhận nhịp tim từ Client -> cập nhật cache RAM -> báo cho Admin
        public async Task SendHeartbeat(MachineStatusDto status)
        {
            var machineName = RequireAgent();

            // Không tin tên máy client tự khai trong payload, dùng tên đã xác thực lúc kết nối
            status.MachineName = machineName;

            var snapshot = _machineCache.Upsert(status);
            await Clients.Group(AdminsGroup).SendAsync("ReceiveMachineStatus", snapshot);
        }

        // 2. Client gửi ticket báo sự cố -> Lưu PostgreSQL -> Báo sang Admin
        public async Task CreateTicket(string machineName, string userName, string description)
        {
            var authenticatedMachine = RequireAgent();

            description = (description ?? string.Empty).Trim();
            if (description.Length == 0) throw new HubException("Nội dung sự cố không được để trống.");
            if (description.Length > MaxDescriptionLength) description = description[..MaxDescriptionLength];

            _logger.LogInformation("[TICKET NHẬN ĐƯỢC] Từ máy: {MachineName} - User: {UserName}", authenticatedMachine, userName);

            var ticket = await _ticketService.CreateAsync(authenticatedMachine, userName, description);
            await Clients.Group(AdminsGroup).SendAsync("ReceiveNewTicket", ticket);
        }

        // 3. Nhận báo cáo kết quả thực thi từ Client
        public async Task ReportCommandResult(string machineName, string commandName, bool isSuccess, string message)
        {
            var authenticatedMachine = RequireAgent();

            _logger.LogInformation("[ACTION RESULT] Máy {MachineName} thực thi lệnh {CommandName}: {Result} - {Message}",
                authenticatedMachine, commandName, isSuccess ? "THÀNH CÔNG" : "THẤT BẠI", message);

            await Clients.Group(AdminsGroup).SendAsync("ReceiveCommandFeedback", authenticatedMachine, commandName, isSuccess, message);
        }

        // 3.1 Nhân viên gửi tin nhắn Chat tới IT Admin (hỗ trợ kèm ảnh ImageKit)
        public async Task<ChatMessageDto> SendChatMessage(string senderName, string message, string? imageUrl = null)
        {
            var machineName = RequireAgent();
            message = (message ?? string.Empty).Trim();
            imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

            if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(imageUrl))
            {
                throw new HubException("Nội dung tin nhắn hoặc hình ảnh không được để trống.");
            }

            if (string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(imageUrl))
            {
                message = "[Hình ảnh]";
            }

            if (message.Length > MaxDescriptionLength) message = message[..MaxDescriptionLength];

            senderName = string.IsNullOrWhiteSpace(senderName) ? "Nhân viên" : senderName.Trim();

            var chatDto = await _chatService.SaveMessageAsync(machineName, senderName, message, isFromAdmin: false, imageUrl);

            await Clients.Group(AdminsGroup).SendAsync("ReceiveChatMessage", chatDto);
            await Clients.Group(MachineGroup(machineName)).SendAsync("ReceiveChatMessage", chatDto);

            return chatDto;
        }

        // 3.2 Máy trạm lấy lại lịch sử chat của chính máy mình
        public async Task<List<ChatMessageDto>> GetMyChatHistory(int count = 50)
        {
            var machineName = RequireAgent();
            return await _chatService.GetHistoryAsync(machineName, count);
        }

        // ===================== ADMIN =====================

        // 4. Admin phát lệnh xuống đúng máy Client và ghi nhật ký lệnh
        public async Task SendCommandToClient(string targetMachineName, string commandName)
        {
            var operatorName = RequireAdmin();

            var command = RemoteCommands.Normalize(commandName)
                ?? throw new HubException($"Lệnh không hợp lệ: {commandName}");

            if (string.IsNullOrWhiteSpace(targetMachineName) || !_machineCache.IsOnline(targetMachineName))
            {
                throw new HubException($"Máy {targetMachineName} đang offline, không thể gửi lệnh.");
            }

            var log = await _actionLogService.CreateAsync(targetMachineName, command, operatorName);

            await Clients.Group(MachineGroup(targetMachineName)).SendAsync("ExecuteCommand", targetMachineName, command);
            await Clients.Group(AdminsGroup).SendAsync("ReceiveNewActionLog", log);
        }

        // 5. Lấy danh sách máy trạm (kể cả máy đang offline) cho Admin khi vừa mở ứng dụng
        public List<MachineStatusDto> GetMachines()
        {
            RequireAdmin();
            return _machineCache.GetAll();
        }

        // 6. Lấy danh sách các Ticket chưa đóng
        public async Task<List<TicketDto>> GetActiveTickets()
        {
            RequireAdmin();
            return await _ticketService.GetActiveAsync();
        }

        // 7. Đánh dấu hoàn tất một Ticket, lưu vào PostgreSQL và ghi nhật ký thao tác
        public async Task ResolveTicket(int ticketId)
        {
            var operatorName = RequireAdmin();

            var log = await _ticketService.ResolveAsync(ticketId, operatorName);
            if (log != null)
            {
                await Clients.Group(AdminsGroup).SendAsync("TicketResolved", ticketId);
                await Clients.Group(AdminsGroup).SendAsync("ReceiveNewActionLog", log);
            }
        }

        // 8. Lấy danh sách 20 lệnh can thiệp gần nhất
        public async Task<List<ActionLogDto>> GetRecentActionLogs()
        {
            RequireAdmin();
            return await _actionLogService.GetRecentAsync(20);
        }

        // 9. IT Admin trả lời tin nhắn tới một máy cụ thể (hỗ trợ kèm ảnh ImageKit)
        public async Task<ChatMessageDto> ReplyChatMessage(string targetMachineName, string message, string? imageUrl = null)
        {
            var operatorName = RequireAdmin();
            if (string.IsNullOrWhiteSpace(targetMachineName)) throw new HubException("Chưa chọn máy trạm để trả lời.");

            message = (message ?? string.Empty).Trim();
            imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

            if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(imageUrl))
            {
                throw new HubException("Nội dung tin nhắn hoặc hình ảnh không được để trống.");
            }

            if (string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(imageUrl))
            {
                message = "[Hình ảnh]";
            }

            if (message.Length > MaxDescriptionLength) message = message[..MaxDescriptionLength];

            var chatDto = await _chatService.SaveMessageAsync(targetMachineName, operatorName, message, isFromAdmin: true, imageUrl);

            await Clients.Group(MachineGroup(targetMachineName)).SendAsync("ReceiveChatMessage", chatDto);
            await Clients.Group(AdminsGroup).SendAsync("ReceiveChatMessage", chatDto);

            return chatDto;
        }

        // 10. Admin lấy lịch sử chat của một máy trạm
        public async Task<List<ChatMessageDto>> GetChatHistory(string machineName, int count = 50)
        {
            RequireAdmin();
            return await _chatService.GetHistoryAsync(machineName, count);
        }

        // 11. Admin lấy danh sách các cuộc hội thoại
        public async Task<List<ChatConversationDto>> GetChatConversations()
        {
            RequireAdmin();
            var convs = await _chatService.GetConversationsAsync();
            foreach (var c in convs)
            {
                c.IsOnline = _machineCache.IsOnline(c.MachineName);
            }
            return convs;
        }

        // ===================== Helpers =====================

        private string RequireAgent()
        {
            if (Context.Items.TryGetValue(RoleKey, out var role) && role is ClientRole.Agent
                && Context.Items[MachineKey] is string machine)
            {
                return machine;
            }
            throw new HubException("Không có quyền: chỉ Agent máy trạm được gọi chức năng này.");
        }

        private string RequireAdmin()
        {
            if (Context.Items.TryGetValue(RoleKey, out var role) && role is ClientRole.Admin)
            {
                return Context.Items[OperatorKey] as string ?? "IT Admin";
            }
            throw new HubException("Không có quyền: chỉ IT Admin được gọi chức năng này.");
        }
    }
}
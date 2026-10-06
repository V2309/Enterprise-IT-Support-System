using DeskPilot.Server.Data;
using DeskPilot.Shared;
using DeskPilot.Shared.DTO;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DeskPilot.Server.Hubs
{
    public class SupportHub : Hub
    {
        private readonly AppDbContext _db;

        public SupportHub(AppDbContext db)
        {
            _db = db;
        }

        // 1. Nhận nhịp tim từ Client và lưu/cập nhật thông số phần cứng
        public async Task SendHeartbeat(MachineStatusDto status)
        {
            status.IsOnline = true;
            // Dùng UtcNow thay vì DateTime.Now
            status.LastSeen = DateTime.UtcNow;

            var existing = await _db.Machines.FirstOrDefaultAsync(m => m.MachineName == status.MachineName);
            if (existing != null)
            {
                existing.UserName = status.UserName;
                existing.RamUsagePercent = status.RamUsagePercent;
                existing.FreeDiskSpaceGB = status.FreeDiskSpaceGB;
                existing.IsOnline = true;
                existing.SpoolerStatus = status.SpoolerStatus;
                existing.LastSeen = status.LastSeen;
            }
            else
            {
                _db.Machines.Add(new MachineStatusEntity
                {
                    MachineName = status.MachineName,
                    UserName = status.UserName,
                    RamUsagePercent = status.RamUsagePercent,
                    FreeDiskSpaceGB = status.FreeDiskSpaceGB,
                    IsOnline = true,
                    SpoolerStatus = status.SpoolerStatus,
                    LastSeen = status.LastSeen
                });
            }

            await _db.SaveChangesAsync();
            await Clients.Others.SendAsync("ReceiveMachineStatus", status);
        }

        // 2. Client gửi ticket báo sự cố -> Lưu PostgreSQL -> Báo sang Admin
        // Nhận ticket từ Client, lưu vào PostgreSQL và phát sang Admin
        public async Task CreateTicket(string machineName, string userName, string description)
        {
            Console.WriteLine($"[TICKET NHẬN ĐƯỢC] Từ máy: {machineName} - User: {userName} - Nội dung: {description}");

            var ticket = new TicketDto
            {
                MachineName = string.IsNullOrWhiteSpace(machineName) ? "Unknown" : machineName,
                UserName = string.IsNullOrWhiteSpace(userName) ? "Unknown" : userName,
                Description = description,
                Status = "Open",
                CreatedAt = DateTime.UtcNow // Chuẩn UTC cho PostgreSQL
            };

            // 1. Thêm vào DbContext
            await _db.Tickets.AddAsync(ticket);

            // 2. Lưu xuống PostgreSQL
            var rowsAffected = await _db.SaveChangesAsync();
            Console.WriteLine($"[TICKET DB] Đã lưu thành công {rowsAffected} dòng vào bảng Tickets.");

            // 3. Báo sang các máy khác (Admin)
            await Clients.Others.SendAsync("ReceiveNewTicket", ticket);
        }

        // 3. Admin phát lệnh xuống máy Client và ghi nhật ký lệnh
        public async Task SendCommandToClient(string targetMachineName, string commandName)
        {
            var log = new ActionLogDto
            {
                TargetMachine = targetMachineName,
                CommandName = commandName,
                ExecutedBy = "IT Admin",
                ExecutedAt = DateTime.UtcNow
            };
            _db.ActionLogs.Add(log);
            await _db.SaveChangesAsync();

            await Clients.Others.SendAsync("ExecuteCommand", targetMachineName, commandName);
            await Clients.All.SendAsync("ReceiveNewActionLog", log);
        }

        // Lấy danh sách các Ticket chưa đóng (Status != "Resolved") từ PostgreSQL gửi về cho Admin khi vừa mở app
        public async Task<List<TicketDto>> GetActiveTickets()
        {
            return await _db.Tickets
                .Where(t => t.Status != "Resolved")
                .OrderByDescending(t => t.CreatedAt) // Sắp xếp phiếu mới nhất lên trên
                .ToListAsync();
        }

        // Đánh dấu hoàn tất một Ticket, lưu vào PostgreSQL và ghi nhật ký thao tác
        public async Task ResolveTicket(int ticketId)
        {
            var ticket = await _db.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                // Đổi trạng thái thành Resolved (Đã giải quyết)
                ticket.Status = "Resolved";

                // Ghi nhận hành động vào bảng ActionLogs
                var log = new ActionLogDto
                {
                    TargetMachine = ticket.MachineName,
                    CommandName = $"Hoàn tất Ticket #{ticket.Id} ({ticket.Description})",
                    ExecutedBy = "IT Admin",
                    ExecutedAt = DateTime.UtcNow
                };
                _db.ActionLogs.Add(log);

                await _db.SaveChangesAsync();

                // Báo cho các màn hình Admin khác biết để xóa Ticket khỏi bảng
                await Clients.All.SendAsync("TicketResolved", ticketId);

                // Cập nhật ngay tức thì vào bảng ActionLogs trên màn hình Admin
                await Clients.All.SendAsync("ReceiveNewActionLog", log);
            }
        }
        // 1. Nhận báo cáo kết quả thực thi từ Client   
        public async Task ReportCommandResult(string machineName, string commandName, bool isSuccess, string message)
        {
            Console.WriteLine($"[ACTION RESULT] Máy {machineName} thực thi lệnh {commandName}: {(isSuccess ? "THÀNH CÔNG" : "THẤT BẠI")} - {message}");

            // Phát thông báo kết quả sang IT Admin để hiện thông báo popup (Toast)
            await Clients.Others.SendAsync("ReceiveCommandFeedback", machineName, commandName, isSuccess, message);
        }

        // 2. Lấy danh sách 20 lệnh can thiệp gần nhất từ PostgreSQL cho Admin xem lịch sử
        public async Task<List<ActionLogDto>> GetRecentActionLogs()
        {
            return await _db.ActionLogs
                .OrderByDescending(a => a.ExecutedAt)
                .Take(20)
                .ToListAsync();
        }
    }
}
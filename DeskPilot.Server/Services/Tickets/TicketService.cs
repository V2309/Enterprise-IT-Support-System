using DeskPilot.Server.Data;
using DeskPilot.Server.Models;
using DeskPilot.Server.Models.Enums;
using DeskPilot.Shared;
using Microsoft.EntityFrameworkCore;

namespace DeskPilot.Server.Services
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TicketService> _logger;

        public TicketService(AppDbContext db, ILogger<TicketService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<TicketDto> CreateAsync(string machineName, string userName, string description, CancellationToken ct = default)
        {
            var ticket = new Ticket
            {
                MachineName = string.IsNullOrWhiteSpace(machineName) ? "Unknown" : machineName,
                UserName = string.IsNullOrWhiteSpace(userName) ? "Unknown" : userName,
                Description = description ?? string.Empty,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[TICKET] #{Id} từ máy {MachineName} - User {UserName}: {Description}",
                ticket.Id, ticket.MachineName, ticket.UserName, ticket.Description);

            return ToDto(ticket);
        }

        public async Task<List<TicketDto>> GetActiveAsync(CancellationToken ct = default)
        {
            var tickets = await _db.Tickets
                .AsNoTracking()
                .Where(t => t.Status != TicketStatus.Resolved)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);

            return tickets.Select(ToDto).ToList();
        }

        public async Task<ActionLogDto?> ResolveAsync(int ticketId, string resolvedBy = "IT Admin", CancellationToken ct = default)
        {
            var ticket = await _db.Tickets.FindAsync(new object[] { ticketId }, ct);
            if (ticket == null || ticket.Status == TicketStatus.Resolved) return null;

            var now = DateTime.UtcNow;
            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedAt = now;

            // Ghi nhật ký trong cùng 1 transaction (1 lần SaveChanges)
            var log = new ActionLog
            {
                TargetMachine = ticket.MachineName,
                CommandName = $"Hoàn tất Ticket #{ticket.Id} ({ticket.Description})",
                ExecutedBy = resolvedBy,
                ExecutedAt = now
            };
            _db.ActionLogs.Add(log);

            await _db.SaveChangesAsync(ct);

            return ActionLogService.ToDto(log);
        }

        private static TicketDto ToDto(Ticket t) => new()
        {
            Id = t.Id,
            MachineName = t.MachineName,
            UserName = t.UserName,
            Description = t.Description,
            Status = t.Status.ToString(),
            CreatedAt = t.CreatedAt
        };
    }
}

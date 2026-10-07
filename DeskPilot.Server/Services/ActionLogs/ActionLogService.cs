using DeskPilot.Server.Data;
using DeskPilot.Server.Models;
using DeskPilot.Shared;
using Microsoft.EntityFrameworkCore;

namespace DeskPilot.Server.Services
{
    public class ActionLogService : IActionLogService
    {
        private readonly AppDbContext _db;

        public ActionLogService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ActionLogDto> CreateAsync(string targetMachine, string commandName, string executedBy = "IT Admin", CancellationToken ct = default)
        {
            var log = new ActionLog
            {
                TargetMachine = targetMachine,
                CommandName = commandName,
                ExecutedBy = executedBy,
                ExecutedAt = DateTime.UtcNow
            };

            _db.ActionLogs.Add(log);
            await _db.SaveChangesAsync(ct);

            return ToDto(log);
        }

        public async Task<List<ActionLogDto>> GetRecentAsync(int count = 20, CancellationToken ct = default)
        {
            var logs = await _db.ActionLogs
                .AsNoTracking()
                .OrderByDescending(a => a.ExecutedAt)
                .Take(count)
                .ToListAsync(ct);

            return logs.Select(ToDto).ToList();
        }

        internal static ActionLogDto ToDto(ActionLog a) => new()
        {
            Id = a.Id,
            TargetMachine = a.TargetMachine,
            CommandName = a.CommandName,
            ExecutedBy = a.ExecutedBy,
            ExecutedAt = a.ExecutedAt
        };
    }
}

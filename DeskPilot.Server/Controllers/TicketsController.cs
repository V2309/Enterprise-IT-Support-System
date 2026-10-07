using DeskPilot.Server.Hubs;
using DeskPilot.Server.Services;
using DeskPilot.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DeskPilot.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly IHubContext<SupportHub> _hubContext;

        public TicketsController(ITicketService ticketService, IHubContext<SupportHub> hubContext)
        {
            _ticketService = ticketService;
            _hubContext = hubContext;
        }

        // GET api/tickets/active
        [HttpGet("active")]
        public async Task<ActionResult<List<TicketDto>>> GetActive(CancellationToken ct = default)
        {
            var tickets = await _ticketService.GetActiveAsync(ct);
            return Ok(tickets);
        }

        // POST api/tickets/{id}/resolve
        [HttpPost("{id:int}/resolve")]
        public async Task<ActionResult<ActionLogDto>> Resolve(int id, CancellationToken ct = default)
        {
            var log = await _ticketService.ResolveAsync(id, "IT Admin", ct);
            if (log == null)
            {
                return NotFound(new { message = $"Ticket #{id} không tồn tại hoặc đã được giải quyết." });
            }

            // Đồng bộ sang các client SignalR Admin Dashboard
            var admins = _hubContext.Clients.Group(SupportHub.AdminsGroup);
            await admins.SendAsync("TicketResolved", id, cancellationToken: ct);
            await admins.SendAsync("ReceiveNewActionLog", log, cancellationToken: ct);

            return Ok(log);
        }
    }
}

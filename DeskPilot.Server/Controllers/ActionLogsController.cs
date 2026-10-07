using DeskPilot.Server.Services;
using DeskPilot.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DeskPilot.Server.Controllers
{
    [ApiController]
    [Route("api/action-logs")]
    public class ActionLogsController : ControllerBase
    {
        private readonly IActionLogService _actionLogService;

        public ActionLogsController(IActionLogService actionLogService)
        {
            _actionLogService = actionLogService;
        }

        // GET api/action-logs
        [HttpGet]
        public async Task<ActionResult<List<ActionLogDto>>> GetRecent([FromQuery] int count = 20, CancellationToken ct = default)
        {
            var logs = await _actionLogService.GetRecentAsync(count, ct);
            return Ok(logs);
        }
    }
}

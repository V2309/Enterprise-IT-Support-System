using DeskPilot.Server.Services;
using DeskPilot.Shared.DTO;
using Microsoft.AspNetCore.Mvc;

namespace DeskPilot.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MachinesController : ControllerBase
    {
        private readonly MachineStateCache _machineCache;

        public MachinesController(MachineStateCache machineCache)
        {
            _machineCache = machineCache;
        }

        // GET api/machines  (đọc từ cache RAM - luôn là trạng thái mới nhất)
        [HttpGet]
        public ActionResult<List<MachineStatusDto>> GetAll()
        {
            return Ok(_machineCache.GetAll());
        }
    }
}

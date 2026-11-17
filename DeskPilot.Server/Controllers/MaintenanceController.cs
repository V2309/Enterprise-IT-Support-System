using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeskPilot.Server.Services;
using DeskPilot.Shared.DTO;
using Microsoft.AspNetCore.Mvc;

namespace DeskPilot.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        // GET api/maintenance?status=Planned&department=KeToan&isOverdue=true&search=Dell
        [HttpGet]
        public async Task<ActionResult<List<MaintenanceScheduleDto>>> GetAll(
            [FromQuery] string? status,
            [FromQuery] string? department,
            [FromQuery] Guid? deviceId,
            [FromQuery] string? search,
            [FromQuery] bool? isOverdue,
            CancellationToken ct = default)
        {
            var list = await _maintenanceService.GetAllAsync(status, department, deviceId, search, isOverdue, ct);
            return Ok(list);
        }

        // GET api/maintenance/{id:guid}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MaintenanceScheduleDto>> GetById(Guid id, CancellationToken ct = default)
        {
            var item = await _maintenanceService.GetByIdAsync(id, ct);
            if (item == null)
            {
                return NotFound(new { message = $"Kế hoạch bảo trì #{id} không tồn tại." });
            }
            return Ok(item);
        }

        // POST api/maintenance
        [HttpPost]
        public async Task<ActionResult<MaintenanceScheduleDto>> Create([FromBody] MaintenanceScheduleUpsertRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new { message = "Tiêu đề kế hoạch bảo trì (Title) là bắt buộc." });
            }

            var created = await _maintenanceService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT api/maintenance/{id:guid}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<MaintenanceScheduleDto>> Update(Guid id, [FromBody] MaintenanceScheduleUpsertRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new { message = "Tiêu đề kế hoạch bảo trì (Title) là bắt buộc." });
            }

            var updated = await _maintenanceService.UpdateAsync(id, request, ct);
            if (updated == null)
            {
                return NotFound(new { message = $"Kế hoạch bảo trì #{id} không tồn tại." });
            }

            return Ok(updated);
        }

        // POST api/maintenance/{id:guid}/complete
        [HttpPost("{id:guid}/complete")]
        public async Task<ActionResult<MaintenanceScheduleDto>> Complete(Guid id, [FromBody] CompleteMaintenanceRequest request, CancellationToken ct = default)
        {
            var completed = await _maintenanceService.CompleteAsync(id, request, ct);
            if (completed == null)
            {
                return NotFound(new { message = $"Kế hoạch bảo trì #{id} không tồn tại." });
            }

            return Ok(completed);
        }

        // DELETE api/maintenance/{id:guid}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            var success = await _maintenanceService.DeleteAsync(id, ct);
            if (!success)
            {
                return NotFound(new { message = $"Kế hoạch bảo trì #{id} không tồn tại." });
            }

            return NoContent();
        }
    }
}

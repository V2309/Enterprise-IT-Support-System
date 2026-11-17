using DeskPilot.Server.Services;
using DeskPilot.Shared.DTO;
using Microsoft.AspNetCore.Mvc;

namespace DeskPilot.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DevicesController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        // GET api/devices?department=KeToan&status=InUse&deviceType=Computer&search=Dell
        [HttpGet]
        public async Task<ActionResult<List<DeviceDto>>> GetAll(
            [FromQuery] string? department,
            [FromQuery] string? status,
            [FromQuery] string? deviceType,
            [FromQuery] string? search,
            CancellationToken ct = default)
        {
            var devices = await _deviceService.GetAllAsync(department, status, deviceType, search, ct);
            return Ok(devices);
        }

        // GET api/devices/{id:guid}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DeviceDto>> GetById(Guid id, CancellationToken ct = default)
        {
            var device = await _deviceService.GetByIdAsync(id, ct);
            if (device == null)
            {
                return NotFound(new { message = $"Thiết bị với ID '{id}' không tồn tại." });
            }
            return Ok(device);
        }

        // POST api/devices
        [HttpPost]
        public async Task<ActionResult<DeviceDto>> Create([FromBody] DeviceUpsertRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceCode) || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Mã thiết bị (DeviceCode) và Tên thiết bị (Name) là bắt buộc." });
            }

            var existing = await _deviceService.GetByCodeAsync(request.DeviceCode, ct);
            if (existing != null)
            {
                return Conflict(new { message = $"Mã thiết bị '{request.DeviceCode}' đã tồn tại trong hệ thống." });
            }

            var created = await _deviceService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT api/devices/{id:guid}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<DeviceDto>> Update(Guid id, [FromBody] DeviceUpsertRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceCode) || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Mã thiết bị (DeviceCode) và Tên thiết bị (Name) là bắt buộc." });
            }

            var existingWithCode = await _deviceService.GetByCodeAsync(request.DeviceCode, ct);
            if (existingWithCode != null && existingWithCode.Id != id)
            {
                return Conflict(new { message = $"Mã thiết bị '{request.DeviceCode}' đã được sử dụng bởi thiết bị khác." });
            }

            var updated = await _deviceService.UpdateAsync(id, request, ct);
            if (updated == null)
            {
                return NotFound(new { message = $"Thiết bị với ID '{id}' không tồn tại." });
            }

            return Ok(updated);
        }

        // DELETE api/devices/{id:guid}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            var success = await _deviceService.DeleteAsync(id, ct);
            if (!success)
            {
                return NotFound(new { message = $"Thiết bị với ID '{id}' không tồn tại." });
            }

            return NoContent();
        }
    }
}

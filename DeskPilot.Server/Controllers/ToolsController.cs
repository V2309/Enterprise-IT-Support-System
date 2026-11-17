using DeskPilot.Server.Services;
using DeskPilot.Shared.DTO;
using Microsoft.AspNetCore.Mvc;

namespace DeskPilot.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToolsController : ControllerBase
    {
        private readonly IToolService _toolService;

        public ToolsController(IToolService toolService)
        {
            _toolService = toolService;
        }

        // GET api/tools?category=System&isActive=true&search=Dns
        [HttpGet]
        public async Task<ActionResult<List<ToolDto>>> GetAll(
            [FromQuery] string? category,
            [FromQuery] bool? isActive,
            [FromQuery] string? search,
            CancellationToken ct = default)
        {
            var tools = await _toolService.GetAllAsync(category, isActive, search, ct);
            return Ok(tools);
        }

        // GET api/tools/{id:guid}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ToolDto>> GetById(Guid id, CancellationToken ct = default)
        {
            var tool = await _toolService.GetByIdAsync(id, ct);
            if (tool == null)
            {
                return NotFound(new { message = $"Công cụ với ID '{id}' không tồn tại." });
            }
            return Ok(tool);
        }

        // GET api/tools/by-code/{code}
        [HttpGet("by-code/{code}")]
        public async Task<ActionResult<ToolDto>> GetByCode(string code, CancellationToken ct = default)
        {
            var tool = await _toolService.GetByCodeAsync(code, ct);
            if (tool == null)
            {
                return NotFound(new { message = $"Công cụ với mã '{code}' không tồn tại." });
            }
            return Ok(tool);
        }

        // POST api/tools
        [HttpPost]
        public async Task<ActionResult<ToolDto>> Create([FromBody] ToolUpsertRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.CommandOrScript))
            {
                return BadRequest(new { message = "Mã công cụ (Code), Tên công cụ (Name) và Lệnh/Script (CommandOrScript) là bắt buộc." });
            }

            var existing = await _toolService.GetByCodeAsync(request.Code, ct);
            if (existing != null)
            {
                return Conflict(new { message = $"Mã công cụ '{request.Code}' đã tồn tại trong hệ thống." });
            }

            var created = await _toolService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT api/tools/{id:guid}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ToolDto>> Update(Guid id, [FromBody] ToolUpsertRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.CommandOrScript))
            {
                return BadRequest(new { message = "Mã công cụ (Code), Tên công cụ (Name) và Lệnh/Script (CommandOrScript) là bắt buộc." });
            }

            var existingWithCode = await _toolService.GetByCodeAsync(request.Code, ct);
            if (existingWithCode != null && existingWithCode.Id != id)
            {
                return Conflict(new { message = $"Mã công cụ '{request.Code}' đã được sử dụng bởi công cụ khác." });
            }

            var updated = await _toolService.UpdateAsync(id, request, ct);
            if (updated == null)
            {
                return NotFound(new { message = $"Công cụ với ID '{id}' không tồn tại." });
            }

            return Ok(updated);
        }

        // DELETE api/tools/{id:guid}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            var success = await _toolService.DeleteAsync(id, ct);
            if (!success)
            {
                return NotFound(new { message = $"Công cụ với ID '{id}' không tồn tại." });
            }

            return NoContent();
        }
    }
}

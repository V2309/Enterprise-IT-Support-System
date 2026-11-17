using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Server.Services
{
    public interface IMaintenanceService
    {
        Task<List<MaintenanceScheduleDto>> GetAllAsync(
            string? status = null,
            string? department = null,
            Guid? deviceId = null,
            string? search = null,
            bool? isOverdue = null,
            CancellationToken ct = default);

        Task<MaintenanceScheduleDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<MaintenanceScheduleDto> CreateAsync(MaintenanceScheduleUpsertRequest request, CancellationToken ct = default);

        Task<MaintenanceScheduleDto?> UpdateAsync(Guid id, MaintenanceScheduleUpsertRequest request, CancellationToken ct = default);

        Task<MaintenanceScheduleDto?> CompleteAsync(Guid id, CompleteMaintenanceRequest request, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

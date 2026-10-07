using DeskPilot.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DeskPilot.Server.Services
{
    // Tiến trình chạy ngầm:
    //  - Quét định kỳ để phát hiện máy mất kết nối (dựa trên cache trong RAM)
    //  - Gom và ghi trạng thái máy xuống PostgreSQL theo lô
    public class HeartbeatMonitorService : BackgroundService
    {
        private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<SupportHub> _hubContext;
        private readonly MachineStateCache _cache;
        private readonly ILogger<HeartbeatMonitorService> _logger;

        public HeartbeatMonitorService(
            IServiceScopeFactory scopeFactory,
            IHubContext<SupportHub> hubContext,
            MachineStateCache cache,
            ILogger<HeartbeatMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LoadInitialStateAsync(stoppingToken);

            var lastFlush = DateTime.UtcNow;
            using var timer = new PeriodicTimer(ScanInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        var offlineMachines = _cache.MarkTimedOut(HeartbeatTimeout);
                        foreach (var machineName in offlineMachines)
                        {
                            _logger.LogInformation("[OFFLINE] Máy {MachineName} đã mất kết nối.", machineName);
                            await _hubContext.Clients.Group(SupportHub.AdminsGroup)
                                .SendAsync("MachineWentOffline", machineName, stoppingToken);
                        }

                        // Ghi ngay khi có máy offline, còn lại gom theo chu kỳ FlushInterval
                        if (offlineMachines.Count > 0 || DateTime.UtcNow - lastFlush >= FlushInterval)
                        {
                            await FlushAsync(stoppingToken);
                            lastFlush = DateTime.UtcNow;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "[HeartbeatMonitor Error]");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Server đang dừng
            }
            finally
            {
                // Lưu nốt dữ liệu còn lại trước khi tắt
                try { await FlushAsync(CancellationToken.None); }
                catch (Exception ex) { _logger.LogError(ex, "[HeartbeatMonitor] Lỗi lưu dữ liệu khi tắt Server"); }
            }
        }

        private async Task LoadInitialStateAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var machineService = scope.ServiceProvider.GetRequiredService<IMachineService>();

                await machineService.MarkAllOfflineAsync(ct);
                _cache.Seed(await machineService.GetAllAsync(ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[HeartbeatMonitor] Không tải được danh sách máy từ DB");
            }
        }

        private async Task FlushAsync(CancellationToken ct)
        {
            var dirty = _cache.TakeDirty();
            if (dirty.Count == 0) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var machineService = scope.ServiceProvider.GetRequiredService<IMachineService>();
                await machineService.SaveSnapshotsAsync(dirty, ct);
            }
            catch
            {
                _cache.MarkDirty(dirty.Select(d => d.MachineName));
                throw;
            }
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeskPilot.Server.Data;
using DeskPilot.Server.Hubs;
using DeskPilot.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DeskPilot.Server
{
    // Tiến trình chạy ngầm quét định kỳ để phát hiện máy mất kết nối
    public class HeartbeatMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<SupportHub> _hubContext;

        public HeartbeatMonitorService(IServiceProvider serviceProvider, IHubContext<SupportHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Dùng DateTime.UtcNow thay vì DateTime.Now
                    var threshold = DateTime.UtcNow.AddSeconds(-10);

                    // Tìm các máy đang Online nhưng LastSeen đã quá thời hạn
                    var timedOutMachines = await db.Machines
                        .Where(m => m.IsOnline && m.LastSeen < threshold)
                        .ToListAsync(stoppingToken);

                    foreach (var machine in timedOutMachines)
                    {
                        // Đổi trạng thái trong cơ sở dữ liệu
                        machine.IsOnline = false;

                        // Thông báo sang Admin để đổi màu Card thành ngoại tuyến
                        await _hubContext.Clients.All.SendAsync("MachineWentOffline", machine.MachineName, stoppingToken);
                        Console.WriteLine($"[OFFLINE] Máy {machine.MachineName} đã mất kết nối.");
                    }

                    if (timedOutMachines.Any())
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HeartbeatMonitor Error] {ex.Message}");
                }

                // Quét mỗi 5 giây 1 lần
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
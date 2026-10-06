

using DeskPilot.Server;
using DeskPilot.Server.Data;
using DeskPilot.Server.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình kết nối PostgreSQL qua Npgsql
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Đăng ký dịch vụ SignalR và cấu hình CORS
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

builder.Services.AddControllers();

// Đăng ký tiến trình chạy ngầm giám sát kết nối máy trạm
builder.Services.AddHostedService<HeartbeatMonitorService>();

// Khởi tạo app từ builder (Tất cả cấu hình DI Service phải nằm TRÊN dòng này)
var app = builder.Build();



// 3. Tự động tạo Database và các Bảng trong PostgreSQL nếu chưa có (Phải nằm DƯỚI dòng builder.Build())
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 4. Kích hoạt Middleware, CORS và định tuyến
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// 5. Đăng ký Endpoint cho SignalR Hub
app.MapHub<SupportHub>("/supportHub");

// Endpoint kiểm tra nhanh tình trạng Server trên trình duyệt
app.MapGet("/", () => "DeskPilot Server is running with PostgreSQL...");

app.Run();
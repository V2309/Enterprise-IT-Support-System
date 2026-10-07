using DeskPilot.Server.Data;
using DeskPilot.Server.Hubs;
using DeskPilot.Server.Security;
using DeskPilot.Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 0. File cấu hình bí mật cục bộ (đã nằm trong .gitignore). Biến môi trường vẫn được ưu tiên cao nhất.
builder.Configuration
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 1. Cấu hình kết nối PostgreSQL qua Npgsql với chuẩn đặt tên snake_case
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Thiếu ConnectionStrings:DefaultConnection. Khai báo trong appsettings.local.json, user-secrets " +
        "hoặc biến môi trường ConnectionStrings__DefaultConnection.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

// 2. Xác thực bằng API key (Admin / Agent)
var apiKeyOptions = builder.Configuration.GetSection(ApiKeyOptions.SectionName).Get<ApiKeyOptions>() ?? new ApiKeyOptions();
builder.Services.AddSingleton(new ApiKeyValidator(apiKeyOptions));

// 3. Đăng ký các nghiệp vụ logic (Services) vào DI Container
builder.Services.AddSingleton<MachineStateCache>();
builder.Services.AddScoped<IMachineService, MachineService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IActionLogService, ActionLogService>();

// 4. Đăng ký dịch vụ SignalR và cấu hình CORS
// Admin/Client là ứng dụng desktop (không gửi Origin) nên CORS chỉ cần cho các origin web được khai báo.
builder.Services.AddSignalR();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 5. Đăng ký Controller tiếp nhận REST API
builder.Services.AddControllers();

// 6. Tiến trình chạy ngầm: giám sát heartbeat + ghi trạng thái máy xuống DB theo lô
builder.Services.AddHostedService<HeartbeatMonitorService>();

var app = builder.Build();

// 7. Chỉ tự động migrate khi được bật (mặc định bật ở Development, tắt ở Production)
if (app.Configuration.GetValue("Database:MigrateOnStartup", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// 8. Kích hoạt Middleware, CORS, xác thực API key và định tuyến
const string HubPath = "/supportHub";
app.UseCors();
app.UseApiKeyProtection(HubPath);
app.MapControllers();

// 9. Đăng ký Endpoint cho SignalR Hub
app.MapHub<SupportHub>(HubPath);

// 10. Endpoint kiểm tra nhanh tình trạng Server trên trình duyệt
app.MapGet("/", () => "DeskPilot Server is running with PostgreSQL...");

app.Run();
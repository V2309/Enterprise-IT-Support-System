using System.Security.Cryptography;
using System.Text;
using DeskPilot.Shared;

namespace DeskPilot.Server.Security
{
    public enum ClientRole
    {
        None,
        Admin,
        Agent
    }

    // Cấu hình khóa API (đọc từ section "Auth" - KHÔNG commit giá trị thật vào git)
    public class ApiKeyOptions
    {
        public const string SectionName = "Auth";

        // Khóa dành cho ứng dụng IT Admin (toàn quyền: gửi lệnh, đóng ticket, xem log)
        public string AdminApiKey { get; set; } = string.Empty;

        // Khóa dành cho Agent cài trên máy trạm (chỉ gửi heartbeat, ticket, báo kết quả lệnh)
        public string AgentApiKey { get; set; } = string.Empty;
    }

    // Xác định vai trò của kết nối dựa trên API key (so sánh thời gian hằng để tránh timing attack)
    public class ApiKeyValidator
    {
        private readonly byte[] _adminKey;
        private readonly byte[] _agentKey;

        public ApiKeyValidator(ApiKeyOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.AdminApiKey) || string.IsNullOrWhiteSpace(options.AgentApiKey))
            {
                throw new InvalidOperationException(
                    "Thiếu cấu hình Auth:AdminApiKey / Auth:AgentApiKey. " +
                    "Hãy khai báo trong appsettings.local.json, user-secrets hoặc biến môi trường (Auth__AdminApiKey, Auth__AgentApiKey).");
            }

            if (options.AdminApiKey == options.AgentApiKey)
            {
                throw new InvalidOperationException("Auth:AdminApiKey và Auth:AgentApiKey phải khác nhau.");
            }

            _adminKey = Encoding.UTF8.GetBytes(options.AdminApiKey);
            _agentKey = Encoding.UTF8.GetBytes(options.AgentApiKey);
        }

        public ClientRole Resolve(string? apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return ClientRole.None;

            var provided = Encoding.UTF8.GetBytes(apiKey);
            if (CryptographicOperations.FixedTimeEquals(provided, _adminKey)) return ClientRole.Admin;
            if (CryptographicOperations.FixedTimeEquals(provided, _agentKey)) return ClientRole.Agent;
            return ClientRole.None;
        }

        public ClientRole Resolve(HttpContext context)
        {
            var key = context.Request.Headers[HubHeaders.ApiKey].ToString();

            // Fallback query string (?api_key=...) cho các transport không gửi được header
            if (string.IsNullOrEmpty(key))
            {
                key = context.Request.Query["api_key"].ToString();
            }

            return Resolve(key);
        }
    }

    public static class ApiKeyMiddlewareExtensions
    {
        // Chặn sớm (401/403) các request không hợp lệ tới REST API và SignalR Hub
        public static IApplicationBuilder UseApiKeyProtection(this IApplicationBuilder app, string hubPath)
        {
            return app.Use(async (context, next) =>
            {
                var path = context.Request.Path;
                var isApi = path.StartsWithSegments("/api");
                var isHub = path.StartsWithSegments(hubPath);

                if (!isApi && !isHub)
                {
                    await next();
                    return;
                }

                var validator = context.RequestServices.GetRequiredService<ApiKeyValidator>();
                var role = validator.Resolve(context);

                if (role == ClientRole.None)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                // REST API chỉ dành cho Admin
                if (isApi && role != ClientRole.Admin)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                await next();
            });
        }
    }
}

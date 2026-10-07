namespace DeskPilot.Shared
{
    // Danh sách lệnh điều khiển từ xa hợp lệ (dùng chung cho Server / Client / Admin)
    public static class RemoteCommands
    {
        public const string RestartSpooler = "RestartSpooler";
        public const string FlushDns = "FlushDns";

        // Chuẩn hóa tên lệnh (chấp nhận alias cũ như "FixPrint", "FlushDNS"). Trả về null nếu không hợp lệ.
        public static string? Normalize(string? commandName) => commandName?.Trim().ToLowerInvariant() switch
        {
            "restartspooler" or "fixprint" => RestartSpooler,
            "flushdns" => FlushDns,
            _ => null
        };
    }

    // Tên các HTTP header dùng khi kết nối SignalR Hub
    public static class HubHeaders
    {
        public const string ApiKey = "X-Api-Key";
        public const string MachineName = "X-Machine-Name";
        public const string OperatorName = "X-Operator-Name";
    }
}

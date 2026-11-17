using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DeskPilot.Shared;
using DeskPilot.Shared.DTO;

namespace DeskPilot.Admin.Services
{
    public class AdminApiClient
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminApiClient(string serverUrl, string apiKey)
        {
            // Parse base address từ HubUrl (ví dụ http://localhost:5000/supportHub -> http://localhost:5000/)
            var uri = new Uri(serverUrl);
            var baseUri = $"{uri.Scheme}://{uri.Authority}";

            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUri),
                Timeout = TimeSpan.FromSeconds(10)
            };

            if (!string.IsNullOrEmpty(apiKey))
            {
                _http.DefaultRequestHeaders.Add(HubHeaders.ApiKey, apiKey);
            }
        }

        // ================= DEVICES API =================

        public async Task<List<DeviceDto>> GetDevicesAsync(string? department = null, string? status = null, string? deviceType = null, string? search = null, CancellationToken ct = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(department) && department != "Tất cả")
                query.Add($"department={Uri.EscapeDataString(department)}");
            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
                query.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(deviceType) && deviceType != "Tất cả")
                query.Add($"deviceType={Uri.EscapeDataString(deviceType)}");
            if (!string.IsNullOrWhiteSpace(search))
                query.Add($"search={Uri.EscapeDataString(search)}");

            var url = "/api/devices" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            var res = await _http.GetFromJsonAsync<List<DeviceDto>>(url, _jsonOptions, ct);
            return res ?? new List<DeviceDto>();
        }

        public async Task<(bool Success, string? ErrorMessage, DeviceDto? Data)> CreateDeviceAsync(DeviceUpsertRequest req, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("/api/devices", req, _jsonOptions, ct);
            if (res.IsSuccessStatusCode)
            {
                var data = await res.Content.ReadFromJsonAsync<DeviceDto>(_jsonOptions, ct);
                return (true, null, data);
            }

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err, null);
        }

        public async Task<(bool Success, string? ErrorMessage, DeviceDto? Data)> UpdateDeviceAsync(Guid id, DeviceUpsertRequest req, CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync($"/api/devices/{id}", req, _jsonOptions, ct);
            if (res.IsSuccessStatusCode)
            {
                var data = await res.Content.ReadFromJsonAsync<DeviceDto>(_jsonOptions, ct);
                return (true, null, data);
            }

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteDeviceAsync(Guid id, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"/api/devices/{id}", ct);
            if (res.IsSuccessStatusCode) return (true, null);

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err);
        }

        // ================= TOOLS API =================

        public async Task<List<ToolDto>> GetToolsAsync(string? category = null, bool? isActive = null, string? search = null, CancellationToken ct = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
                query.Add($"category={Uri.EscapeDataString(category)}");
            if (isActive.HasValue)
                query.Add($"isActive={isActive.Value}");
            if (!string.IsNullOrWhiteSpace(search))
                query.Add($"search={Uri.EscapeDataString(search)}");

            var url = "/api/tools" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            var res = await _http.GetFromJsonAsync<List<ToolDto>>(url, _jsonOptions, ct);
            return res ?? new List<ToolDto>();
        }

        public async Task<(bool Success, string? ErrorMessage, ToolDto? Data)> CreateToolAsync(ToolUpsertRequest req, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("/api/tools", req, _jsonOptions, ct);
            if (res.IsSuccessStatusCode)
            {
                var data = await res.Content.ReadFromJsonAsync<ToolDto>(_jsonOptions, ct);
                return (true, null, data);
            }

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err, null);
        }

        public async Task<(bool Success, string? ErrorMessage, ToolDto? Data)> UpdateToolAsync(Guid id, ToolUpsertRequest req, CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync($"/api/tools/{id}", req, _jsonOptions, ct);
            if (res.IsSuccessStatusCode)
            {
                var data = await res.Content.ReadFromJsonAsync<ToolDto>(_jsonOptions, ct);
                return (true, null, data);
            }

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteToolAsync(Guid id, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"/api/tools/{id}", ct);
            if (res.IsSuccessStatusCode) return (true, null);

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err);
        }

        // ================= MAINTENANCE API =================

        public async Task<List<MaintenanceScheduleDto>> GetMaintenanceSchedulesAsync(
            string? status = null,
            string? department = null,
            Guid? deviceId = null,
            string? search = null,
            bool? isOverdue = null,
            CancellationToken ct = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
                query.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(department) && department != "Tất cả")
                query.Add($"department={Uri.EscapeDataString(department)}");
            if (deviceId.HasValue)
                query.Add($"deviceId={deviceId.Value}");
            if (!string.IsNullOrWhiteSpace(search))
                query.Add($"search={Uri.EscapeDataString(search)}");
            if (isOverdue.HasValue)
                query.Add($"isOverdue={isOverdue.Value}");

            var url = "/api/maintenance" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            var res = await _http.GetFromJsonAsync<List<MaintenanceScheduleDto>>(url, _jsonOptions, ct);
            return res ?? new List<MaintenanceScheduleDto>();
        }

        public async Task<(bool Success, string? ErrorMessage, MaintenanceScheduleDto? Data)> CreateMaintenanceScheduleAsync(MaintenanceScheduleUpsertRequest req, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("/api/maintenance", req, _jsonOptions, ct);
            if (res.IsSuccessStatusCode)
            {
                var data = await res.Content.ReadFromJsonAsync<MaintenanceScheduleDto>(_jsonOptions, ct);
                return (true, null, data);
            }

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err, null);
        }

        public async Task<(bool Success, string? ErrorMessage, MaintenanceScheduleDto? Data)> UpdateMaintenanceScheduleAsync(Guid id, MaintenanceScheduleUpsertRequest req, CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync($"/api/maintenance/{id}", req, _jsonOptions, ct);
            if (res.IsSuccessStatusCode)
            {
                var data = await res.Content.ReadFromJsonAsync<MaintenanceScheduleDto>(_jsonOptions, ct);
                return (true, null, data);
            }

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err, null);
        }

        public async Task<(bool Success, string? ErrorMessage, MaintenanceScheduleDto? Data)> CompleteMaintenanceScheduleAsync(Guid id, CompleteMaintenanceRequest req, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync($"/api/maintenance/{id}/complete", req, _jsonOptions, ct);
            if (res.IsSuccessStatusCode)
            {
                var data = await res.Content.ReadFromJsonAsync<MaintenanceScheduleDto>(_jsonOptions, ct);
                return (true, null, data);
            }

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteMaintenanceScheduleAsync(Guid id, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"/api/maintenance/{id}", ct);
            if (res.IsSuccessStatusCode) return (true, null);

            var err = await TryExtractErrorMessage(res, ct);
            return (false, err);
        }

        // ================= CHAT IMAGE UPLOAD API =================

        public async Task<(bool Success, string? ErrorMessage, string? Url)> UploadChatImageStreamAsync(System.IO.Stream stream, string fileName, CancellationToken ct = default)
        {
            try
            {
                using var form = new MultipartFormDataContent();
                var streamContent = new StreamContent(stream);
                form.Add(streamContent, "file", fileName);

                var res = await _http.PostAsync("/api/chat/upload", form, ct);
                var content = await res.Content.ReadAsStringAsync(ct);

                if (!res.IsSuccessStatusCode)
                    return (false, $"Lỗi server ({res.StatusCode}): {content}", null);

                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("url", out var urlEl))
                {
                    return (true, null, urlEl.GetString());
                }

                return (false, "Không tìm thấy URL hình ảnh trong phản hồi.", null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string? ErrorMessage, string? Url)> UploadChatImageAsync(string filePath, CancellationToken ct = default)
        {
            if (!System.IO.File.Exists(filePath))
                return (false, "File không tồn tại.", null);

            using var stream = System.IO.File.OpenRead(filePath);
            return await UploadChatImageStreamAsync(stream, System.IO.Path.GetFileName(filePath), ct);
        }

        private static async Task<string> TryExtractErrorMessage(HttpResponseMessage res, CancellationToken ct)
        {
            try
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                {
                    return msg.GetString() ?? $"Lỗi {(int)res.StatusCode}: {res.ReasonPhrase}";
                }
            }
            catch { }

            return $"Lỗi {(int)res.StatusCode}: {res.ReasonPhrase}";
        }
    }
}

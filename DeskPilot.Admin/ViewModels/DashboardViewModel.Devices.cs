using DeskPilot.Shared.DTO;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DeskPilot.Admin.ViewModels
{
    public partial class DashboardViewModel
    {
        public ObservableCollection<DeviceDto> Devices { get; set; } = new();

        public async Task LoadDevicesAsync(string? department = null, string? status = null, string? type = null, string? search = null)
        {
            try
            {
                var list = await _apiClient.GetDevicesAsync(department, status, type, search);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Devices.Clear();
                    foreach (var d in list)
                    {
                        Devices.Add(d);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải Devices: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Error)> SaveDeviceAsync(Guid? id, DeviceUpsertRequest req)
        {
            try
            {
                if (id.HasValue && id.Value != Guid.Empty)
                {
                    var res = await _apiClient.UpdateDeviceAsync(id.Value, req);
                    if (res.Success && res.Data != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var idx = Devices.ToList().FindIndex(x => x.Id == id.Value);
                            if (idx >= 0) Devices[idx] = res.Data;
                        });
                        return (true, null);
                    }
                    return (false, res.ErrorMessage);
                }
                else
                {
                    var res = await _apiClient.CreateDeviceAsync(req);
                    if (res.Success && res.Data != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Devices.Insert(0, res.Data);
                        });
                        return (true, null);
                    }
                    return (false, res.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> DeleteDeviceAsync(Guid id)
        {
            try
            {
                var res = await _apiClient.DeleteDeviceAsync(id);
                if (res.Success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var item = Devices.FirstOrDefault(x => x.Id == id);
                        if (item != null) Devices.Remove(item);
                    });
                    return (true, null);
                }
                return (false, res.ErrorMessage);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}

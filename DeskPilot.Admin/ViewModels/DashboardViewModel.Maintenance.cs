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
        public ObservableCollection<MaintenanceScheduleDto> MaintenanceSchedules { get; set; } = new();

        public async Task LoadMaintenanceSchedulesAsync(string? status = null, string? department = null, Guid? deviceId = null, string? search = null, bool? isOverdue = null)
        {
            try
            {
                var list = await _apiClient.GetMaintenanceSchedulesAsync(status, department, deviceId, search, isOverdue);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MaintenanceSchedules.Clear();
                    foreach (var m in list)
                    {
                        MaintenanceSchedules.Add(m);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải MaintenanceSchedules: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Error)> SaveMaintenanceScheduleAsync(Guid? id, MaintenanceScheduleUpsertRequest req)
        {
            try
            {
                if (id.HasValue && id.Value != Guid.Empty)
                {
                    var res = await _apiClient.UpdateMaintenanceScheduleAsync(id.Value, req);
                    if (res.Success && res.Data != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var idx = MaintenanceSchedules.ToList().FindIndex(x => x.Id == id.Value);
                            if (idx >= 0) MaintenanceSchedules[idx] = res.Data;
                        });
                        return (true, null);
                    }
                    return (false, res.ErrorMessage);
                }
                else
                {
                    var res = await _apiClient.CreateMaintenanceScheduleAsync(req);
                    if (res.Success && res.Data != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MaintenanceSchedules.Insert(0, res.Data);
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

        public async Task<(bool Success, string? Error)> CompleteMaintenanceScheduleAsync(Guid id, CompleteMaintenanceRequest req)
        {
            try
            {
                var res = await _apiClient.CompleteMaintenanceScheduleAsync(id, req);
                if (res.Success && res.Data != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var idx = MaintenanceSchedules.ToList().FindIndex(x => x.Id == id);
                        if (idx >= 0) MaintenanceSchedules[idx] = res.Data;
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

        public async Task<(bool Success, string? Error)> DeleteMaintenanceScheduleAsync(Guid id)
        {
            try
            {
                var res = await _apiClient.DeleteMaintenanceScheduleAsync(id);
                if (res.Success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var item = MaintenanceSchedules.FirstOrDefault(x => x.Id == id);
                        if (item != null) MaintenanceSchedules.Remove(item);
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

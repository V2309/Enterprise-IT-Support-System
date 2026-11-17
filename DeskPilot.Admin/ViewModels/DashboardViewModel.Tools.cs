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
        public ObservableCollection<ToolDto> Tools { get; set; } = new();

        public async Task LoadToolsAsync(string? category = null, bool? isActive = null, string? search = null)
        {
            try
            {
                var list = await _apiClient.GetToolsAsync(category, isActive, search);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Tools.Clear();
                    foreach (var t in list)
                    {
                        Tools.Add(t);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải Tools: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Error)> SaveToolAsync(Guid? id, ToolUpsertRequest req)
        {
            try
            {
                if (id.HasValue && id.Value != Guid.Empty)
                {
                    var res = await _apiClient.UpdateToolAsync(id.Value, req);
                    if (res.Success && res.Data != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var idx = Tools.ToList().FindIndex(x => x.Id == id.Value);
                            if (idx >= 0) Tools[idx] = res.Data;
                        });
                        return (true, null);
                    }
                    return (false, res.ErrorMessage);
                }
                else
                {
                    var res = await _apiClient.CreateToolAsync(req);
                    if (res.Success && res.Data != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Tools.Insert(0, res.Data);
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

        public async Task<(bool Success, string? Error)> DeleteToolAsync(Guid id)
        {
            try
            {
                var res = await _apiClient.DeleteToolAsync(id);
                if (res.Success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var item = Tools.FirstOrDefault(x => x.Id == id);
                        if (item != null) Tools.Remove(item);
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

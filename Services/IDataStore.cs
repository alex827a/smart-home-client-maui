using SmartHome2.Models;

namespace SmartHome2.Services
{
    public interface IDataStore
    {
        Task SaveMetricsAsync(MetricsDto m);
        Task<MetricsDto?> LoadMetricsAsync();
        Task SaveDevicesAsync(IEnumerable<DeviceDto> d);
        Task<List<DeviceDto>> LoadDevicesAsync();
        
        // Metric history
        Task SaveMetricHistoryAsync(MetricsDto m);
        Task<List<MetricsDto>> LoadMetricHistoryAsync(int maxCount = 50);
        Task ClearOldMetricsAsync(int keepCount = 50);
        Task ClearDevicesAsync();
        Task ClearMetricHistoryAsync();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartHome2.Models;

namespace SmartHome2.Services
{
    public interface IApiClient
    {
        Task<MetricsDto> GetMetricsAsync(CancellationToken ct = default);
        Task<List<DeviceDto>> GetDevicesAsync(CancellationToken ct = default);
        Task<DeviceDto> ToggleDeviceAsync(string id, CancellationToken ct = default);
        Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default) where T : class;
    }
}

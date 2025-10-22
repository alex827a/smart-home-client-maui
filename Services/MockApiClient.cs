using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartHome2.Models;


namespace SmartHome2.Services
{
    public class MockApiClient : IApiClient
    {
        readonly Random _rnd = new();
        
        public Task<MetricsDto> GetMetricsAsync(CancellationToken ct = default)
        {
            var dto = new MetricsDto(
                Temp: Math.Round(20 + _rnd.NextDouble() * 6, 1),
                Humidity: _rnd.Next(35, 56),
                Power: _rnd.Next(280, 361),
                Ts: DateTime.Now.ToString("O")
            );
            return Task.FromResult(dto);
        }

        public Task<List<DeviceDto>> GetDevicesAsync(CancellationToken ct = default)
        {
            var devices = new List<DeviceDto>
            {
                new DeviceDto("lamp", "Living Room Light", _rnd.NextDouble() > 0.5, DateTime.Now.ToString("O")),
                new DeviceDto("fan", "Kitchen Fan", _rnd.NextDouble() > 0.5, DateTime.Now.ToString("O")),
                new DeviceDto("hvac", "Bedroom AC", _rnd.NextDouble() > 0.5, DateTime.Now.ToString("O")),
                new DeviceDto("lock", "Front Door Lock", _rnd.NextDouble() > 0.5, DateTime.Now.ToString("O")),
                new DeviceDto("door", "Garage Door", _rnd.NextDouble() > 0.5, DateTime.Now.ToString("O"))
            };
            return Task.FromResult(devices);
        }

        public Task<DeviceDto> ToggleDeviceAsync(string id, CancellationToken ct = default)
        {
            // Mock toggle - just return the device with flipped state
            var device = new DeviceDto(id, $"Mock {id}", _rnd.NextDouble() > 0.5, DateTime.Now.ToString("O"));
            return Task.FromResult(device);
        }
    }
}

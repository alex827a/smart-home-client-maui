using System.Net.Http.Json;
using SmartHome2.Models;
using System.Text.Json;

namespace SmartHome2.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiClient(HttpClient http)
        {
            _http = http;
            _http.BaseAddress = new Uri(AppSettings.BaseUrl);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<MetricsDto> GetMetricsAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync("api/metrics", ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var metrics = JsonSerializer.Deserialize<MetricsDto>(content, _jsonOptions);
                return metrics ?? throw new InvalidOperationException("Failed to deserialize metrics");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient: GetMetricsAsync failed - {ex.Message}");
                throw;
            }
        }

        public async Task<List<DeviceDto>> GetDevicesAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync("api/devices", ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var devices = JsonSerializer.Deserialize<List<DeviceDto>>(content, _jsonOptions);
                return devices ?? new List<DeviceDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient: GetDevicesAsync failed - {ex.Message}");
                throw;
            }
        }

        public async Task<DeviceDto> ToggleDeviceAsync(string id, CancellationToken ct = default)
        {
            try
            {
                var response = await _http.PostAsync($"api/devices/{id}/toggle", null, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var device = JsonSerializer.Deserialize<DeviceDto>(content, _jsonOptions);
                return device ?? throw new InvalidOperationException("Failed to deserialize device");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient: ToggleDeviceAsync failed - {ex.Message}");
                throw;
            }
        }
    }
}
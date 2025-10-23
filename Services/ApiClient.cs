using System.Net.Http.Json;
using SmartHome2.Models;
using System.Text.Json;
using System.Text;

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
            
            // ????????????? Basic Auth ????????? ??? ??????? credentials
            UpdateAuthHeaders();
        }

        /// <summary>
        /// ????????? ????????? ??????????? ?? ?????? ??????? credentials
        /// </summary>
        private void UpdateAuthHeaders()
        {
            _http.DefaultRequestHeaders.Authorization = null;
            
            var username = AppSettings.MqttUsername;
            var password = AppSettings.MqttPassword;
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                System.Diagnostics.Debug.WriteLine($"ApiClient: Basic Auth configured for user '{username}'");
            }
        }

        public async Task<MetricsDto> GetMetricsAsync(CancellationToken ct = default)
        {
            try
            {
                UpdateAuthHeaders(); // ????????? ????????? ????? ????????
                var response = await _http.GetAsync("api/metrics", ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var metrics = JsonSerializer.Deserialize<MetricsDto>(content, _jsonOptions);
                return metrics ?? throw new InvalidOperationException("Failed to deserialize metrics");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient: Unauthorized (401) - check credentials");
                throw new UnauthorizedAccessException("Invalid credentials", ex);
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
                UpdateAuthHeaders();
                var response = await _http.GetAsync("api/devices", ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var devices = JsonSerializer.Deserialize<List<DeviceDto>>(content, _jsonOptions);
                return devices ?? new List<DeviceDto>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient: Unauthorized (401) - check credentials");
                throw new UnauthorizedAccessException("Invalid credentials", ex);
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
                UpdateAuthHeaders();
                var response = await _http.PostAsync($"api/devices/{id}/toggle", null, ct).ConfigureAwait(false);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("User does not have permission to control devices. Admin role required.");
                }
                
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var device = JsonSerializer.Deserialize<DeviceDto>(content, _jsonOptions);
                return device ?? throw new InvalidOperationException("Failed to deserialize device");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient: Unauthorized (401) - check credentials");
                throw new UnauthorizedAccessException("Invalid credentials", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient: ToggleDeviceAsync failed - {ex.Message}");
                throw;
            }
        }

        public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default) where T : class
        {
            try
            {
                UpdateAuthHeaders();
                var response = await _http.GetAsync(endpoint, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return result;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient: Unauthorized (401) - check credentials");
                throw new UnauthorizedAccessException("Invalid credentials", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient: GetAsync<{typeof(T).Name}> failed - {ex.Message}");
                throw;
            }
        }
    }
}
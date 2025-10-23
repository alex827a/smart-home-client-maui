using SmartHome2.Models;

namespace SmartHome2.Services
{
    /// <summary>
    /// Unified real-time service that automatically falls back to SSE when MQTT is unavailable
    /// </summary>
    public interface IRealtimeService
    {
        event EventHandler<MetricsDto>? MetricsReceived;
        event EventHandler<DeviceDto>? DeviceStateReceived;
        event EventHandler<string>? ConnectionStatusChanged;
        
        Task StartAsync();
        Task StopAsync();
        bool IsConnected { get; }
        string CurrentMode { get; } // "mqtt", "sse", or "disconnected"
    }

    public class RealtimeService : IRealtimeService, IAsyncDisposable
    {
        private readonly IMqttService _mqttService;
        private readonly ISseService _sseService;
        private readonly IApiClient _apiClient;
        private bool _disposed = false;
        private string _currentMode = "disconnected";
        private readonly SemaphoreSlim _switchLock = new(1, 1);

        public event EventHandler<MetricsDto>? MetricsReceived;
        public event EventHandler<DeviceDto>? DeviceStateReceived;
        public event EventHandler<string>? ConnectionStatusChanged;

        public bool IsConnected => _mqttService.IsConnected || _sseService.IsConnected;
        public string CurrentMode => _currentMode;

        public RealtimeService(IMqttService mqttService, ISseService sseService, IApiClient apiClient)
        {
            _mqttService = mqttService;
            _sseService = sseService;
            _apiClient = apiClient;

            // Subscribe to MQTT events
            _mqttService.MetricsReceived += OnMqttMetricsReceived;
            _mqttService.DeviceStateReceived += OnMqttDeviceStateReceived;
            _mqttService.ConnectionStatusChanged += OnMqttConnectionStatusChanged;

            // Subscribe to SSE events
            _sseService.MetricsReceived += OnSseMetricsReceived;
            _sseService.DeviceStateReceived += OnSseDeviceStateReceived;
            _sseService.ConnectionStatusChanged += OnSseConnectionStatusChanged;

            System.Diagnostics.Debug.WriteLine("RealtimeService: Instance created");
        }

        public async Task StartAsync()
        {
            await _switchLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(RealtimeService));

                System.Diagnostics.Debug.WriteLine("RealtimeService: Starting...");

                // ALWAYS try MQTT first if TLS is enabled
                if (AppSettings.MqttUseTls && !string.IsNullOrEmpty(AppSettings.MqttUsername))
                {
                    System.Diagnostics.Debug.WriteLine("RealtimeService: Attempting MQTT connection...");
                    await StartMqttAsync().ConfigureAwait(false);
                    
                    // Wait a bit to see if MQTT connects
                    await Task.Delay(2000).ConfigureAwait(false);
                    
                    if (_mqttService.IsConnected)
                    {
                        System.Diagnostics.Debug.WriteLine("RealtimeService: MQTT connected successfully");
                        _currentMode = "mqtt";
                        return; // Success!
                    }
                    
                    System.Diagnostics.Debug.WriteLine("RealtimeService: MQTT connection failed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("RealtimeService: MQTT disabled or no credentials");
                }

                // MQTT failed or disabled - check if SSE fallback is available
                System.Diagnostics.Debug.WriteLine("RealtimeService: Checking SSE fallback availability...");
                var serverStatus = await CheckServerStatusAsync().ConfigureAwait(false);
                
                if (serverStatus != null)
                {
                    System.Diagnostics.Debug.WriteLine($"RealtimeService: Server status - MQTT: {serverStatus.MqttAvailable}, Recommended: {serverStatus.RecommendedMode}");
                    
                    // Try SSE fallback
                    System.Diagnostics.Debug.WriteLine("RealtimeService: Attempting SSE fallback...");
                    await StartSseAsync().ConfigureAwait(false);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("RealtimeService: Server unreachable - no fallback available");
                    _currentMode = "disconnected";
                    ConnectionStatusChanged?.Invoke(this, "Failed: Server unreachable");
                }
            }
            finally
            {
                _switchLock.Release();
            }
        }

        private async Task<ServerStatus?> CheckServerStatusAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync<ServerStatus>("api/status").ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"RealtimeService: Server status - MQTT: {response?.MqttAvailable}, Recommended: {response?.RecommendedMode}");
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RealtimeService: Failed to check server status - {ex.Message}");
                return null;
            }
        }

        private async Task StartMqttAsync()
        {
            try
            {
                await _mqttService.StartAsync().ConfigureAwait(false);
                if (_mqttService.IsConnected)
                {
                    _currentMode = "mqtt";
                    System.Diagnostics.Debug.WriteLine("RealtimeService: MQTT mode active");
                }
                else
                {
                    // MQTT failed, fallback to SSE
                    System.Diagnostics.Debug.WriteLine("RealtimeService: MQTT failed, falling back to SSE");
                    await StartSseAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RealtimeService: MQTT start failed - {ex.Message}");
                await StartSseAsync().ConfigureAwait(false);
            }
        }

        private async Task StartSseAsync()
        {
            try
            {
                await _sseService.StartAsync().ConfigureAwait(false);
                _currentMode = "sse";
                System.Diagnostics.Debug.WriteLine("RealtimeService: SSE mode active");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RealtimeService: SSE start failed - {ex.Message}");
                _currentMode = "disconnected";
                ConnectionStatusChanged?.Invoke(this, "Failed: All connection methods unavailable");
            }
        }

        public async Task StopAsync()
        {
            await _switchLock.WaitAsync().ConfigureAwait(false);
            try
            {
                System.Diagnostics.Debug.WriteLine("RealtimeService: Stopping...");

                var mqttStopTask = _mqttService.StopAsync();
                var sseStopTask = _sseService.StopAsync();

                await Task.WhenAll(mqttStopTask, sseStopTask).ConfigureAwait(false);

                _currentMode = "disconnected";
                System.Diagnostics.Debug.WriteLine("RealtimeService: Stopped");
            }
            finally
            {
                _switchLock.Release();
            }
        }

        // MQTT event handlers
        private void OnMqttMetricsReceived(object? sender, MetricsDto metrics)
        {
            if (_currentMode == "mqtt")
            {
                MetricsReceived?.Invoke(this, metrics);
            }
        }

        private void OnMqttDeviceStateReceived(object? sender, DeviceDto device)
        {
            if (_currentMode == "mqtt")
            {
                DeviceStateReceived?.Invoke(this, device);
            }
        }

        private async void OnMqttConnectionStatusChanged(object? sender, string status)
        {
            System.Diagnostics.Debug.WriteLine($"RealtimeService: MQTT status changed - {status}");
            
            // Just report status, don't auto-fallback to SSE
            // User can manually retry which will trigger SSE fallback if needed
            if (_currentMode == "mqtt")
            {
                ConnectionStatusChanged?.Invoke(this, $"MQTT: {status}");
                
                if (status == "Disconnected")
                {
                    _currentMode = "disconnected";
                }
            }
        }

        // SSE event handlers
        private void OnSseMetricsReceived(object? sender, MetricsDto metrics)
        {
            if (_currentMode == "sse")
            {
                MetricsReceived?.Invoke(this, metrics);
            }
        }

        private void OnSseDeviceStateReceived(object? sender, DeviceDto device)
        {
            if (_currentMode == "sse")
            {
                DeviceStateReceived?.Invoke(this, device);
            }
        }

        private void OnSseConnectionStatusChanged(object? sender, string status)
        {
            System.Diagnostics.Debug.WriteLine($"RealtimeService: SSE status changed - {status}");
            
            if (_currentMode == "sse")
            {
                ConnectionStatusChanged?.Invoke(this, $"SSE Fallback: {status}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            System.Diagnostics.Debug.WriteLine("RealtimeService: Disposing...");
            _disposed = true;

            // Unsubscribe from events
            _mqttService.MetricsReceived -= OnMqttMetricsReceived;
            _mqttService.DeviceStateReceived -= OnMqttDeviceStateReceived;
            _mqttService.ConnectionStatusChanged -= OnMqttConnectionStatusChanged;

            _sseService.MetricsReceived -= OnSseMetricsReceived;
            _sseService.DeviceStateReceived -= OnSseDeviceStateReceived;
            _sseService.ConnectionStatusChanged -= OnSseConnectionStatusChanged;

            await StopAsync().ConfigureAwait(false);
            _switchLock.Dispose();

            System.Diagnostics.Debug.WriteLine("RealtimeService: Disposed");
        }

        private class ServerStatus
        {
            public bool MqttAvailable { get; set; }
            public string? MqttBroker { get; set; }
            public int MqttPort { get; set; }
            public bool MqttTls { get; set; }
            public int SseClientsCount { get; set; }
            public string? RecommendedMode { get; set; }
            public string? Timestamp { get; set; }
        }
    }
}

using System.Text;
using System.Text.Json;
using SmartHome2.Models;

namespace SmartHome2.Services
{
    /// <summary>
    /// Server-Sent Events service for fallback mode when MQTT is unavailable
    /// </summary>
    public interface ISseService
    {
        event EventHandler<MetricsDto>? MetricsReceived;
        event EventHandler<DeviceDto>? DeviceStateReceived;
        event EventHandler<string>? ConnectionStatusChanged;
        
        Task StartAsync();
        Task StopAsync();
        bool IsConnected { get; }
    }

    public class SseService : ISseService, IAsyncDisposable
    {
        private readonly IDataStore _store;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cts;
        private Task? _receiveTask;
        private bool _disposed = false;
        private bool _isConnected = false;

        public event EventHandler<MetricsDto>? MetricsReceived;
        public event EventHandler<DeviceDto>? DeviceStateReceived;
        public event EventHandler<string>? ConnectionStatusChanged;

        public bool IsConnected => _isConnected;

        public SseService(IDataStore store, HttpClient httpClient)
        {
            _store = store;
            _httpClient = httpClient;
            _httpClient.Timeout = Timeout.InfiniteTimeSpan; // SSE needs infinite timeout
            System.Diagnostics.Debug.WriteLine("SseService: Instance created");
        }

        public async Task StartAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SseService));

            if (_isConnected)
            {
                System.Diagnostics.Debug.WriteLine("SseService: Already connected");
                return;
            }

            System.Diagnostics.Debug.WriteLine("SseService: Starting SSE connection...");
            
            _cts = new CancellationTokenSource();
            _receiveTask = ReceiveEventsAsync(_cts.Token);
        }

        private async Task ReceiveEventsAsync(CancellationToken cancellationToken)
        {
            var sseUrl = $"{AppSettings.BaseUrl}api/events/stream";
            System.Diagnostics.Debug.WriteLine($"SseService: Connecting to {sseUrl}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, sseUrl);
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
                    
                    // ????????? Basic Authentication ?????????
                    var username = AppSettings.MqttUsername;
                    var password = AppSettings.MqttPassword;
                    
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                        System.Diagnostics.Debug.WriteLine($"SseService: Using Basic Auth for user '{username}'");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("SseService: WARNING - No credentials configured!");
                    }

                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("SseService: Unauthorized (401) - Invalid credentials");
                        ConnectionStatusChanged?.Invoke(this, "Unauthorized - Check credentials");
                        await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        System.Diagnostics.Debug.WriteLine("SseService: Forbidden (403) - Access denied");
                        ConnectionStatusChanged?.Invoke(this, "Forbidden - Access denied");
                        await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"SseService: Failed to connect - {response.StatusCode}");
                        ConnectionStatusChanged?.Invoke(this, "Failed");
                        await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine("SseService: Connected");
                    _isConnected = true;
                    ConnectionStatusChanged?.Invoke(this, "Connected");

                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    var dataBuffer = new StringBuilder();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        
                        if (line == null)
                        {
                            // Stream ended
                            break;
                        }

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            // Empty line indicates end of event
                            if (dataBuffer.Length > 0)
                            {
                                await ProcessEventAsync(dataBuffer.ToString()).ConfigureAwait(false);
                                dataBuffer.Clear();
                            }
                            continue;
                        }

                        if (line.StartsWith("data: "))
                        {
                            dataBuffer.Append(line.Substring(6));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("SseService: Cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SseService: Error - {ex.Message}");
                    _isConnected = false;
                    ConnectionStatusChanged?.Invoke(this, "Disconnected");
                    
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine("SseService: Reconnecting in 5 seconds...");
                        await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            _isConnected = false;
            System.Diagnostics.Debug.WriteLine("SseService: Stopped");
        }

        private async Task ProcessEventAsync(string eventData)
        {
            try
            {
                var sseEvent = JsonSerializer.Deserialize<SseEvent>(eventData);
                if (sseEvent == null) return;

                System.Diagnostics.Debug.WriteLine($"SseService: Event received - Topic: {sseEvent.Topic}");

                // Handle different event types based on topic
                if (sseEvent.Topic == "system/connection" || sseEvent.Topic == "system/keepalive")
                {
                    // Connection status events
                    return;
                }

                if (sseEvent.Topic == "system/initial-state")
                {
                    // Initial state with devices list
                    if (sseEvent.Payload != null)
                    {
                        var stateData = JsonSerializer.Deserialize<InitialStatePayload>(
                            JsonSerializer.Serialize(sseEvent.Payload));
                        
                        if (stateData?.Devices != null)
                        {
                            foreach (var device in stateData.Devices)
                            {
                                DeviceStateReceived?.Invoke(this, device);
                            }
                        }
                    }
                    return;
                }

                // Handle metrics events (home/*/metrics or home/system/metrics)
                if (sseEvent.Topic?.Contains("/metrics") == true)
                {
                    if (sseEvent.Payload != null)
                    {
                        var metrics = JsonSerializer.Deserialize<MetricsDto>(
                            JsonSerializer.Serialize(sseEvent.Payload));
                        
                        if (metrics != null)
                        {
                            MetricsReceived?.Invoke(this, metrics);
                            
                            // Save to history (fire-and-forget)
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _store.SaveMetricHistoryAsync(metrics).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"SseService: Failed to save metric history - {ex.Message}");
                                }
                            });
                        }
                    }
                }
                // Handle device state events (home/*/state)
                else if (sseEvent.Topic?.Contains("/state") == true)
                {
                    if (sseEvent.Payload != null)
                    {
                        var device = JsonSerializer.Deserialize<DeviceDto>(
                            JsonSerializer.Serialize(sseEvent.Payload));
                        
                        if (device != null)
                        {
                            DeviceStateReceived?.Invoke(this, device);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SseService: Failed to process event - {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            if (!_isConnected && _cts == null)
            {
                System.Diagnostics.Debug.WriteLine("SseService: Already stopped");
                return;
            }

            System.Diagnostics.Debug.WriteLine("SseService: Stopping...");
            
            _cts?.Cancel();
            
            if (_receiveTask != null)
            {
                try
                {
                    await _receiveTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SseService: Error during stop - {ex.Message}");
                }
            }

            _cts?.Dispose();
            _cts = null;
            _receiveTask = null;
            _isConnected = false;

            System.Diagnostics.Debug.WriteLine("SseService: Stopped");
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            System.Diagnostics.Debug.WriteLine("SseService: Disposing...");
            _disposed = true;

            await StopAsync().ConfigureAwait(false);
            _httpClient.Dispose();

            System.Diagnostics.Debug.WriteLine("SseService: Disposed");
        }

        // Helper classes for SSE event deserialization
        private class SseEvent
        {
            public string? Topic { get; set; }
            public object? Payload { get; set; }
            public string? Timestamp { get; set; }
        }

        private class InitialStatePayload
        {
            public List<DeviceDto>? Devices { get; set; }
            public string? Timestamp { get; set; }
        }
    }
}

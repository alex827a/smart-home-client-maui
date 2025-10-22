using MQTTnet;
using MQTTnet.Client;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using SmartHome2.Models;

namespace SmartHome2.Services
{
    public interface IMqttService
    {
        event EventHandler<MetricsDto>? MetricsReceived;
        event EventHandler<DeviceDto>? DeviceStateReceived;
        event EventHandler<string>? ConnectionStatusChanged;
        
        Task StartAsync();
        Task StopAsync();
        bool IsConnected { get; }
    }

    public class MqttService : IMqttService, IAsyncDisposable
    {
        private IMqttClient? _mqttClient;
        private readonly IDataStore _store;
        private CancellationTokenSource? _reconnectCts;
        private Task? _reconnectTask;
        private bool _disposed = false;
        private readonly SemaphoreSlim _startStopLock = new SemaphoreSlim(1, 1);

        public event EventHandler<MetricsDto>? MetricsReceived;
        public event EventHandler<DeviceDto>? DeviceStateReceived;
        public event EventHandler<string>? ConnectionStatusChanged;

        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        public MqttService(IDataStore store)
        {
            _store = store;
            System.Diagnostics.Debug.WriteLine("MqttService: Instance created");
        }

        public async Task StartAsync()
        {
            await _startStopLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(MqttService));

                if (_mqttClient?.IsConnected == true)
                {
                    System.Diagnostics.Debug.WriteLine("MqttService: Already connected");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("MqttService: Starting...");
                System.Diagnostics.Debug.WriteLine($"MqttService: Broker={AppSettings.MqttBroker}:{AppSettings.MqttPort}");
                System.Diagnostics.Debug.WriteLine($"MqttService: Username={AppSettings.MqttUsername}");
                System.Diagnostics.Debug.WriteLine($"MqttService: UseTLS={AppSettings.MqttUseTls}");
                System.Diagnostics.Debug.WriteLine($"MqttService: UseClientCerts={AppSettings.MqttUseClientCerts}");

                var factory = new MqttFactory();
                _mqttClient = factory.CreateMqttClient();

                MqttClientTlsOptions? tlsOptions = null;

                if (AppSettings.MqttUseTls)
                {
                    tlsOptions = new MqttClientTlsOptions
                    {
                        UseTls = true,
                        AllowUntrustedCertificates = true,
                        IgnoreCertificateChainErrors = true,
                        IgnoreCertificateRevocationErrors = true,
                        SslProtocol = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                        CertificateValidationHandler = context =>
                        {
                            System.Diagnostics.Debug.WriteLine($"MqttService: Server certificate - Subject={context.Certificate?.Subject}, Issuer={context.Certificate?.Issuer}");
                            return true;
                        }
                    };

                    if (AppSettings.MqttUseClientCerts)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("MqttService: Loading client certificates...");
                            
                            X509Certificate2? certificate = null;
                            
                            try
                            {
                                var pfxBytes = await LoadCertificateBytesAsync("client.pfx").ConfigureAwait(false);
                                certificate = new X509Certificate2(pfxBytes, "", X509KeyStorageFlags.Exportable);
                                System.Diagnostics.Debug.WriteLine($"MqttService: PFX certificate loaded - Subject={certificate.Subject}");
                            }
                            catch (Exception pfxEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"MqttService: PFX not found or invalid - {pfxEx.Message}");
                                System.Diagnostics.Debug.WriteLine("MqttService: Trying PEM format...");
                                
                                var clientCert = await LoadCertificateAsync("client-cert.pem").ConfigureAwait(false);
                                var clientKey = await LoadCertificateAsync("client-key.pem").ConfigureAwait(false);
                                certificate = X509Certificate2.CreateFromPem(clientCert, clientKey);
                                System.Diagnostics.Debug.WriteLine($"MqttService: PEM certificate loaded - Subject={certificate.Subject}");
                            }
                            
                            if (certificate != null)
                            {
                                tlsOptions.ClientCertificatesProvider = new DefaultMqttCertificatesProvider(new List<X509Certificate2> { certificate });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"MqttService: Failed to load client certificates - {ex.Message}");
                            System.Diagnostics.Debug.WriteLine("MqttService: Continuing without client certificates");
                        }
                    }
                }

                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId($"SmartHome2_{Guid.NewGuid()}")
                    .WithTcpServer(AppSettings.MqttBroker, AppSettings.MqttPort)
                    .WithCredentials(AppSettings.MqttUsername, AppSettings.MqttPassword)
                    .WithCleanSession()
                    .WithTimeout(TimeSpan.FromSeconds(15));

                if (tlsOptions != null)
                {
                    optionsBuilder = optionsBuilder.WithTlsOptions(tlsOptions);
                }

                var options = optionsBuilder.Build();

                // Subscribe to events
                _mqttClient.ConnectedAsync += OnConnectedAsync;
                _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
                _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

                System.Diagnostics.Debug.WriteLine($"MqttService: Attempting connection...");
                await _mqttClient.ConnectAsync(options).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"MqttService: Connection successful!");

                // Start reconnect task
                _reconnectCts = new CancellationTokenSource();
                _reconnectTask = ReconnectLoopAsync(_reconnectCts.Token);

                System.Diagnostics.Debug.WriteLine("MqttService: Started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MqttService StartAsync failed: {ex}");
                System.Diagnostics.Debug.WriteLine($"MqttService Exception details: {ex.InnerException?.Message}");
                ConnectionStatusChanged?.Invoke(this, $"Failed: {ex.Message}");
            }
            finally
            {
                _startStopLock.Release();
            }
        }

        private async Task<byte[]> LoadCertificateBytesAsync(string filename)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(filename).ConfigureAwait(false);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            return memoryStream.ToArray();
        }

        private async Task<string> LoadCertificateAsync(string filename)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(filename).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                    
                    if (_mqttClient != null && !_mqttClient.IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine("MqttService: Attempting reconnect...");
                        try
                        {
                            await _mqttClient.ReconnectAsync(cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"MqttService: Reconnect failed - {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("MqttService: Reconnect loop cancelled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MqttService: Reconnect loop error - {ex.Message}");
            }
        }

        private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("MqttService: Connected to broker");
            ConnectionStatusChanged?.Invoke(this, "Connected");

            // Subscribe to topics
            await _mqttClient!.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic("home/+/metrics")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                .Build()).ConfigureAwait(false);

            await _mqttClient!.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic("home/+/state")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build()).ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine("MqttService: Subscribed to topics");
        }

        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"MqttService: Disconnected - {args.Reason}");
            ConnectionStatusChanged?.Invoke(this, "Disconnected");
            return Task.CompletedTask;
        }

        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            try
            {
                var topic = args.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

                System.Diagnostics.Debug.WriteLine($"MqttService: Message received on {topic}: {payload}");

                if (topic.Contains("/metrics"))
                {
                    var metrics = JsonSerializer.Deserialize<MetricsDto>(payload);
                    if (metrics != null)
                    {
                        MetricsReceived?.Invoke(this, metrics);
                        // Save to history (fire-and-forget with ConfigureAwait)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _store.SaveMetricHistoryAsync(metrics).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to save metric history: {ex.Message}");
                            }
                        });
                    }
                }
                else if (topic.Contains("/state"))
                {
                    var device = JsonSerializer.Deserialize<DeviceDto>(payload);
                    if (device != null)
                    {
                        DeviceStateReceived?.Invoke(this, device);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MqttService: Failed to process message - {ex}");
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            await _startStopLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_mqttClient == null)
                {
                    System.Diagnostics.Debug.WriteLine("MqttService: Already stopped");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("MqttService: Stopping...");
                
                // Cancel reconnect loop
                _reconnectCts?.Cancel();
                
                if (_reconnectTask != null)
                {
                    try
                    {
                        await _reconnectTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"MqttService: Error waiting for reconnect task - {ex.Message}");
                    }
                    finally
                    {
                        _reconnectTask = null;
                    }
                }

                // Unsubscribe from events
                _mqttClient.ConnectedAsync -= OnConnectedAsync;
                _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;
                _mqttClient.ApplicationMessageReceivedAsync -= OnMessageReceivedAsync;

                // Disconnect
                await _mqttClient.DisconnectAsync().ConfigureAwait(false);
                _mqttClient.Dispose();
                _mqttClient = null;
                
                // Dispose cancellation token
                _reconnectCts?.Dispose();
                _reconnectCts = null;

                System.Diagnostics.Debug.WriteLine("MqttService: Stopped");
            }
            finally
            {
                _startStopLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            System.Diagnostics.Debug.WriteLine("MqttService: Disposing...");
            _disposed = true;

            await StopAsync().ConfigureAwait(false);
            _startStopLock.Dispose();

            System.Diagnostics.Debug.WriteLine("MqttService: Disposed");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartHome2.Models;
using SmartHome2.Services;
using Microsoft.Maui.ApplicationModel;

namespace SmartHome2.ViewModels
{
    public partial class DashboardVm : ObservableObject
    {
        private readonly IApiClient _api;
        private readonly IDataStore _store;
        private readonly IMqttService _mqtt;
        private IDispatcherTimer? _timer;
        private CancellationTokenSource? _timerCts;
        private bool _isTimerRunning;

        [ObservableProperty] private MetricsDto? metrics;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string status = "—";
        [ObservableProperty] private string mqttStatus = "Disconnected";
        [ObservableProperty] private string userRole = "";
        [ObservableProperty] private bool isAdmin = false;
        [ObservableProperty] private bool isGuest = false;

        // Individual properties to bind in UI (avoid nested binding issues)
        [ObservableProperty] private double temp;
        [ObservableProperty] private int humidity;
        [ObservableProperty] private int power;
        [ObservableProperty] private string ts = "";

        public DashboardVm(IApiClient api, IDataStore store, IMqttService mqtt)
        {
            _api = api;
            _store = store;
            _mqtt = mqtt;

            // Subscribe to MQTT events
            _mqtt.MetricsReceived += OnMqttMetricsReceived;
            _mqtt.ConnectionStatusChanged += OnMqttConnectionStatusChanged;

            // Set user role
            UserRole = AppSettings.CurrentUserRole;
            IsAdmin = AppSettings.IsAdmin;
            IsGuest = AppSettings.IsGuest;
            
            System.Diagnostics.Debug.WriteLine($"DashboardVm: UserRole={UserRole}, IsAdmin={IsAdmin}, IsGuest={IsGuest}");
        }

        private void OnMqttMetricsReceived(object? sender, MetricsDto metrics)
        {
            System.Diagnostics.Debug.WriteLine($"DashboardVm: MQTT metrics received: {metrics.Temp}°C");
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Metrics = metrics;
                Temp = metrics.Temp;
                Humidity = metrics.Humidity;
                Power = metrics.Power;
                Ts = metrics.Ts;
                Status = "MQTT (realtime)";
            });
        }

        private void OnMqttConnectionStatusChanged(object? sender, string status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MqttStatus = status;
                System.Diagnostics.Debug.WriteLine($"DashboardVm: MQTT status changed: {status}");
            });
        }

        public async Task InitializeAsync()
        {
            // Refresh user role from AppSettings (in case it changed after logout/login)
            UserRole = AppSettings.CurrentUserRole;
            IsAdmin = AppSettings.IsAdmin;
            IsGuest = AppSettings.IsGuest;
            
            System.Diagnostics.Debug.WriteLine($"DashboardVm.InitializeAsync: Refreshed user role - UserRole={UserRole}, IsAdmin={IsAdmin}, IsGuest={IsGuest}");
            
            // Start MQTT connection
            await _mqtt.StartAsync();
            
            // Load cached data initially
            var cached = await _store.LoadMetricsAsync();
            if (cached != null)
            {
                Metrics = cached;
                Temp = cached.Temp;
                Humidity = cached.Humidity;
                Power = cached.Power;
                Ts = cached.Ts;
            }
        }

        public void StartTimer()
        {
            // avoid multiple timers
            if (_isTimerRunning)
            {
                System.Diagnostics.Debug.WriteLine("StartTimer: timer already running");
                return;
            }

            var intervalSeconds = AppSettings.RefreshInterval;
            System.Diagnostics.Debug.WriteLine($"StartTimer: creating timer with {intervalSeconds}s interval");

            _timer = Application.Current?.Dispatcher?.CreateTimer();
            if (_timer != null)
            {
                _timer.Interval = TimeSpan.FromSeconds(intervalSeconds);
                _timer.Tick += async (_, __) =>
                {
                    System.Diagnostics.Debug.WriteLine("Timer tick (background)");
                    try
                    {
                        await RefreshCoreAsync(false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Timer tick Refresh failed: {ex}");
                    }
                };
                _timer.Start();
                _isTimerRunning = true;
                System.Diagnostics.Debug.WriteLine("StartTimer: Dispatcher timer started");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("StartTimer: Dispatcher.CreateTimer returned null, using Task-based timer");
                StartTaskBasedTimer();
            }
        }

        private void StartTaskBasedTimer()
        {
            _timerCts = new CancellationTokenSource();
            _isTimerRunning = true;
            var intervalMs = AppSettings.RefreshInterval * 1000;

            Task.Run(async () =>
            {
                System.Diagnostics.Debug.WriteLine($"Task-based timer started with {intervalMs}ms interval");
                try
                {
                    while (!_timerCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(intervalMs, _timerCts.Token);
                        if (!_timerCts.Token.IsCancellationRequested)
                        {
                            System.Diagnostics.Debug.WriteLine("Task timer tick (background)");
                            try
                            {
                                await RefreshCoreAsync(false);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Task timer Refresh failed: {ex}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("Task-based timer cancelled");
                }
            }, _timerCts.Token);
        }

        public void StopTimer()
        {
            if (!_isTimerRunning)
                return;

            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
                System.Diagnostics.Debug.WriteLine("StopTimer: Dispatcher timer stopped");
            }

            if (_timerCts != null)
            {
                _timerCts.Cancel();
                _timerCts.Dispose();
                _timerCts = null;
                System.Diagnostics.Debug.WriteLine("StopTimer: Task timer stopped");
            }

            _isTimerRunning = false;
        }

        // Public command for manual refresh (keeps existing generated command name)
        [RelayCommand]
        public async Task RefreshAsync()
        {
            await RefreshCoreAsync(true);
        }

        // Core refresh logic. If showBusy==false then avoid toggling IsBusy/Status to prevent UI flicker.
        private async Task RefreshCoreAsync(bool showBusy)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshCore started (showBusy={showBusy})");
            try
            {
                if (showBusy)
                {
                    IsBusy = true;
                    Status = "Fetching…";
                }

                var m = await _api.GetMetricsAsync();
                System.Diagnostics.Debug.WriteLine($"Got metrics from API: temp={m?.Temp}, hum={m?.Humidity}, power={m?.Power}, ts={m?.Ts}");

                // Update UI-bound properties on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Metrics = m;
                    if (m is not null)
                    {
                        Temp = m.Temp;
                        Humidity = m.Humidity;
                        Power = m.Power;
                        Ts = m.Ts;
                    }
                    if (showBusy)
                        Status = "Online (HTTP)";
                });

                // Save to cache and history (do not block UI updates)
                try
                {
                    await _store.SaveMetricsAsync(m!);
                    await _store.SaveMetricHistoryAsync(m!);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveMetricsAsync failed: {ex}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshCore failed: {ex}");

                var cached = await _store.LoadMetricsAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Metrics = cached;
                    if (cached is not null)
                    {
                        Temp = cached.Temp;
                        Humidity = cached.Humidity;
                        Power = cached.Power;
                        Ts = cached.Ts;
                    }
                    Status = cached is null ? "Offline (no cache)" : "Offline (cached)";
                });
            }
            finally
            {
                if (showBusy)
                {
                    IsBusy = false;
                }
                System.Diagnostics.Debug.WriteLine("RefreshCore finished");
            }
        }

        public void Cleanup()
        {
            // Unsubscribe from MQTT events to prevent memory leaks
            _mqtt.MetricsReceived -= OnMqttMetricsReceived;
            _mqtt.ConnectionStatusChanged -= OnMqttConnectionStatusChanged;
            StopTimer();
            System.Diagnostics.Debug.WriteLine("DashboardVm: Cleanup completed");
        }

        [RelayCommand]
        private async Task Logout()
        {
            System.Diagnostics.Debug.WriteLine("=============================================");
            System.Diagnostics.Debug.WriteLine("DashboardVm: LogoutCommand executed!");
            System.Diagnostics.Debug.WriteLine("=============================================");
        }
    }
}

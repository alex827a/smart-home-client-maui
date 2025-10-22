using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SmartHome2.Models;
using SmartHome2.Services;

namespace SmartHome2.ViewModels
{
    public partial class ChartsVm : ObservableObject
    {
        private readonly IApiClient _api;
        private readonly IDataStore _store;
        private readonly IMqttService _mqtt;
        private IDispatcherTimer? _timer;
        private bool _isTimerRunning;

        [ObservableProperty] private bool isRealtime = true;
        [ObservableProperty] private string status = "Ready";
        [ObservableProperty] private string userRole = "";
        [ObservableProperty] private bool isAdmin = false;
        [ObservableProperty] private int maxDataPoints = 100; // Increased from 20

        public ObservableCollection<ISeries> TempSeries { get; set; } = new();
        public ObservableCollection<ISeries> HumiditySeries { get; set; } = new();
        public ObservableCollection<ISeries> PowerSeries { get; set; } = new();

        private ObservableCollection<double> _tempValues = new();
        private ObservableCollection<double> _humidityValues = new();
        private ObservableCollection<double> _powerValues = new();

        public ChartsVm(IApiClient api, IDataStore store, IMqttService mqtt)
        {
            _api = api;
            _store = store;
            _mqtt = mqtt;
            
            // Set user role
            UserRole = AppSettings.CurrentUserRole;
            IsAdmin = AppSettings.IsAdmin;
            
            InitializeCharts();
            
            // Subscribe to MQTT metrics for background updates
            ReattachEvents();
        }

        public void ReattachEvents()
        {
            // Ensure we're not subscribed multiple times
            _mqtt.MetricsReceived -= OnMqttMetricsReceived;
            _mqtt.MetricsReceived += OnMqttMetricsReceived;
            System.Diagnostics.Debug.WriteLine("ChartsVm: Events reattached");
        }

        private void OnMqttMetricsReceived(object? sender, MetricsDto metrics)
        {
            if (!IsRealtime)
                return; // Only update in realtime mode

            System.Diagnostics.Debug.WriteLine($"ChartsVm: MQTT metrics received: {metrics.Temp}°C");
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _tempValues.Add(metrics.Temp);
                _humidityValues.Add(metrics.Humidity);
                _powerValues.Add(metrics.Power);

                // Keep configurable number of points
                while (_tempValues.Count > MaxDataPoints)
                {
                    _tempValues.RemoveAt(0);
                    _humidityValues.RemoveAt(0);
                    _powerValues.RemoveAt(0);
                }

                Status = $"Last update: {DateTime.Now:HH:mm:ss} (MQTT)";
            });
        }

        private void InitializeCharts()
        {
            TempSeries.Add(new LineSeries<double>
            {
                Values = _tempValues,
                Name = "Temperature (°C)",
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                GeometrySize = 4,
                LineSmoothness = 0.3
            });

            HumiditySeries.Add(new LineSeries<double>
            {
                Values = _humidityValues,
                Name = "Humidity (%)",
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
                GeometrySize = 4,
                LineSmoothness = 0.3
            });

            PowerSeries.Add(new LineSeries<double>
            {
                Values = _powerValues,
                Name = "Power (W)",
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                GeometrySize = 4,
                LineSmoothness = 0.3
            });
        }

        public void StartRealtimeUpdates()
        {
            if (_isTimerRunning)
                return;

            var intervalSeconds = AppSettings.RefreshInterval;
            System.Diagnostics.Debug.WriteLine($"ChartsVm: Starting realtime timer ({intervalSeconds}s)");

            // Load initial data from history
            _ = LoadInitialDataAsync();

            _timer = Application.Current?.Dispatcher?.CreateTimer();
            if (_timer != null)
            {
                _timer.Interval = TimeSpan.FromSeconds(intervalSeconds);
                _timer.Tick += async (_, __) => await UpdateRealtimeDataAsync();
                _timer.Start();
                _isTimerRunning = true;
            }
        }

        public void StopRealtimeUpdates()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
                _isTimerRunning = false;
                System.Diagnostics.Debug.WriteLine("ChartsVm: Realtime timer stopped");
            }
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                var history = await _store.LoadMetricHistoryAsync(Math.Min(MaxDataPoints, 50));
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _tempValues.Clear();
                    _humidityValues.Clear();
                    _powerValues.Clear();

                    foreach (var m in history)
                    {
                        _tempValues.Add(m.Temp);
                        _humidityValues.Add(m.Humidity);
                        _powerValues.Add(m.Power);
                    }

                    Status = $"Loaded {history.Count} initial points";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load initial data failed: {ex.Message}");
            }
        }

        private async Task UpdateRealtimeDataAsync()
        {
            try
            {
                var m = await _api.GetMetricsAsync();
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _tempValues.Add(m.Temp);
                    _humidityValues.Add(m.Humidity);
                    _powerValues.Add(m.Power);

                    // Keep configurable number of points
                    while (_tempValues.Count > MaxDataPoints)
                    {
                        _tempValues.RemoveAt(0);
                        _humidityValues.RemoveAt(0);
                        _powerValues.RemoveAt(0);
                    }

                    Status = $"Last update: {DateTime.Now:HH:mm:ss} (HTTP)";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Realtime update failed: {ex.Message}");
                Status = "Update failed";
            }
        }

        [RelayCommand]
        private async Task LoadHistoryAsync()
        {
            try
            {
                Status = "Loading history...";
                var history = await _store.LoadMetricHistoryAsync(MaxDataPoints);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _tempValues.Clear();
                    _humidityValues.Clear();
                    _powerValues.Clear();

                    foreach (var m in history)
                    {
                        _tempValues.Add(m.Temp);
                        _humidityValues.Add(m.Humidity);
                        _powerValues.Add(m.Power);
                    }

                    Status = $"Loaded {history.Count} records";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load history failed: {ex.Message}");
                Status = "Load failed";
            }
        }

        [RelayCommand]
        private void ToggleMode()
        {
            IsRealtime = !IsRealtime;
            
            if (IsRealtime)
            {
                StartRealtimeUpdates();
                Status = "Realtime mode";
            }
            else
            {
                StopRealtimeUpdates();
                _ = LoadHistoryAsync();
            }
        }

        [RelayCommand]
        private void SetMaxPoints(string points)
        {
            if (int.TryParse(points, out int value) && value > 0)
            {
                MaxDataPoints = value;
                Status = $"Max points set to {value}";
                
                // Trim existing data if needed
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    while (_tempValues.Count > MaxDataPoints)
                    {
                        _tempValues.RemoveAt(0);
                        _humidityValues.RemoveAt(0);
                        _powerValues.RemoveAt(0);
                    }
                });
            }
        }

        public void Cleanup()
        {
            // Unsubscribe from MQTT events
            _mqtt.MetricsReceived -= OnMqttMetricsReceived;
            StopRealtimeUpdates();
            System.Diagnostics.Debug.WriteLine("ChartsVm: Cleanup completed");
        }
    }
}
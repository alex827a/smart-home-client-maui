using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartHome2.Services;
using SmartHome2.Views;
using SmartHome2.Resources.Strings;

namespace SmartHome2.ViewModels
{
    public partial class LoginVm : ObservableObject
    {
        private readonly IRealtimeService _realtimeService;
        private readonly IApiClient _apiClient;

        [ObservableProperty] private string username = "";
        [ObservableProperty] private string password = "";
        [ObservableProperty] private string errorMessage = "";
        [ObservableProperty] private bool hasError = false;
        [ObservableProperty] private bool isBusy = false;
        [ObservableProperty] private string connectionMode = "";
        [ObservableProperty] private bool showGuestModeInfo = false;

        public LoginVm(IRealtimeService realtimeService, IApiClient apiClient)
        {
            _realtimeService = realtimeService;
            _apiClient = apiClient;
        }

        /// <summary>
        /// Called when page appears - does NOT auto-login, just checks availability
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Just check server availability, don't auto-login
                var status = await CheckServerStatusAsync();
                
                if (status != null)
                {
                    if (status.MqttAvailable)
                    {
                        ConnectionMode = "MQTT Available";
                        ShowGuestModeInfo = false;
                    }
                    else
                    {
                        ConnectionMode = "MQTT Unavailable (SSE fallback available)";
                        ShowGuestModeInfo = false; // Don't show banner yet
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoginVm: InitializeAsync failed - {ex.Message}");
            }
        }

        private async Task<ServerStatus?> CheckServerStatusAsync()
        {
            try
            {
                return await _apiClient.GetAsync<ServerStatus>("api/status");
            }
            catch
            {
                return null;
            }
        }

        [RelayCommand]
        private void ChangeLanguage(string lang)
        {
            System.Diagnostics.Debug.WriteLine($"ChangeLanguage called with: {lang}");
            
            // Update preferences
            AppSettings.Language = lang;
            
            // This will trigger PropertyChanged for all properties in AppResources.Instance
            System.Diagnostics.Debug.WriteLine($"Language changed from {AppResources.Instance.CurrentLanguage} to {lang}");
            
            // Force complete UI refresh by navigating
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Navigate to itself to force refresh with animation disabled
                    await Shell.Current.GoToAsync("//LoginPage", false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                }
            });
        }

        [RelayCommand]
        private async Task Login()
        {
            var loc = AppResources.Instance;
            
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ShowError(loc.PleaseEnterCredentials);
                return;
            }

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = "";
                ShowGuestModeInfo = false;

                // Save credentials
                AppSettings.MqttUsername = Username.Trim();
                AppSettings.MqttPassword = Password;

                // Determine role based on username
                var role = Username.Trim().ToLower() == "admin" ? "admin" : "guest";
                AppSettings.CurrentUserRole = role;

                System.Diagnostics.Debug.WriteLine($"LoginVm: Attempting login as {role}...");

                // Try to connect (will try MQTT first, then SSE fallback automatically)
                await _realtimeService.StartAsync();

                // Wait to see if connection succeeds
                await Task.Delay(3000); // Give more time for connection

                if (_realtimeService.IsConnected)
                {
                    var mode = _realtimeService.CurrentMode.ToUpper();
                    System.Diagnostics.Debug.WriteLine($"LoginVm: Connected successfully via {mode}");
                    
                    // Navigate to main page
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
                else
                {
                    // Connection failed - check if guest mode fallback is available
                    System.Diagnostics.Debug.WriteLine("LoginVm: Connection failed, checking guest mode fallback...");
                    
                    var serverStatus = await CheckServerStatusAsync();
                    
                    if (serverStatus != null && !serverStatus.MqttAvailable && role == "guest")
                    {
                        // MQTT unavailable but we can try SSE guest mode
                        System.Diagnostics.Debug.WriteLine("LoginVm: Trying SSE guest mode fallback...");
                        ShowGuestModeInfo = true;
                        ConnectionMode = "Connecting via SSE fallback...";
                        
                        // Try SSE connection
                        await _realtimeService.StopAsync(); // Stop failed attempt
                        await Task.Delay(500);
                        await _realtimeService.StartAsync(); // Restart (will use SSE)
                        
                        await Task.Delay(2000);
                        
                        if (_realtimeService.IsConnected && _realtimeService.CurrentMode == "sse")
                        {
                            System.Diagnostics.Debug.WriteLine("LoginVm: SSE guest mode connection successful!");
                            await Shell.Current.GoToAsync("//DashboardPage");
                            return;
                        }
                    }
                    
                    // Still failed
                    ShowError(loc.FailedToConnect);
                    AppSettings.MqttUsername = "";
                    AppSettings.MqttPassword = "";
                    ShowGuestModeInfo = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoginVm: Login exception - {ex.Message}");
                ShowError($"{loc.LoginFailed}: {ex.Message}");
                ShowGuestModeInfo = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task QuickLogin(string role)
        {
            if (role == "guest")
            {
                Username = "guest";
                Password = "123";
            }
            else if (role == "admin")
            {
                Username = "admin";
                Password = "admin";
            }

            await Login();
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        private class ServerStatus
        {
            public bool MqttAvailable { get; set; }
            public string? RecommendedMode { get; set; }
        }
    }
}
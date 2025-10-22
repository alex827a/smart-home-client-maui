using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartHome2.Services;
using SmartHome2.Views;
using SmartHome2.Resources.Strings;

namespace SmartHome2.ViewModels
{
    public partial class LoginVm : ObservableObject
    {
        private readonly IMqttService _mqttService;

        [ObservableProperty] private string username = "";
        [ObservableProperty] private string password = "";
        [ObservableProperty] private string errorMessage = "";
        [ObservableProperty] private bool hasError = false;
        [ObservableProperty] private bool isBusy = false;

        public LoginVm(IMqttService mqttService)
        {
            _mqttService = mqttService;
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

                // Save credentials
                AppSettings.MqttUsername = Username.Trim();
                AppSettings.MqttPassword = Password;

                // Determine role based on username
                var role = Username.Trim().ToLower() == "admin" ? "admin" : "guest";
                AppSettings.CurrentUserRole = role;

                // Test connection
                await _mqttService.StartAsync();

                // Wait a bit to see if connection succeeds
                await Task.Delay(2000);

                if (_mqttService.IsConnected)
                {
                    // Navigate to main page
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
                else
                {
                    ShowError(loc.FailedToConnect);
                    AppSettings.MqttUsername = "";
                    AppSettings.MqttPassword = "";
                }
            }
            catch (Exception ex)
            {
                ShowError($"{loc.LoginFailed}: {ex.Message}");
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
    }
}
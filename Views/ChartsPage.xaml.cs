using SmartHome2.ViewModels;
using SmartHome2.Services;

namespace SmartHome2.Views
{
    public partial class ChartsPage : ContentPage
    {
        private readonly ChartsVm _vm;
        private readonly IMqttService _mqttService;

        public ChartsPage(ChartsVm vm, IMqttService mqttService)
        {
            InitializeComponent();
            _vm = vm;
            _mqttService = mqttService;
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("ChartsPage: OnAppearing");
            
            // Reattach MQTT events
            _vm.ReattachEvents();
            
            if (_vm.IsRealtime)
            {
                _vm.StartRealtimeUpdates();
            }
            else
            {
                _ = _vm.LoadHistoryCommand.ExecuteAsync(null);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            System.Diagnostics.Debug.WriteLine("ChartsPage: OnDisappearing");
            
            // Important: Cleanup to prevent memory leaks
            // Don't call full Cleanup() here because we want to keep the VM state
            // Just stop updates, but keep MQTT subscription via Cleanup/Reattach cycle
            _vm.StopRealtimeUpdates();
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            _ = OnLogoutClickedAsync();
        }

        private async Task OnLogoutClickedAsync()
        {
            try
            {
                var loc = SmartHome2.Resources.Strings.AppResources.Instance;
                var confirm = await DisplayAlert(loc.Logout, loc.AreYouSure, loc.Yes, loc.No);
                if (confirm)
                {
                    // Full cleanup on logout
                    _vm.Cleanup();
                    
                    // Stop MQTT
                    await _mqttService.StopAsync();
                    
                    // Clear credentials
                    Services.AppSettings.MqttUsername = "";
                    Services.AppSettings.MqttPassword = "";
                    Services.AppSettings.CurrentUserRole = "guest";
                    
                    // Navigate to login
                    await Shell.Current.GoToAsync("//LoginPage", false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChartsPage: Logout error - {ex.Message}");
                await DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
            }
        }
    }
}
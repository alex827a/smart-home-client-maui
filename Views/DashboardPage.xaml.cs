namespace SmartHome2.Views;
using SmartHome2.ViewModels;
using SmartHome2.Services;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardVm _vm;
    private readonly IMqttService _mqttService;

    public DashboardPage(DashboardVm vm, IMqttService mqttService)
    {
        InitializeComponent();
        _vm = vm;
        _mqttService = mqttService;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = InitializePageAsync();
    }

    private async Task InitializePageAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("DashboardPage OnAppearing - initializing");
            System.Diagnostics.Debug.WriteLine($"DashboardPage OnAppearing: CurrentUserRole={AppSettings.CurrentUserRole}");
            System.Diagnostics.Debug.WriteLine($"DashboardPage OnAppearing: IsAdmin={AppSettings.IsAdmin}, IsGuest={AppSettings.IsGuest}");
            
            // Refresh VM role properties from AppSettings in case they changed
            _vm.UserRole = AppSettings.CurrentUserRole;
            _vm.IsAdmin = AppSettings.IsAdmin;
            _vm.IsGuest = AppSettings.IsGuest;
            
            System.Diagnostics.Debug.WriteLine($"DashboardPage OnAppearing: VM updated - UserRole={_vm.UserRole}, IsAdmin={_vm.IsAdmin}, IsGuest={_vm.IsGuest}");
            
            await _vm.InitializeAsync(); // Start MQTT and load cache
            _ = _vm.RefreshAsync(); // Initial HTTP refresh (fire-and-forget is ok here)
            _vm.StartTimer(); // Start polling timer as fallback
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DashboardPage InitializePageAsync failed: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        System.Diagnostics.Debug.WriteLine("DashboardPage OnDisappearing - stopping timer");
        _vm.StopTimer();
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        _ = OnLogoutClickedAsync();
    }

    private async Task OnLogoutClickedAsync()
    {
        System.Diagnostics.Debug.WriteLine("=============================================");
        System.Diagnostics.Debug.WriteLine("DashboardPage: OnLogoutClicked fired!");
        System.Diagnostics.Debug.WriteLine("=============================================");
        
        try
        {
            var loc = SmartHome2.Resources.Strings.AppResources.Instance;
            System.Diagnostics.Debug.WriteLine($"DashboardPage: Current language: {loc.CurrentLanguage}");
            System.Diagnostics.Debug.WriteLine($"DashboardPage: IsGuest: {_vm.IsGuest}");
            System.Diagnostics.Debug.WriteLine($"DashboardPage: IsAdmin: {_vm.IsAdmin}");
            System.Diagnostics.Debug.WriteLine($"DashboardPage: UserRole: {_vm.UserRole}");
            
            var confirm = await DisplayAlert(loc.Logout, loc.AreYouSure, loc.Yes, loc.No);
            System.Diagnostics.Debug.WriteLine($"DashboardPage: DisplayAlert result: {confirm}");
            
            if (confirm)
            {
                System.Diagnostics.Debug.WriteLine("DashboardPage: Logout confirmed - starting cleanup");
                
                // Stop timer first
                System.Diagnostics.Debug.WriteLine("DashboardPage: Stopping timer");
                _vm.StopTimer();
                
                // Cleanup VM
                System.Diagnostics.Debug.WriteLine("DashboardPage: Calling _vm.Cleanup()");
                _vm.Cleanup();
                System.Diagnostics.Debug.WriteLine("DashboardPage: _vm.Cleanup() completed");
                
                // Stop MQTT
                System.Diagnostics.Debug.WriteLine("DashboardPage: Calling _mqttService.StopAsync()");
                await _mqttService.StopAsync();
                System.Diagnostics.Debug.WriteLine("DashboardPage: _mqttService.StopAsync() completed");
                
                // Clear credentials and reset to guest
                System.Diagnostics.Debug.WriteLine("DashboardPage: Clearing credentials");
                AppSettings.MqttUsername = "";
                AppSettings.MqttPassword = "";
                AppSettings.CurrentUserRole = "guest";
                System.Diagnostics.Debug.WriteLine("DashboardPage: Credentials cleared");
                
                // Wait a bit to ensure cleanup completes
                await Task.Delay(500);
                
                System.Diagnostics.Debug.WriteLine("DashboardPage: Navigating to LoginPage");
                
                // Navigate to login with animation disabled for clean transition
                await Shell.Current.GoToAsync("//LoginPage", false);
                
                System.Diagnostics.Debug.WriteLine("DashboardPage: Navigation completed successfully!");
                System.Diagnostics.Debug.WriteLine("=============================================");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DashboardPage: Logout cancelled by user");
                System.Diagnostics.Debug.WriteLine("=============================================");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("=============================================");
            System.Diagnostics.Debug.WriteLine($"DashboardPage: EXCEPTION in OnLogoutClicked!");
            System.Diagnostics.Debug.WriteLine($"DashboardPage: Exception type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"DashboardPage: Exception message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"DashboardPage: Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"DashboardPage: Inner exception: {ex.InnerException.Message}");
            }
            System.Diagnostics.Debug.WriteLine("=============================================");
            
            try
            {
                await DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
            }
            catch (Exception displayEx)
            {
                System.Diagnostics.Debug.WriteLine($"DashboardPage: Even DisplayAlert failed: {displayEx.Message}");
            }
        }
    }

    private void OpenDevices_OnClicked(object sender, EventArgs e)
    {
        _ = NavigateToDevicesAsync();
    }

    private async Task NavigateToDevicesAsync()
    {
        try
        {
            await Navigation.PushAsync(
                Application.Current!.Handler.MauiContext!.Services.GetRequiredService<DevicesPage>());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation to Devices failed: {ex.Message}");
        }
    }

    private void OpenCharts_OnClicked(object sender, EventArgs e)
    {
        _ = NavigateToChartsAsync();
    }

    private async Task NavigateToChartsAsync()
    {
        try
        {
            await Navigation.PushAsync(
                Application.Current!.Handler.MauiContext!.Services.GetRequiredService<ChartsPage>());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation to Charts failed: {ex.Message}");
        }
    }

    private void OpenSettings_OnClicked(object sender, EventArgs e)
    {
        _ = NavigateToSettingsAsync();
    }

    private async Task NavigateToSettingsAsync()
    {
        try
        {
            if (AppSettings.IsAdmin)
            {
                await Navigation.PushAsync(
                    Application.Current!.Handler.MauiContext!.Services.GetRequiredService<SettingsPage>());
            }
            else
            {
                await DisplayAlert("Access Denied", "Only administrators can access settings.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation to Settings failed: {ex.Message}");
        }
    }

    private void OnRefreshClicked(object sender, EventArgs e)
    {
        _ = RefreshDataAsync();
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Refresh button clicked (codebehind)");
            await _vm.RefreshAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Refresh failed: {ex.Message}");
        }
    }
}
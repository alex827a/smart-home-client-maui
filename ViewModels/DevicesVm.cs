using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartHome2.Models;
using SmartHome2.Services;

namespace SmartHome2.ViewModels
{
    public partial class DevicesVm : ObservableObject
    {
        private readonly IApiClient _api;
        private readonly IDataStore _store;
        
        [ObservableProperty] 
        private ObservableCollection<DeviceDto> devices = new();

        [ObservableProperty]
        private bool canToggleDevices = false;

        public DevicesVm(IApiClient api, IDataStore store)
        {
            _api = api;
            _store = store;
            RefreshPermissions();
            _ = LoadDevicesAsync();
        }

        public void RefreshPermissions()
        {
            CanToggleDevices = AppSettings.IsAdmin; // Only admin can toggle
            System.Diagnostics.Debug.WriteLine($"DevicesVm: RefreshPermissions - CanToggleDevices={CanToggleDevices}, CurrentUserRole={AppSettings.CurrentUserRole}, IsAdmin={AppSettings.IsAdmin}");
        }

        private async Task LoadDevicesAsync()
        {
            try
            {
                var list = await _api.GetDevicesAsync();
                Devices.Clear();
                foreach (var device in list)
                {
                    Devices.Add(device);
                }
                await _store.SaveDevicesAsync(list);
            }
            catch
            {
                var cachedDevices = await _store.LoadDevicesAsync();
                Devices.Clear();
                foreach (var device in cachedDevices)
                {
                    Devices.Add(device);
                }
            }
        }

        [RelayCommand]
        private async Task ToggleDevice(DeviceDto device)
        {
            // Refresh permissions before each toggle attempt
            RefreshPermissions();
            
            System.Diagnostics.Debug.WriteLine($"DevicesVm: ToggleDevice called for {device.Name}");
            System.Diagnostics.Debug.WriteLine($"DevicesVm: CanToggleDevices={CanToggleDevices}, CurrentUserRole={AppSettings.CurrentUserRole}, IsAdmin={AppSettings.IsAdmin}");
            
            if (!CanToggleDevices)
            {
                System.Diagnostics.Debug.WriteLine("DevicesVm: Toggle denied - insufficient permissions");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"DevicesVm: Toggle allowed - proceeding to toggle {device.Name}");

            try
            {
                var updatedDevice = await _api.ToggleDeviceAsync(device.Id);
                var index = Devices.IndexOf(device);
                if (index >= 0)
                {
                    Devices[index] = updatedDevice;
                }
                // Save updated devices to cache
                await _store.SaveDevicesAsync(Devices);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevicesVm: Toggle failed - {ex.Message}");
                // Fallback to local toggle if API call fails
                var index = Devices.IndexOf(device);
                if (index >= 0)
                {
                    var newDevice = device with { IsOn = !device.IsOn };
                    Devices[index] = newDevice;
                }
            }
        }
    }
}
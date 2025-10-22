using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartHome2.Services;
using SmartHome2.Resources.Strings;

namespace SmartHome2.ViewModels
{
    public partial class SettingsVm : ObservableObject
    {
        private CancellationTokenSource? _messageTokenSource;

        [ObservableProperty] private string baseUrl = "";
        [ObservableProperty] private string refreshInterval = "";
        [ObservableProperty] private string statusMessage = "";
        [ObservableProperty] private bool showStatus = false;
        [ObservableProperty] private int languageIndex = 0;

        public SettingsVm()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            BaseUrl = AppSettings.BaseUrl;
            RefreshInterval = AppSettings.RefreshInterval.ToString();
            LanguageIndex = AppSettings.Language == "de" ? 1 : 0;
        }

        [RelayCommand]
        private async Task SaveSettings()
        {
            try
            {
                var loc = AppResources.Instance;
                
                // Validate URL
                if (string.IsNullOrWhiteSpace(BaseUrl))
                {
                    await ShowMessageAsync(loc.BaseUrl + " " + loc.CannotBeEmpty);
                    return;
                }

                if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
                {
                    await ShowMessageAsync(loc.InvalidFormat);
                    return;
                }

                // Validate interval
                if (!int.TryParse(RefreshInterval, out int interval) || interval < 1)
                {
                    await ShowMessageAsync(loc.RefreshIntervalLabel + " " + loc.MustBePositive);
                    return;
                }

                // Save settings
                AppSettings.BaseUrl = BaseUrl.TrimEnd('/') + "/";
                AppSettings.RefreshInterval = interval;
                
                // Save language
                var newLanguage = LanguageIndex == 1 ? "de" : "en";
                if (newLanguage != AppSettings.Language)
                {
                    AppSettings.Language = newLanguage;
                    AppResources.Instance.CurrentLanguage = newLanguage;
                    await ShowMessageAsync(loc.SettingsSaved + " " + loc.PleaseRestart);
                }
                else
                {
                    await ShowMessageAsync(loc.SettingsSaved);
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"{AppResources.Instance.Error}: {ex.Message}");
            }
        }

        private async Task ShowMessageAsync(string message)
        {
            // Cancel previous message timer
            _messageTokenSource?.Cancel();
            _messageTokenSource = new CancellationTokenSource();
            
            try
            {
                StatusMessage = message;
                ShowStatus = true;
                
                // Hide message after 3 seconds
                await Task.Delay(3000, _messageTokenSource.Token);
                ShowStatus = false;
            }
            catch (OperationCanceledException)
            {
                // Message was cancelled, that's ok
            }
        }

        public void Cleanup()
        {
            _messageTokenSource?.Cancel();
            _messageTokenSource?.Dispose();
        }
    }
}
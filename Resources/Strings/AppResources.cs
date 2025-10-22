using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartHome2.Resources.Strings
{
    public class AppResources : INotifyPropertyChanged
    {
        private static AppResources? _instance;
        public static AppResources Instance => _instance ??= new AppResources();

        public event PropertyChangedEventHandler? PropertyChanged;

        private string _currentLanguage = "en";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged();
                    // Notify all properties changed
                    OnPropertyChanged(string.Empty);
                }
            }
        }

        // Login Page
        public string Login => GetString("Login", "Anmelden");
        public string Username => GetString("Username", "Benutzername");
        public string Password => GetString("Password", "Passwort");
        public string EnterUsername => GetString("Enter username", "Benutzername eingeben");
        public string EnterPassword => GetString("Enter password", "Passwort eingeben");
        public string Guest => GetString("Guest", "Gast");
        public string Admin => GetString("Admin", "Administrator");
        public string QuickLogin => GetString("Quick Login:", "Schnellanmeldung:");
        public string PleaseEnterCredentials => GetString("Please enter username and password", "Bitte Benutzername und Passwort eingeben");
        public string LoginFailed => GetString("Login failed", "Anmeldung fehlgeschlagen");
        public string FailedToConnect => GetString("Failed to connect to MQTT broker. Check credentials.", "Verbindung zum MQTT-Broker fehlgeschlagen. Überprüfen Sie die Anmeldedaten.");
        
        // Dashboard
        public string Dashboard => GetString("Dashboard", "Armaturenbrett");
        public string Logout => GetString("Logout", "Abmelden");
        public string Role => GetString("Role", "Rolle");
        public string Status => GetString("Status", "Status");
        public string Mqtt => GetString("MQTT", "MQTT");
        public string Temperature => GetString("Temperature", "Temperatur");
        public string Humidity => GetString("Humidity", "Feuchtigkeit");
        public string Power => GetString("Power", "Leistung");
        public string Updated => GetString("Updated", "Aktualisiert");
        public string Refresh => GetString("Refresh", "Aktualisieren");
        public string OpenDevices => GetString("Open Devices", "Geräte öffnen");
        public string ViewCharts => GetString("View Charts", "Diagramme anzeigen");
        public string Settings => GetString("Settings", "Einstellungen");
        public string Online => GetString("Online", "Online");
        public string Offline => GetString("Offline", "Offline");
        public string Fetching => GetString("Fetching…", "Lädt…");
        
        // Devices
        public string Devices => GetString("Devices", "Geräte");
        public string Mode => GetString("Mode", "Modus");
        public string CanToggleDevices => GetString("Can toggle devices", "Kann Geräte schalten");
        public string ViewOnly => GetString("View only", "Nur ansehen");
        public string Toggle => GetString("Toggle", "Umschalten");
        public string On => GetString("On", "An");
        public string Off => GetString("Off", "Aus");
        
        // Charts
        public string MetricsCharts => GetString("Metrics Charts", "Metriken-Diagramme");
        public string LastUpdate => GetString("Last update", "Letzte Aktualisierung");
        public string SwitchToHistory => GetString("Switch to History", "Zu Verlauf wechseln");
        public string SwitchToRealtime => GetString("Switch to Realtime", "Zu Echtzeit wechseln");
        public string MaxDataPoints => GetString("Max Data Points:", "Max. Datenpunkte:");
        public string Set => GetString("Set", "Setzen");
        public string TemperatureTrend => GetString("Temperature Trend", "Temperaturverlauf");
        public string HumidityTrend => GetString("Humidity Trend", "Feuchtigkeitsverlauf");
        public string PowerTrend => GetString("Power Trend", "Leistungsverlauf");
        public string RealtimeMode => GetString("Realtime mode", "Echtzeitmodus");
        public string LoadingHistory => GetString("Loading history...", "Verlauf wird geladen...");
        public string Loaded => GetString("Loaded", "Geladen");
        public string Records => GetString("records", "Einträge");
        
        // Settings
        public string ServerSettings => GetString("Server Settings", "Servereinstellungen");
        public string BaseUrl => GetString("Base URL:", "Basis-URL:");
        public string RefreshIntervalLabel => GetString("Refresh Interval (seconds):", "Aktualisierungsintervall (Sekunden):");
        public string Language => GetString("Language / Sprache:", "Sprache / Language:");
        public string SaveSettings => GetString("Save Settings", "Einstellungen speichern");
        public string SettingsSaved => GetString("Settings saved successfully!", "Einstellungen erfolgreich gespeichert!");
        public string CannotBeEmpty => GetString("cannot be empty", "darf nicht leer sein");
        public string InvalidFormat => GetString("Invalid format", "Ungültiges Format");
        public string MustBePositive => GetString("must be a positive number", "muss eine positive Zahl sein");
        public string PleaseRestart => GetString("Please restart the app.", "Bitte App neu starten.");
        
        // Common
        public string Yes => GetString("Yes", "Ja");
        public string No => GetString("No", "Nein");
        public string Ok => GetString("OK", "OK");
        public string Cancel => GetString("Cancel", "Abbrechen");
        public string Error => GetString("Error", "Fehler");
        public string Success => GetString("Success", "Erfolg");
        public string AreYouSure => GetString("Are you sure you want to logout?", "Möchten Sie sich wirklich abmelden?");
        public string AccessDenied => GetString("Access Denied", "Zugriff verweigert");
        public string OnlyAdmins => GetString("Only administrators can access settings.", "Nur Administratoren können auf Einstellungen zugreifen.");
        public string Connected => GetString("Connected", "Verbunden");
        public string Disconnected => GetString("Disconnected", "Getrennt");

        private string GetString(string english, string german)
        {
            return CurrentLanguage == "de" ? german : english;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
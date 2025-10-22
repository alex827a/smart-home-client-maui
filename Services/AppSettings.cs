namespace SmartHome2.Services
{
    public static class AppSettings
    {
        public const string BaseUrlKey = "base_url";
        public const string RefreshIntervalKey = "refresh_interval";
        public const string MqttBrokerKey = "mqtt_broker";
        public const string MqttPortKey = "mqtt_port";
        public const string MqttUseTlsKey = "mqtt_use_tls";
        public const string MqttUsernameKey = "mqtt_username";
        public const string MqttPasswordKey = "mqtt_password";
        public const string CurrentUserRoleKey = "current_user_role";
        public const string MqttUseClientCertsKey = "mqtt_use_client_certs";
        public const string LanguageKey = "language";
        
        public static string BaseUrl
        {
            get => Preferences.Get(BaseUrlKey, "http://127.0.0.1:8000/");
            set => Preferences.Set(BaseUrlKey, value);
        }
        
        public static int RefreshInterval
        {
            get => Preferences.Get(RefreshIntervalKey, 5);
            set => Preferences.Set(RefreshIntervalKey, value);
        }

        public static string MqttBroker
        {
            get => Preferences.Get(MqttBrokerKey, "127.0.0.1");
            set => Preferences.Set(MqttBrokerKey, value);
        }

        public static int MqttPort
        {
            get => Preferences.Get(MqttPortKey, 8883); // default TLS port
            set => Preferences.Set(MqttPortKey, value);
        }

        public static bool MqttUseTls
        {
            get => Preferences.Get(MqttUseTlsKey, true);
            set => Preferences.Set(MqttUseTlsKey, value);
        }

        public static bool MqttUseClientCerts
        {
            get => Preferences.Get(MqttUseClientCertsKey, true);
            set => Preferences.Set(MqttUseClientCertsKey, value);
        }

        public static string MqttUsername
        {
            get => Preferences.Get(MqttUsernameKey, "guest");
            set => Preferences.Set(MqttUsernameKey, value);
        }

        public static string MqttPassword
        {
            get => Preferences.Get(MqttPasswordKey, "");
            set => Preferences.Set(MqttPasswordKey, value);
        }

        public static string CurrentUserRole
        {
            get => Preferences.Get(CurrentUserRoleKey, "guest");
            set => Preferences.Set(CurrentUserRoleKey, value);
        }

        public static string Language
        {
            get => Preferences.Get(LanguageKey, "en");
            set
            {
                Preferences.Set(LanguageKey, value);
                Resources.Strings.AppResources.Instance.CurrentLanguage = value;
            }
        }

        public static bool IsAdmin => CurrentUserRole.Equals("admin", StringComparison.OrdinalIgnoreCase);
        public static bool IsGuest => CurrentUserRole.Equals("guest", StringComparison.OrdinalIgnoreCase);
    }
}
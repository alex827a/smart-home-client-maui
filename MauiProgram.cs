using Microsoft.Extensions.Logging;
using SmartHome2.Views;
using SmartHome2.ViewModels;
using SmartHome2.Services;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace SmartHome2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register SQLite database as SINGLETON (only one connection)
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "shd.db3");
            builder.Services.AddSingleton<IDataStore>(sp => new SqliteDataStore(dbPath));

            // HttpClient with Polly resilience policies
            builder.Services.AddHttpClient<IApiClient, ApiClient>()
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetTimeoutPolicy());
            
            // SSE Service (requires separate HttpClient with infinite timeout)
            builder.Services.AddHttpClient<ISseService, SseService>();
            
            // MQTT Service (Singleton - shared state is needed)
            builder.Services.AddSingleton<IMqttService, MqttService>();
            
            // Unified Realtime Service with automatic fallback (Singleton)
            builder.Services.AddSingleton<IRealtimeService, RealtimeService>();

            // ViewModels and Pages - Transient to prevent state issues
            builder.Services.AddTransient<LoginVm>();
            builder.Services.AddTransient<LoginPage>();

            builder.Services.AddTransient<DashboardVm>();
            builder.Services.AddTransient<DashboardPage>();
            
            builder.Services.AddTransient<DevicesVm>();
            builder.Services.AddTransient<DevicesPage>();

            builder.Services.AddTransient<SettingsVm>();
            builder.Services.AddTransient<SettingsPage>();

            builder.Services.AddTransient<ChartsVm>();
            builder.Services.AddTransient<ChartsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
            HttpPolicyExtensions
                .HandleTransientHttpError()                     
                .OrResult(r => r.StatusCode == (HttpStatusCode)429)
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

        static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
            Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(3));
    }
}

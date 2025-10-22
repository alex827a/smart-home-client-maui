# ?? SmartHome2 - .NET MAUI Smart Home Client

[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![MQTT](https://img.shields.io/badge/MQTT-4.3.7-660066?logo=mqtt)](https://mqtt.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Cross-platform smart home monitoring and control application built with .NET MAUI, featuring real-time MQTT updates and HTTP API integration.

![SmartHome2 Dashboard](docs/screenshots/dashboard.png)

## ?? Table of Contents

- [Features](#-features)
- [Architecture](#-architecture)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Running the Application](#-running-the-application)
- [Usage](#-usage)
- [Project Structure](#-project-structure)
- [Development](#-development)
- [Troubleshooting](#-troubleshooting)
- [Contributing](#-contributing)
- [License](#-license)

---

## ? Features

### ?? Real-time Monitoring
- **Live Metrics Dashboard**: Temperature, Humidity, Power consumption
- **MQTT Push Notifications**: Instant updates without polling
- **Historical Charts**: Interactive LiveCharts with configurable data points
- **Offline Support**: SQLite caching for offline operation

### ?? Device Control
- **Remote Device Management**: Toggle smart home devices (lights, fans, HVAC, etc.)
- **Role-Based Access Control**: Admin and Guest user roles
- **Visual Feedback**: Real-time device state updates

### ?? Internationalization
- **Multi-language Support**: English and German (Deutsch)
- **Dynamic Language Switching**: Change language without app restart
- **Localized UI**: All screens fully translated

### ?? Security
- **MQTT TLS/SSL**: Encrypted communication with broker
- **Client Certificates**: mTLS authentication support
- **ACL Integration**: Mosquitto ACL enforcement
- **Secure Credentials**: Persistent credential storage

### ?? Modern UI/UX
- **Material Design**: Clean, responsive interface
- **Cross-Platform**: Windows, Android, iOS, macOS
- **Pull-to-Refresh**: Intuitive gesture controls
- **Dark Theme Support**: (Coming soon)

---

## ??? Architecture

```
???????????????????????????????????????????????????????????????????
?                        MAUI Application                          ?
???????????????????????????????????????????????????????????????????
?                                                                  ?
?  ????????????????    ????????????????    ????????????????     ?
?  ?   Views      ??????  ViewModels  ??????   Services   ?     ?
?  ?  (XAML)      ?    ?   (MVVM)     ?    ?              ?     ?
?  ????????????????    ????????????????    ????????????????     ?
?                                                   ?              ?
?                          ????????????????????????????????????   ?
?                          ?                        ?         ?   ?
?                   ??????????????         ???????????????  ???? ?
?                   ? ApiClient  ?         ? MqttService ?  ?DB? ?
?                   ?  (HTTP)    ?         ?  (MQTT)     ?  ?  ? ?
?                   ??????????????         ???????????????  ???? ?
?????????????????????????????????????????????????????????????????
                          ?                       ?
                          ?                       ?
                   ????????????????       ????????????????
                   ?  FastAPI     ?????????  Mosquitto   ?
                   ?  Server      ?       ?  Broker      ?
                   ????????????????       ????????????????
```

### Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Framework** | .NET MAUI | 8.0 |
| **Language** | C# | 12.0 |
| **MVVM** | CommunityToolkit.Mvvm | 8.2.2 |
| **Database** | SQLite (sqlite-net-pcl) | 1.9.172 |
| **HTTP Client** | Polly (Resilience) | 8.0.10 |
| **MQTT** | MQTTnet | 4.3.7.1207 |
| **Charts** | LiveChartsCore | 2.0.0-rc3.3 |
| **UI** | SkiaSharp | Latest |

---

## ?? Prerequisites

### Required Software

1. **Visual Studio 2022** (v17.8 or later)
   - Workload: `.NET Multi-platform App UI development`
   - OR **Visual Studio Code** with C# Dev Kit extension

2. **.NET 8.0 SDK**
   ```bash
   # Check if installed
   dotnet --version
   # Should return 8.0.x or higher
   ```
   Download: https://dotnet.microsoft.com/download/dotnet/8.0

3. **Platform-specific SDKs** (optional, for deployment)
   - **Windows**: Windows 10.0.19041.0 SDK or higher
   - **Android**: Android SDK 21+ (included in VS)
   - **iOS/macOS**: Xcode 15+ (macOS only)

### Backend Services

4. **FastAPI Server** (Python)
   - Repository: `[your-fastapi-repo]`
   - Default URL: `http://127.0.0.1:8000`

5. **Mosquitto MQTT Broker**
   - Version: 2.0.18+
   - Port: 8883 (TLS) or 1883 (non-TLS)
   - Download: https://mosquitto.org/download/

### Optional Tools

- **Git**: For version control
- **Postman**: For API testing
- **MQTT Explorer**: For MQTT debugging

---

## ?? Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/SmartHome2.git
cd SmartHome2
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

Or in Visual Studio: `Right-click Solution ? Restore NuGet Packages`

### 3. Setup MQTT Certificates (for TLS)

#### Option A: Use Included Certificates (Development)

The project includes pre-generated mkcert certificates:
- `client.pfx` (Windows-compatible)
- `client-cert.pem` + `client-key.pem` (PEM format)

These are already configured in the project.

#### Option B: Generate Your Own Certificates

```bash
# Install mkcert
# Windows: scoop install mkcert
# macOS: brew install mkcert
# Linux: https://github.com/FiloSottile/mkcert#installation

# Generate CA and certificates
mkcert -install
mkcert -client localhost 127.0.0.1 ::1

# Convert to PFX (Windows)
.\ConvertToPfx.ps1
# OR use PfxConverter utility
cd PfxConverter
dotnet run
```

Place generated files in project root:
- `client.pfx`
- `client-cert.pem`
- `client-key.pem`

---

## ?? Configuration

### 1. Backend Server Setup

Ensure your FastAPI server is running:

```bash
cd /path/to/fastapi-server
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

API Endpoints required:
- `GET /api/metrics` - Returns current sensor metrics
- `GET /api/devices` - Returns list of devices
- `POST /api/devices/{id}/toggle` - Toggles device state

### 2. Mosquitto Broker Setup

#### Basic Configuration (`mosquitto.conf`):

```conf
# Listener
listener 8883
certfile /path/to/server-cert.pem
keyfile /path/to/server-key.pem
cafile /path/to/ca-cert.pem

# Authentication
allow_anonymous false
password_file /path/to/passwd

# ACL
acl_file /path/to/acl.conf
```

#### ACL Configuration (`acl.conf`):

```conf
# Admin user - full access
user admin
topic readwrite #

# Guest user - read only
user guest
topic read home/+/metrics
topic read home/+/state
```

#### Create Users:

```bash
mosquitto_passwd -c passwd admin
# Enter password: admin

mosquitto_passwd passwd guest
# Enter password: 123
```

#### Start Broker:

```bash
mosquitto -c mosquitto.conf -v
```

### 3. App Configuration

On first launch, configure in **Settings**:

| Setting | Default | Description |
|---------|---------|-------------|
| **Base URL** | `http://127.0.0.1:8000/` | FastAPI server address |
| **MQTT Broker** | `127.0.0.1` | Mosquitto broker IP |
| **MQTT Port** | `8883` | Broker port (8883=TLS, 1883=plain) |
| **Use TLS** | `true` | Enable/disable TLS encryption |
| **Refresh Interval** | `5` seconds | Polling interval (fallback) |
| **Language** | `en` | UI language (en/de) |

Credentials are configured in `Services/AppSettings.cs`:
```csharp
public static string MqttUsername { get; set; } = "guest"; // or "admin"
public static string MqttPassword { get; set; } = "";      // Set via login
```

---

## ?? Running the Application

### Visual Studio 2022

1. Open `SmartHome2.sln`
2. Select target platform:
   - **Windows Machine** (recommended for development)
   - **Android Emulator** / Physical device
   - **iOS Simulator** / Physical device (macOS only)
3. Press `F5` or click **? Start Debugging**

### Visual Studio Code

```bash
# Windows
dotnet build -t:Run -f net8.0-windows10.0.19041.0

# Android
dotnet build -t:Run -f net8.0-android

# iOS (macOS only)
dotnet build -t:Run -f net8.0-ios
```

### Command Line

```bash
# Restore and build
dotnet restore
dotnet build

# Run on Windows
dotnet run --framework net8.0-windows10.0.19041.0

# Run on Android (device must be connected)
dotnet run --framework net8.0-android
```

---

## ?? Usage

### Login

1. Launch the application
2. Select language: **EN** / **DE**
3. Login options:
   - **Quick Login**: Tap "Guest" or "Admin" buttons
   - **Manual**: Enter credentials

| User | Username | Password | Permissions |
|------|----------|----------|-------------|
| Guest | `guest` | `123` | View only (metrics, devices) |
| Admin | `admin` | `admin` | Full control (toggle devices, settings) |

### Dashboard

- View real-time metrics (Temperature, Humidity, Power)
- See MQTT connection status
- Navigate to:
  - **Open Devices**: Control smart devices
  - **View Charts**: Historical data visualization
  - **Settings**: App configuration (Admin only)

### Devices

- **Guest**: View device states only
- **Admin**: Toggle devices on/off
- Visual indicators: **An** (On) / **Aus** (Off) in German

### Charts

- Switch between **Realtime** and **History** modes
- Configure data points: 50, 100, 200, or custom
- Interactive zoom on X-axis
- Three charts: Temperature, Humidity, Power

### Settings (Admin Only)

- Configure server URL
- Adjust refresh interval
- Change language (English/Deutsch)
- Logout

---

## ?? Project Structure

```
SmartHome2/
??? ?? Models/                  # Data models (DTOs)
?   ??? Dtos.cs                 # MetricsDto, DeviceDto
??? ?? Services/                # Business logic
?   ??? ApiClient.cs            # HTTP API client (Polly)
?   ??? MqttService.cs          # MQTT client (MQTTnet)
?   ??? SqliteDataStore.cs      # Local database (SQLite)
?   ??? AppSettings.cs          # Configuration (Preferences)
?   ??? IApiClient.cs           # Service interfaces
??? ?? ViewModels/              # MVVM ViewModels
?   ??? DashboardVm.cs          # Dashboard logic
?   ??? DevicesVm.cs            # Device control
?   ??? ChartsVm.cs             # Charts data binding
?   ??? LoginVm.cs              # Authentication
?   ??? SettingsVm.cs           # Configuration
??? ?? Views/                   # XAML UI pages
?   ??? LoginPage.xaml          # Login screen
?   ??? DashboardPage.xaml      # Main dashboard
?   ??? DevicesPage.xaml        # Device list
?   ??? ChartsPage.xaml         # Charts visualization
?   ??? SettingsPage.xaml       # Settings screen
??? ?? Resources/               # Assets
?   ??? Strings/                # Localization
?   ?   ??? AppResources.cs     # EN/DE translations
?   ??? Images/                 # Images/icons
?   ??? Styles/                 # XAML styles
??? ?? Utils/                   # Helper classes
?   ??? BoolToOnOffConverter.cs # XAML converters
?   ??? BoolToModeConverter.cs
??? ?? Platforms/               # Platform-specific code
?   ??? Windows/
?   ??? Android/
?   ??? iOS/
??? ?? MauiProgram.cs           # DI container setup
??? ?? App.xaml.cs              # Application entry point
??? ?? AppShell.xaml            # Navigation shell
??? ?? SmartHome2.csproj        # Project file
??? ?? client.pfx               # MQTT client certificate
??? ?? client-cert.pem          # PEM certificate
??? ?? client-key.pem           # PEM private key
??? ?? README.md                # This file
```

---

## ?? Development

### Code Style

- **C# 12.0** features (record types, pattern matching)
- **MVVM pattern** with CommunityToolkit.Mvvm
- **Async/await** for all I/O operations
- **Dependency Injection** via `MauiProgram.cs`
- **ConfigureAwait(false)** in services (not ViewModels)

### Key Patterns

#### 1. MVVM with Source Generators
```csharp
public partial class DashboardVm : ObservableObject
{
    [ObservableProperty] private double temp;  // Auto-generates Temp property
    
    [RelayCommand]  // Auto-generates RefreshCommand
    private async Task Refresh() { }
}
```

#### 2. Resilience with Polly
```csharp
builder.Services.AddHttpClient<IApiClient, ApiClient>()
    .AddPolicyHandler(GetRetryPolicy())        // 3 retries with exponential backoff
    .AddPolicyHandler(GetTimeoutPolicy());     // 3-second timeout
```

#### 3. Resource Management
```csharp
public class SqliteDataStore : IDataStore, IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await _db.CloseAsync().ConfigureAwait(false);
        _initLock.Dispose();
    }
}
```

### Building Release Version

```bash
# Windows
dotnet publish -f net8.0-windows10.0.19041.0 -c Release

# Android APK
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk

# Android AAB (Google Play)
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=aab
```

### Debugging MQTT

Enable verbose logging:
```csharp
// MqttService.cs - Already enabled
System.Diagnostics.Debug.WriteLine($"MQTT: Message received on {topic}: {payload}");
```

View logs in Visual Studio: **Debug ? Windows ? Output** (select "Debug")

---

## ?? Troubleshooting

### Common Issues

#### 1. **MQTT Connection Failed**

**Symptoms:**
```
MqttService: Failed to start - SocketException: Connection refused
```

**Solutions:**
- ? Check broker is running: `netstat -an | findstr 8883`
- ? Verify firewall allows port 8883
- ? Test with MQTT Explorer: `mqtt://127.0.0.1:8883`
- ? Check ACL configuration allows user
- ? Verify certificate paths in `MqttService.cs`

#### 2. **HTTP 404 Not Found**

**Symptoms:**
```
ApiClient: GetMetricsAsync failed - 404 Not Found
```

**Solutions:**
- ? Ensure FastAPI server is running
- ? Check BaseURL in Settings: `http://127.0.0.1:8000/`
- ? Verify API endpoints: `curl http://127.0.0.1:8000/api/metrics`
- ? Check server logs for errors

#### 3. **Certificate Validation Failed**

**Symptoms:**
```
MqttService: TLS handshake failed - RemoteCertificateNameMismatch
```

**Solutions:**
- ? Set `AllowUntrustedCertificates = true` for self-signed certs
- ? Install CA cert: `mkcert -install`
- ? Regenerate certificates with correct hostname
- ? Disable TLS for testing: `AppSettings.MqttUseTls = false`

#### 4. **Localization Not Working**

**Symptoms:**
- Buttons show old language after switching

**Solutions:**
- ? Navigate away and back to refresh UI
- ? Restart app after language change
- ? Check `AppResources.Instance.CurrentLanguage` is updated

#### 5. **Database Locked**

**Symptoms:**
```
SqliteDataStore: Database is locked
```

**Solutions:**
- ? Ensure `IDataStore` is registered as **Singleton**
- ? Close all apps accessing the database
- ? Delete database: `%LOCALAPPDATA%\Packages\[AppId]\LocalState\shd.db3`

### Debug Output

Enable detailed logging in `MauiProgram.cs`:
```csharp
#if DEBUG
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
#endif
```

---

## ?? Contributing

Contributions are welcome! Please follow these guidelines:

### Branching Strategy
- `main` - Production-ready code
- `develop` - Development branch
- `feature/*` - New features
- `bugfix/*` - Bug fixes

### Pull Request Process
1. Fork the repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open Pull Request

### Code Standards
- Follow Microsoft C# naming conventions
- Add XML documentation for public APIs
- Include unit tests for new features
- Update README.md if needed

---

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## ?? Acknowledgments

- [.NET MAUI Team](https://github.com/dotnet/maui)
- [MQTTnet](https://github.com/dotnet/MQTTnet)
- [LiveChartsCore](https://github.com/beto-rodriguez/LiveCharts2)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [Polly](https://github.com/App-vNext/Polly)

---



---

**Made  using .NET MAUI**

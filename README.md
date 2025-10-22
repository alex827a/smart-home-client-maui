# 🏠 SmartHome2 - .NET MAUI Smart Home Client

[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![MQTT](https://img.shields.io/badge/MQTT-4.3.7-660066?logo=mqtt)](https://mqtt.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Cross-platform smart home monitoring and control application built with .NET MAUI, featuring real-time MQTT updates and HTTP API integration.



## 📋 Table of Contents

- [⚡ Features](#-features)
- [🧩 Architecture](#-architecture)
- [🛠️ Prerequisites](#-prerequisites)
- [⬇️ Installation](#-installation)
- [⚙️ Configuration](#-configuration)
- [▶️ Running the Application](#-running-the-application)
- [🚀 Usage](#-usage)
- [🗂️ Project Structure](#-project-structure)
- [🧑‍💻 Development](#-development)
- [📄 License](#-license)

---

## ⚡ Features

### 📟 Real-time Monitoring
- **Live Metrics Dashboard**: Temperature, Humidity, Power consumption
- **MQTT Push Notifications**: Instant updates without polling
- **Historical Charts**: Interactive LiveCharts with configurable data points
- **Offline Support**: SQLite caching for offline operation

### 🕹️ Device Control
- **Remote Device Management**: Toggle smart home devices (lights, fans, HVAC, etc.)
- **Role-Based Access Control**: Admin and Guest user roles
- **Visual Feedback**: Real-time device state updates

### 🌐 Internationalization
- **Multi-language Support**: English and German 
- **Dynamic Language Switching**: Change language without app restart
- **Localized UI**: All screens fully translated

### 🔒 Security
- **MQTT TLS/SSL**: Encrypted communication with broker
- **Client Certificates**: mTLS authentication support
- **ACL Integration**: Mosquitto ACL enforcement
- **Secure Credentials**: Persistent credential storage


---

## 🧩 Architecture

```
+-----------------------------+        +-----------------------------+
|    MAUI UI (Views, XAML)    |<------>|      ViewModels (MVVM)      |
+-----------------------------+        +-----------------------------+
            |                                   |
            v                                   v
+-----------------------------+        +-----------------------------+
|        Services Layer        |<------>|   Utils / Converters        |
| (ApiClient, MqttService,    |        +-----------------------------+
|  SqliteDataStore, etc.)     |
+-----------------------------+
            |
            v
+-----------------------------+
|        Data Layer           |
|   (Local SQLite, DTOs)      |
+-----------------------------+
            |
            v
+-----------------------------+
|   Backend & Integrations    |
|  - FastAPI Server (HTTP)    |
|  - Mosquitto MQTT Broker    |
+-----------------------------+
```

**Layer Descriptions:**

- **MAUI UI**: User interface built with XAML pages.
- **ViewModels (MVVM)**: Presentation logic, binds UI to business logic.
- **Services Layer**: Handles interactions with external services (HTTP API, MQTT broker), data management and caching.
- **Utils / Converters**: Helpers for data formatting and UI binding.
- **Data Layer**: Local storage and data transfer objects (DTOs).
- **Backend & Integrations**: FastAPI server for RESTful API, and Mosquitto for MQTT real-time communication.

---



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

## 🛠️ Prerequisites

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
   - Repository: https://github.com/alex827a/smart-home-backend.git
   - Default URL: `http://127.0.0.1:8000`

5. **Mosquitto MQTT Broker**
   - Version: 2.0.18+
   - Port: 8883 (TLS) or 1883 (non-TLS)
   - Download: https://mosquitto.org/download/

---

## ⬇️ Installation

### 1. Clone the Repository

```bash
git clone https://github.com/alex827a/smart-home-client-maui.git
cd SmartHome2
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

Or in Visual Studio: `Right-click Solution → Restore NuGet Packages`

#### 3. Setup MQTT Certificates (for TLS)

**⚠️ Certificates not included!**
To enable secure TLS connections, you must generate your own development certificates.

##### How to Generate MQTT Certificates

1. **Install mkcert:**
   ```bash
   # Windows (using Scoop)
   scoop install mkcert

   # macOS (using Homebrew)
   brew install mkcert

   # Linux
   # See instructions: https://github.com/FiloSottile/mkcert#installation
   ```

2. **Generate CA and client certificates:**
   ```bash
   mkcert -install
   mkcert -client localhost 127.0.0.1 ::1
   ```
   This will produce files such as:
   - `localhost+2-client.pem`
   - `localhost+2-client-key.pem`

3. **(Optional) Convert PEM to PFX for Windows:**
   ```powershell
   # Example using OpenSSL
   openssl pkcs12 -export -out client.pfx -inkey localhost+2-client-key.pem -in localhost+2-client.pem -certfile rootCA.pem
   ```

4. **Place the generated files in the project root:**
   - `client.pfx`
   - `client-cert.pem`
   - `client-key.pem`

5. **Update your app and broker configuration to point to these files.**

For more details, refer to [mkcert documentation](https://github.com/FiloSottile/mkcert).


## ⚙️ Configuration

### 1. Backend Server Setup

FastAPI backend repository: [alex827a/smart-home-backend](https://github.com/alex827a/smart-home-backend.git)

Ensure your FastAPI server is running:
...
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

| Setting             | Default                | Description                        |
|---------------------|-----------------------|------------------------------------|
| **Base URL**        | `http://127.0.0.1:8000/` | FastAPI server address             |
| **MQTT Broker**     | `127.0.0.1`           | Mosquitto broker IP                |
| **MQTT Port**       | `8883`                | Broker port (8883=TLS, 1883=plain) |
| **Use TLS**         | `true`                | Enable/disable TLS encryption      |
| **Refresh Interval**| `5` seconds           | Polling interval (fallback)        |
| **Language**        | `en`                  | UI language (en/de)                |

Credentials are configured in `Services/AppSettings.cs`:
```csharp
public static string MqttUsername { get; set; } = "guest"; // or "admin"
public static string MqttPassword { get; set; } = "";      // Set via login
```

---

## ▶️ Running the Application

### Visual Studio 2022

1. Open `SmartHome2.sln`
2. Select target platform:
   - **Windows Machine** (recommended for development)
   - **Android Emulator** / Physical device
   - **iOS Simulator** / Physical device (macOS only)
3. Press `F5` or click **▶️ Start Debugging**

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

## 🚀 Usage

### Login

1. Launch the application
2. Select language: **EN** / **DE**
3. Login options:
   - **Quick Login**: Tap "Guest" or "Admin" buttons
   - **Manual**: Enter credentials

| User  | Username | Password | Permissions                       |
|-------|----------|----------|-----------------------------------|
| Guest | `guest`  | `123`    | View only (metrics, devices)      |
| Admin | `admin`  | `admin`  | Full control (toggle devices, settings) |

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

## 🗂️ Project Structure

```
SmartHome2/
├── Models/                  # Data models (DTOs)
│   └── Dtos.cs              # MetricsDto, DeviceDto
├── Services/                # Business logic
│   ├── ApiClient.cs         # HTTP API client (Polly)
│   ├── MqttService.cs       # MQTT client (MQTTnet)
│   ├── SqliteDataStore.cs   # Local database (SQLite)
│   ├── AppSettings.cs       # Configuration (Preferences)
│   └── IApiClient.cs        # Service interfaces
├── ViewModels/              # MVVM ViewModels
│   ├── DashboardVm.cs       # Dashboard logic
│   ├── DevicesVm.cs         # Device control
│   ├── ChartsVm.cs          # Charts data binding
│   ├── LoginVm.cs           # Authentication
│   └── SettingsVm.cs        # Configuration
├── Views/                   # XAML UI pages
│   ├── LoginPage.xaml       # Login screen
│   ├── DashboardPage.xaml   # Main dashboard
│   ├── DevicesPage.xaml     # Device list
│   ├── ChartsPage.xaml      # Charts visualization
│   └── SettingsPage.xaml    # Settings screen
├── Resources/               # Assets
│   ├── Strings/             # Localization
│   │   └── AppResources.cs  # EN/DE translations
│   ├── Images/              # Images/icons
│   └── Styles/              # XAML styles
├── Utils/                   # Helper classes
│   ├── BoolToOnOffConverter.cs # XAML converters
│   └── BoolToModeConverter.cs
├── Platforms/               # Platform-specific code
│   ├── Windows/
│   ├── Android/
│   └── iOS/
├── MauiProgram.cs           # DI container setup
├── App.xaml.cs              # Application entry point
├── AppShell.xaml            # Navigation shell
├── SmartHome2.csproj        # Project file
└── README.md                # This file
```

---

## 🧑‍💻 Development

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

View logs in Visual Studio: **Debug → Windows → Output** (select "Debug")


## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- [.NET MAUI Team](https://github.com/dotnet/maui)
- [MQTTnet](https://github.com/dotnet/MQTTnet)
- [LiveChartsCore](https://github.com/beto-rodriguez/LiveCharts2)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [Polly](https://github.com/App-vNext/Polly)

---

**Made using .NET MAUI**

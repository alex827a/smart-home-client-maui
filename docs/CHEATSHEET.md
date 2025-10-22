# ?? SmartHome2 Cheat Sheet

Quick reference for common tasks.

---

## ?? Quick Commands

### Build & Run
```bash
# Restore packages
dotnet restore

# Build for Windows
dotnet build -f net8.0-windows10.0.19041.0

# Run on Windows
dotnet run -f net8.0-windows10.0.19041.0

# Build for Android
dotnet build -f net8.0-android

# Release build
dotnet publish -c Release
```

### Start Backend Services
```bash
# FastAPI Server
uvicorn main:app --reload --port 8000

# Mosquitto MQTT Broker
mosquitto -c mosquitto.conf -v

# Stop Mosquitto
mosquitto -p /var/run/mosquitto.pid --kill
```

---

## ?? Default Credentials

| User | Username | Password | Role | Permissions |
|------|----------|----------|------|-------------|
| Admin | `admin` | `admin` | Admin | Full control |
| Guest | `guest` | `123` | Guest | Read-only |

---

## ?? Default URLs & Ports

| Service | URL | Port | Protocol |
|---------|-----|------|----------|
| FastAPI | http://127.0.0.1:8000 | 8000 | HTTP |
| Mosquitto (TLS) | mqtt://127.0.0.1:8883 | 8883 | MQTTS |
| Mosquitto (Plain) | mqtt://127.0.0.1:1883 | 1883 | MQTT |

---

## ?? MQTT Topics

### Subscribe (MAUI App)
```csharp
await _mqttClient.SubscribeAsync("home/+/metrics");  // Sensor data
await _mqttClient.SubscribeAsync("home/+/state");    // Device states
```

### Publish (FastAPI Server)
```python
client.publish("home/sensor1/metrics", json.dumps(payload), qos=0)
client.publish("home/lamp/state", json.dumps(payload), qos=1)
```

### Test with mosquitto_sub
```bash
# Subscribe to all topics
mosquitto_sub -h 127.0.0.1 -p 8883 -t '#' -u admin -P admin --cafile ca.crt -v

# Subscribe to metrics only
mosquitto_sub -h 127.0.0.1 -p 8883 -t 'home/+/metrics' -u guest -P 123
```

### Test with mosquitto_pub
```bash
# Publish test metrics
mosquitto_pub -h 127.0.0.1 -p 8883 -t 'home/sensor/metrics' \
  -m '{"temp":22.5,"humidity":45,"power":320,"ts":"2024-01-15T14:30:00Z"}' \
  -u admin -P admin

# Publish device state
mosquitto_pub -h 127.0.0.1 -p 8883 -t 'home/lamp/state' \
  -m '{"id":"lamp","name":"Living Room Light","isOn":true,"lastSeen":"2024-01-15T14:30:00Z"}' \
  -u admin -P admin
```

---

## ?? API Endpoints

### GET /api/metrics
```bash
curl http://127.0.0.1:8000/api/metrics
```

### GET /api/devices
```bash
curl -u admin:admin http://127.0.0.1:8000/api/devices
```

### POST /api/devices/{id}/toggle
```bash
curl -X POST -u admin:admin http://127.0.0.1:8000/api/devices/lamp/toggle
```

---

## ?? Important Files

| File | Location | Purpose |
|------|----------|---------|
| App Settings | `Services/AppSettings.cs` | Configuration |
| Localization | `Resources/Strings/AppResources.cs` | Translations (EN/DE) |
| Database | `%LOCALAPPDATA%/.../ shd.db3` | SQLite cache |
| Certificates | `client.pfx`, `client-cert.pem` | MQTT TLS |

---

## ?? Debug Output Filters

In Visual Studio Output window:

```
# Show only MQTT logs
MqttService

# Show only API logs
ApiClient

# Show all app logs
SmartHome2
```

---

## ?? Common Fixes

### MQTT Won't Connect
```csharp
// Temporary: Disable TLS
AppSettings.MqttUseTls = false;
AppSettings.MqttPort = 1883;
```

### HTTP 404 Errors
```csharp
// Check BaseURL has trailing slash
AppSettings.BaseUrl = "http://127.0.0.1:8000/";
```

### Certificate Errors
```bash
# Regenerate certificates
mkcert -install
mkcert -client localhost 127.0.0.1 ::1
.\ConvertToPfx.ps1
```

### Database Locked
```powershell
# Delete database (loses cache)
Remove-Item "$env:LOCALAPPDATA\Packages\*SmartHome2*\LocalState\shd.db3"
```

---

## ?? Useful Code Snippets

### Fire-and-Forget Async
```csharp
private void OnButtonClicked(object sender, EventArgs e)
{
    _ = OnButtonClickedAsync(); // Discard operator
}

private async Task OnButtonClickedAsync()
{
    await DoSomethingAsync();
}
```

### Update UI from Background Thread
```csharp
MainThread.BeginInvokeOnMainThread(() =>
{
    MyProperty = newValue;
});
```

### ConfigureAwait in Services
```csharp
public async Task<T> GetDataAsync()
{
    var result = await _http.GetAsync("api/data").ConfigureAwait(false);
    return await ParseAsync(result).ConfigureAwait(false);
}
```

### CancellationToken Pattern
```csharp
private CancellationTokenSource? _cts;

public async Task LoadAsync()
{
    _cts?.Cancel();
    _cts = new CancellationTokenSource();
    
    try
    {
        await _api.GetDataAsync(_cts.Token);
    }
    catch (OperationCanceledException) { }
}
```

---

## ?? XAML Binding Tips

### Static Resource Binding
```xaml
<Label Text="{Binding Source={x:Static res:AppResources.Instance}, Path=Dashboard}" />
```

### Multi-Binding
```xaml
<Label>
    <Label.FormattedText>
        <FormattedString>
            <Span Text="{Binding Source={x:Static res:AppResources.Instance}, Path=Role}"/>
            <Span Text=": "/>
            <Span Text="{Binding UserRole}"/>
        </FormattedString>
    </Label.FormattedText>
</Label>
```

### Value Converter
```xaml
<ContentPage.Resources>
    <local:BoolToOnOffConverter x:Key="BoolToOnOffConverter"/>
</ContentPage.Resources>

<Label Text="{Binding IsOn, Converter={StaticResource BoolToOnOffConverter}}" />
```

---

## ?? NuGet Packages

```bash
# Add new package
dotnet add package PackageName

# Update package
dotnet add package PackageName --version x.y.z

# List packages
dotnet list package

# Remove package
dotnet remove package PackageName
```

---

## ?? Generate Certificates

```bash
# Install mkcert
# Windows: scoop install mkcert
# macOS: brew install mkcert

# Install CA
mkcert -install

# Generate server certs
mkcert -cert-file server-cert.pem -key-file server-key.pem localhost 127.0.0.1

# Generate client certs
mkcert -client -cert-file client-cert.pem -key-file client-key.pem localhost 127.0.0.1

# Convert to PFX (Windows)
openssl pkcs12 -export -out client.pfx -inkey client-key.pem -in client-cert.pem -passout pass:
```

---

## ?? Performance Tips

1. **Use ObservableCollection efficiently**
   ```csharp
   // Clear once, add all
   Devices.Clear();
   foreach (var d in devices) Devices.Add(d);
   ```

2. **Debounce frequent updates**
   ```csharp
   await Task.Delay(300, _debounceCts.Token);
   ```

3. **Cache heavy computations**
   ```csharp
   private List<T>? _cache;
   public async Task<List<T>> GetAsync()
   {
       return _cache ??= await LoadAsync();
   }
   ```

---

## ?? Platform-Specific Paths

### Windows
```
Database: %LOCALAPPDATA%\Packages\[AppId]\LocalState\shd.db3
Logs:     %LOCALAPPDATA%\Packages\[AppId]\LocalState\logs\
```

### Android
```
Database: /data/data/[package]/files/shd.db3
Logs:     /data/data/[package]/files/logs/
```

### iOS
```
Database: ~/Library/[AppId]/shd.db3
Logs:     ~/Library/[AppId]/logs/
```

---

## ?? Emergency Commands

### Reset App Settings
```csharp
// Add to Settings page
Preferences.Clear();
```

### Force Logout
```csharp
AppSettings.MqttUsername = "";
AppSettings.MqttPassword = "";
AppSettings.CurrentUserRole = "guest";
await Shell.Current.GoToAsync("//LoginPage");
```

### Clear All Caches
```csharp
await _store.ClearDevicesAsync();
await _store.ClearMetricHistoryAsync();
Preferences.Clear();
```

---

**Cheat Sheet Version:** 1.0  
**Keep this handy! ??**

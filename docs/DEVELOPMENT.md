# 🔧 Development Guide

## Development Environment Setup

### Required Extensions (VS Code)

```json
{
  "recommendations": [
    "ms-dotnettools.csdevkit",
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "formulahendry.dotnet-test-explorer",
    "jmrog.vscode-nuget-package-manager"
  ]
}
```

### Workspace Settings

```json
{
  "csharp.format.enable": true,
  "omnisharp.enableEditorConfigSupport": true,
  "editor.formatOnSave": true,
  "editor.defaultFormatter": "ms-dotnettools.csharp"
}
```

## Architecture Overview

### MVVM Pattern

```
View (XAML)
    ↓ Binding
ViewModel (ObservableObject)
    ↓ Uses
Service (Business Logic)
    ↓ Calls
Repository/API (Data Layer)
```

### Dependency Injection

```csharp
// MauiProgram.cs
builder.Services.AddSingleton<IDataStore, SqliteDataStore>();
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddTransient<DashboardVm>();
builder.Services.AddTransient<DashboardPage>();
```

**Lifetime Rules:**
- **Singleton**: Shared state (DB, MQTT, API client)
- **Transient**: Per-navigation instances (ViewModels, Pages)

## Coding Standards

### Naming Conventions

```csharp
// Classes: PascalCase
public class DashboardVm { }

// Interfaces: IPascalCase
public interface IApiClient { }

// Private fields: _camelCase
private readonly IApiClient _api;

// Properties: PascalCase
public string Status { get; set; }

// Methods: PascalCase
public async Task LoadDataAsync() { }

// Constants: PascalCase
public const int MaxRetries = 3;

// Local variables: camelCase
var metrics = await _api.GetMetricsAsync();
```

### Async/Await Guidelines

```csharp
// ✅ DO: Use async Task for event handlers
private void OnButtonClicked(object sender, EventArgs e)
{
    _ = OnButtonClickedAsync(); // Fire-and-forget with discard
}

private async Task OnButtonClickedAsync()
{
    try
    {
        await _api.DoSomethingAsync();
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error: {ex.Message}");
    }
}

// ✅ DO: ConfigureAwait(false) in Services
public async Task<MetricsDto> GetMetricsAsync()
{
    var response = await _http.GetAsync("api/metrics").ConfigureAwait(false);
    return await ParseResponseAsync(response).ConfigureAwait(false);
}

// ❌ DON'T: ConfigureAwait(false) in ViewModels
public async Task LoadDataAsync()
{
    var data = await _api.GetDataAsync(); // Need UI context
    MainThread.BeginInvokeOnMainThread(() => {
        Data = data; // Update UI
    });
}

// ✅ DO: Use CancellationToken
public async Task<T> FetchAsync(CancellationToken ct = default)
{
    return await _http.GetAsync("api/data", ct).ConfigureAwait(false);
}
```

### Resource Management

```csharp
// ✅ DO: Implement IAsyncDisposable for resources
public class MqttService : IMqttService, IAsyncDisposable
{
    private IMqttClient? _mqttClient;
    private CancellationTokenSource? _cts;

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        
        if (_mqttClient != null)
        {
            await _mqttClient.DisconnectAsync().ConfigureAwait(false);
            _mqttClient.Dispose();
        }
    }
}

// ✅ DO: Unsubscribe from events
public void Cleanup()
{
    _mqtt.MetricsReceived -= OnMetricsReceived;
    _mqtt.ConnectionStatusChanged -= OnConnectionChanged;
}
```

### Error Handling

```csharp
// ✅ DO: Log and rethrow
public async Task<MetricsDto> GetMetricsAsync()
{
    try
    {
        var response = await _http.GetAsync("api/metrics").ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await ParseAsync(response).ConfigureAwait(false);
    }
    catch (HttpRequestException ex)
    {
        Debug.WriteLine($"HTTP error: {ex.Message}");
        throw; // Rethrow for caller to handle
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Unexpected error: {ex}");
        throw;
    }
}

// ✅ DO: Provide fallback in ViewModels
public async Task LoadDataAsync()
{
    try
    {
        Metrics = await _api.GetMetricsAsync();
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Load failed: {ex.Message}");
        
        // Fallback to cached data
        Metrics = await _store.LoadMetricsAsync();
        Status = "Offline (cached)";
    }
}
```

## Testing

### Unit Tests (Coming Soon)

```csharp
[Fact]
public async Task GetMetricsAsync_ReturnsValidData()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("*/api/metrics")
            .Respond("application/json", "{ \"temp\": 22.5 }");
    
    var client = new ApiClient(mockHttp.ToHttpClient());

    // Act
    var result = await client.GetMetricsAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(22.5, result.Temp);
}
```

### Integration Tests

```csharp
[Fact]
public async Task MqttService_ConnectsSuccessfully()
{
    // Arrange
    var service = new MqttService(Mock.Of<IDataStore>());

    // Act
    await service.StartAsync();

    // Assert
    Assert.True(service.IsConnected);
}
```

## Debugging

### Visual Studio

```csharp
// Breakpoint on specific condition
if (device.Id == "lamp")
{
    System.Diagnostics.Debugger.Break(); // Break here
}

// Conditional breakpoint
// Right-click breakpoint → Conditions → device.Id == "lamp"

// Tracepoint (log without breaking)
// Right-click breakpoint → Actions → Log message to Output
```

### MQTT Debugging

```bash
# Subscribe to all topics
mosquitto_sub -h 127.0.0.1 -p 8883 -t '#' -u admin -P admin --cafile ca.crt -v

# Publish test message
mosquitto_pub -h 127.0.0.1 -p 8883 -t 'home/sensor/metrics' -m '{"temp":25.5}' -u admin -P admin
```

### HTTP Debugging (Fiddler)

```csharp
// Force HTTP through proxy (for Fiddler)
var handler = new HttpClientHandler
{
    Proxy = new WebProxy("http://127.0.0.1:8888"),
    UseProxy = true
};

builder.Services.AddHttpClient<IApiClient, ApiClient>()
    .ConfigurePrimaryHttpMessageHandler(() => handler);
```

## Performance Tips

### 1. Use ObservableCollection Efficiently

```csharp
// ❌ BAD: Multiple notifications
foreach (var device in devices)
{
    Devices.Add(device); // Triggers UI update each time
}

// ✅ GOOD: Batch update
Devices.Clear();
foreach (var device in devices)
{
    Devices.Add(device);
}
```

### 2. Debounce Frequent Updates

```csharp
private CancellationTokenSource? _debounceCts;

private async Task OnSearchTextChanged(string text)
{
    _debounceCts?.Cancel();
    _debounceCts = new CancellationTokenSource();

    try
    {
        await Task.Delay(300, _debounceCts.Token); // Wait 300ms
        await SearchAsync(text);
    }
    catch (OperationCanceledException) { }
}
```

### 3. Lazy Load Heavy Data

```csharp
private List<MetricsDto>? _cachedHistory;

public async Task<List<MetricsDto>> GetHistoryAsync()
{
    if (_cachedHistory != null)
        return _cachedHistory;

    _cachedHistory = await _store.LoadMetricHistoryAsync(100);
    return _cachedHistory;
}
```

## Git Workflow

### Commit Messages

```
feat: Add dark theme support
fix: Resolve MQTT reconnection issue
docs: Update installation guide
refactor: Extract HTTP client configuration
test: Add unit tests for ApiClient
chore: Update NuGet packages
```

### Branch Naming

```
feature/dark-theme
bugfix/mqtt-reconnect
hotfix/security-patch
release/v1.1.0
```

## Build & Release

### Debug Build

```bash
dotnet build -c Debug
```

### Release Build (Windows)

```bash
dotnet publish -f net8.0-windows10.0.19041.0 -c Release -p:PublishSingleFile=true -p:SelfContained=false
```

### Android APK

```bash
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=myapp.keystore
```

### Version Bumping

```xml
<!-- SmartHome2.csproj -->
<ApplicationDisplayVersion>1.1.0</ApplicationDisplayVersion>
<ApplicationVersion>2</ApplicationVersion>
```

---


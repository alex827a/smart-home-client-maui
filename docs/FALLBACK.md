# ?? Fallback Mode Documentation

## Overview

SmartHome2 ???????????? **?????????????? fallback** ??? ????????????? MQTT ???????. ?????????? ????? ???????? ? ???? ???????:

1. **MQTT Mode** (preferred) - ?????? ??????????? ? Mosquitto ???????
2. **SSE Fallback Mode** - Server-Sent Events ????? HTTP (????? MQTT ??????????)

---

## ??? Architecture

```
???????????????????????????????????????????????????????????
?                  RealtimeService                        ?
?              (Auto-switching logic)                     ?
???????????????????????????????????????????????????????????
                  ?                       ?
        ????????????????????    ????????????????????
        ?   MqttService    ?    ?   SseService     ?
        ?   (Primary)      ?    ?   (Fallback)     ?
        ????????????????????    ????????????????????
                  ?                       ?
        ????????????????????    ????????????????????
        ?  Mosquitto       ?    ?  FastAPI         ?
        ?  Port: 8883      ?    ?  /api/events/    ?
        ????????????????????    ????????????????????
```

---

## ?? How It Works

### Startup Sequence

1. **Check Server Status**
   ```
   GET /api/status
   Response:
   {
     "mqtt_available": true/false,
     "recommended_mode": "mqtt" | "sse"
   }
   ```

2. **Connection Priority**
   - ? ???? `mqtt_available = true` ? ???????? MQTT
   - ? ???? MQTT ?? ???????????? ? ????????????? SSE
   - ??  ???? `mqtt_available = false` ? ????? SSE

3. **Auto-Reconnect**
   - MQTT disconnected ? ????????????? SSE fallback
   - ????????????? ??????? ???????????? MQTT (???????????)

---

## ?? SSE Protocol

### Server-Sent Events Format

```
data: {"topic":"home/system/metrics","payload":{...},"timestamp":"..."}\n\n
data: {"topic":"home/lamp/state","payload":{...},"timestamp":"..."}\n\n
data: {"topic":"system/keepalive","payload":{...},"timestamp":"..."}\n\n
```

### Event Types

| Topic Pattern | Description | Frequency |
|--------------|-------------|-----------|
| `home/*/metrics` | Sensor data (temp, humidity, power) | Every 5s |
| `home/*/state` | Device state updates | On change |
| `system/initial-state` | Initial devices list | On connect |
| `system/connection` | Connection status | On connect |
| `system/keepalive` | Connection health check | Every 30s |

---

## ?? Server Configuration

### FastAPI Server (with SSE support)

```python
# main.py
from fastapi import FastAPI
from fastapi.responses import StreamingResponse
import asyncio
import json

app = FastAPI()

sse_clients: Set[asyncio.Queue] = set()

@app.get("/api/events/stream")
async def event_stream(request: Request):
    """SSE endpoint for fallback mode"""
    client_queue = asyncio.Queue(maxsize=100)
    sse_clients.add(client_queue)
    
    async def event_generator():
        try:
            # Send initial state
            initial_state = {
                "topic": "system/initial-state",
                "payload": {"devices": [...], ...},
                "timestamp": "..."
            }
            yield f"data: {json.dumps(initial_state)}\n\n"
            
            # Stream events
            while True:
                if await request.is_disconnected():
                    break
                
                event = await asyncio.wait_for(
                    client_queue.get(), 
                    timeout=30.0
                )
                yield f"data: {json.dumps(event)}\n\n"
        except:
            pass
        finally:
            sse_clients.discard(client_queue)
    
    return StreamingResponse(
        event_generator(),
        media_type="text/event-stream"
    )

async def broadcast_to_sse_clients(topic: str, payload: str):
    """Broadcast MQTT messages to SSE clients"""
    event_data = {
        "topic": topic,
        "payload": json.loads(payload),
        "timestamp": datetime.now().isoformat()
    }
    
    for client_queue in sse_clients:
        try:
            client_queue.put_nowait(event_data)
        except:
            pass
```

### MQTT Publisher Integration

```python
# When MQTT is available - publish to both
await mqtt_client.publish(topic, payload)
await broadcast_to_sse_clients(topic, payload)  # SSE clients still get updates

# When MQTT is unavailable - only SSE
await broadcast_to_sse_clients(topic, payload)
```

---

## ?? Client (MAUI) Usage

### Automatic Mode (Recommended)

```csharp
// DI container automatically provides RealtimeService
public class DashboardVm
{
    private readonly IRealtimeService _realtime;

    public DashboardVm(IRealtimeService realtime)
    {
        _realtime = realtime;
        
        // Subscribe to events (source is abstracted)
        _realtime.MetricsReceived += OnMetricsReceived;
        _realtime.ConnectionStatusChanged += OnStatusChanged;
    }

    public async Task InitializeAsync()
    {
        // Automatically chooses MQTT or SSE
        await _realtime.StartAsync();
    }
}
```

### Check Current Mode

```csharp
string mode = _realtime.CurrentMode;
// Returns: "mqtt", "sse", or "disconnected"

bool isConnected = _realtime.IsConnected;
// Returns: true if either MQTT or SSE is connected
```

---

## ?? Debugging

### Enable Logging

```csharp
// Services/SseService.cs
System.Diagnostics.Debug.WriteLine($"SseService: Connected to {sseUrl}");
System.Diagnostics.Debug.WriteLine($"SseService: Event received - Topic: {topic}");
```

### Visual Studio Output Window

```
SmartHome2.exe Information: 0 : RealtimeService: Starting...
SmartHome2.exe Information: 0 : RealtimeService: MQTT connection failed
SmartHome2.exe Information: 0 : RealtimeService: Switching to SSE fallback
SmartHome2.exe Information: 0 : SseService: Connected
SmartHome2.exe Information: 0 : SseService: Event received - Topic: home/system/metrics
```

---

## ?? Configuration

### AppSettings.cs

```csharp
public static class AppSettings
{
    // MQTT settings (primary)
    public static string MqttBroker => "127.0.0.1";
    public static int MqttPort => 8883;
    public static bool MqttUseTls => true;
    
    // SSE fallback (automatic via BaseUrl)
    public static string BaseUrl => "http://127.0.0.1:8000/";
    // SSE endpoint: {BaseUrl}api/events/stream
}
```

### Force SSE Mode (for testing)

```csharp
// Temporarily disable MQTT
AppSettings.MqttUseTls = false; // This will trigger SSE fallback
```

---

## ?? Comparison: MQTT vs SSE

| Feature | MQTT | SSE Fallback |
|---------|------|--------------|
| **Latency** | ~10ms | ~50-100ms |
| **Bandwidth** | Low (binary) | Medium (JSON) |
| **Reliability** | QoS 0/1/2 | HTTP reconnect |
| **Firewall** | May be blocked | HTTP (allowed) |
| **Setup** | Requires broker | Built into FastAPI |
| **Read-Only** | No | **Yes** ?? |

?? **Important:** SSE mode is **read-only** (guest mode). Device control requires MQTT or direct HTTP API calls.

---

## ??? Troubleshooting

### SSE not receiving events

1. Check server logs:
   ```bash
   INFO: SSE client connected. Active clients: 1
   INFO: Broadcasting to 1 SSE clients
   ```

2. Test SSE endpoint manually:
   ```bash
   curl -N http://127.0.0.1:8000/api/events/stream
   ```

3. Check firewall/proxy settings (may buffer SSE)

### MQTT fallback not triggering

1. Verify server status endpoint:
   ```bash
   curl http://127.0.0.1:8000/api/status
   ```

2. Check MQTT connection timeout (15s):
   ```csharp
   .WithTimeout(TimeSpan.FromSeconds(15))
   ```

3. Review RealtimeService logs for switch decision

---

## ?? Best Practices

### 1. Always Use RealtimeService

? **Don't** inject `IMqttService` directly
```csharp
public DashboardVm(IMqttService mqtt) // Bad
```

? **Do** inject `IRealtimeService`
```csharp
public DashboardVm(IRealtimeService realtime) // Good
```

### 2. Handle Connection Status

```csharp
_realtime.ConnectionStatusChanged += (sender, status) =>
{
    // Update UI to show current mode
    ConnectionLabel.Text = $"{status} ({_realtime.CurrentMode})";
};
```

### 3. Cache Data for Offline Mode

```csharp
_realtime.MetricsReceived += async (sender, metrics) =>
{
    await _store.SaveMetricsAsync(metrics);
};

// On app start
var cached = await _store.LoadMetricsAsync();
```

---

## ?? Security Considerations

### MQTT Mode
- ? TLS encryption
- ? Username/password authentication
- ? ACL per-topic permissions
- ? Client certificates (optional)

### SSE Mode
- ?? HTTP only (consider HTTPS in production)
- ?? No per-client authentication (guest mode)
- ? Can add Bearer token authentication
- ?? Read-only access recommended

### Production Recommendations

1. **Use HTTPS for SSE**:
   ```csharp
   AppSettings.BaseUrl = "https://your-server.com/";
   ```

2. **Add Authentication Header**:
   ```csharp
   request.Headers.Authorization = 
       new AuthenticationHeaderValue("Bearer", token);
   ```

3. **Rate Limiting**:
   ```python
   from slowapi import Limiter
   limiter = Limiter(key_func=get_remote_address)
   
   @app.get("/api/events/stream")
   @limiter.limit("1/second")
   async def event_stream(request: Request):
       ...
   ```

---

## ?? Summary

- ? **Automatic fallback**: MQTT ? SSE ??? ?????????????
- ? **Transparent API**: ???? ????????? `IRealtimeService`
- ? **Production-ready**: reconnect, error handling, logging
- ? **Guest mode**: read-only ?????? ??? MQTT
- ?? **Limitations**: SSE ?? ???????????? QoS, slower latency

---

**Last Updated:** 2024-01-15  
**Version:** 1.0

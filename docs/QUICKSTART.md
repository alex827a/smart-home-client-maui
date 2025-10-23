# ?? Quick Start Guide

Get SmartHome2 up and running in 5 minutes!

## Prerequisites Checklist

- [ ] Visual Studio 2022 (with .NET MAUI workload)
- [ ] .NET 8.0 SDK
- [ ] FastAPI server running (**Required**)
- [ ] Mosquitto MQTT broker (**Optional** - app uses SSE fallback if unavailable)

## Step-by-Step Setup

### 1. Clone and Restore (2 min)

```bash
git clone https://github.com/yourusername/SmartHome2.git
cd SmartHome2
dotnet restore
```

### 2. Start Backend Services

**Required: FastAPI Server**
```bash
cd /path/to/fastapi-server
uvicorn main:app --reload --port 8000
```

**Optional: Mosquitto MQTT Broker** (recommended for best performance)
```bash
mosquitto -c mosquitto.conf -v
```

> **Note**: If you skip Mosquitto, the app will automatically use SSE fallback mode!

### 3. Run the App (1 min)

**Visual Studio:**
- Open `SmartHome2.sln`
- Select **Windows Machine**
- Press **F5**

**Command Line:**
```bash
dotnet run --framework net8.0-windows10.0.19041.0
```

### 4. Login (30 sec)

- Tap **"Guest"** for quick login
- Or enter:
  - Username: `guest` / Password: `123`
  - Username: `admin` / Password: `admin`

**With MQTT:**
- Login ? Immediate connection
- Status: "Connection: Connected (MQTT)" ??

**Without MQTT (SSE Fallback):**
- Login ? Wait 2-3 seconds (MQTT timeout)
- Status: "Connection: Connected (SSE)" ??
- Data updates continue normally!

### 5. Verify (30 sec)

? Dashboard shows metrics  
? Connection status shows "MQTT" or "SSE"  
? Devices page loads  
? Charts display data  

## Connection Modes

| Mode | Indicator | Setup | Latency |
|------|-----------|-------|---------|
| ?? **MQTT** | `Connected (MQTT)` | FastAPI + Mosquitto | ~10ms |
| ?? **SSE** | `Connected (SSE)` | FastAPI only | ~100ms |
| ?? **Offline** | `Offline (cached)` | None | N/A |

## Troubleshooting

### "MQTT Connection Failed" ? SSE Fallback Activates
```bash
# This is NORMAL if Mosquitto is not running
# App automatically switches to SSE mode

# To verify SSE is working:
curl -N http://127.0.0.1:8000/api/events/stream
# Should stream: data: {...}
```

### Want to use MQTT instead?
```bash
# Start Mosquitto
mosquitto -c mosquitto.conf -v

# Check broker is running
netstat -an | findstr 8883  # Windows
netstat -an | grep 8883     # Linux/Mac

# Restart app - will now use MQTT
```

### "HTTP 404" or "Connection Failed"
```bash
# Verify FastAPI is running
curl http://127.0.0.1:8000/api/metrics

# Check server logs
# Should see: INFO: Application startup complete

# Verify SSE endpoint
curl http://127.0.0.1:8000/api/status
# Should return: {"mqtt_available": false, ...}
```

### "No Data Showing"
1. Check connection status on Dashboard
2. If showing "Offline" ? verify FastAPI is running
3. If showing "SSE" ? SSE fallback is active (normal without MQTT)
4. If showing "MQTT" ? all systems operational

### "Certificate Error" (MQTT Only)
```csharp
// Temporary fix - disable TLS
// Services/AppSettings.cs
public static bool MqttUseTls => false;

// Or switch to SSE fallback (no certificates needed)
// Just don't start Mosquitto
```

## Testing Different Modes

**Test MQTT Mode:**
```bash
# Terminal 1
mosquitto -c mosquitto.conf -v

# Terminal 2
python main.py

# Launch app ? Should show "Connected (MQTT)"
```

**Test SSE Fallback:**
```bash
# Terminal 1 (DON'T start Mosquitto)
python main.py

# Launch app ? Should show "Connected (SSE)"

```

## Next Steps

1. **Understand Connection Modes**: See [FALLBACK.md](FALLBACK.md) for details
2. **Customize Settings**: Tap Settings ? Change server URL
3. **Add Devices**: Configure in FastAPI server
4. **View History**: Tap View Charts ? Switch to History mode
5. **Change Language**: Settings ? Sprache ? Deutsch
6. **Test SSE Fallback**: Stop Mosquitto and watch automatic fallback

## Quick Mode Comparison

| Feature | MQTT Mode | SSE Fallback |
|---------|-----------|--------------|
| Speed | ? Fast (~10ms) | ?? Slower (~100ms) |
| Setup | Requires Mosquitto | Just FastAPI |
| Read Data | ? Yes | ? Yes |
| Control Devices | ? Yes | ?? Limited* |
| Offline Cache | ? Yes | ? Yes |

*Device control in SSE mode requires HTTP API calls (slower than MQTT)

---

**Need help?** See:
- [README.md](../README.md) - Full documentation
- [FALLBACK_QUICKSTART.md](FALLBACK_QUICKSTART.md) - SSE mode guide
- [CONFIGURATION.md](CONFIGURATION.md) - Advanced setup

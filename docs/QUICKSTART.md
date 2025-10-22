# ?? Quick Start Guide

Get SmartHome2 up and running in 5 minutes!

## Prerequisites Checklist

- [ ] Visual Studio 2022 (with .NET MAUI workload)
- [ ] .NET 8.0 SDK
- [ ] FastAPI server running
- [ ] Mosquitto MQTT broker running

## Step-by-Step Setup

### 1. Clone and Restore (2 min)

```bash
git clone https://github.com/yourusername/SmartHome2.git
cd SmartHome2
dotnet restore
```

### 2. Start Backend Services (1 min)

**Terminal 1 - FastAPI:**
```bash
cd /path/to/fastapi-server
uvicorn main:app --reload --port 8000
```

**Terminal 2 - Mosquitto:**
```bash
mosquitto -c mosquitto.conf -v
```

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

### 5. Verify (30 sec)

? Dashboard shows metrics  
? MQTT status: "Connected"  
? Devices page loads  
? Charts display data  

## Troubleshooting

### "MQTT Connection Failed"
```bash
# Check broker is running
netstat -an | findstr 8883

# Test connection
mqtt-explorer connect mqtt://127.0.0.1:8883
```

### "HTTP 404"
```bash
# Verify FastAPI
curl http://127.0.0.1:8000/api/metrics

# Check server logs
# Should see: INFO: Application startup complete
```

### "Certificate Error"
```csharp
// Temporary fix - disable TLS
// AppSettings.cs
public static bool MqttUseTls => false;
```

## Next Steps

1. **Customize Settings**: Tap Settings ? Change server URL
2. **Add Devices**: Configure in FastAPI server
3. **View History**: Tap View Charts ? Switch to History mode
4. **Change Language**: Settings ? Sprache ? Deutsch

---

**Need help?** See [README.md](README.md) for detailed documentation.

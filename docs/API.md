# ?? API Reference

Backend API endpoints required for SmartHome2 client.

## Base URL

```
http://127.0.0.1:8000
```

All endpoints are prefixed with `/api/`.

---

## Endpoints

### 1. Get Metrics

**Endpoint:** `GET /api/metrics`

**Description:** Returns current sensor metrics (temperature, humidity, power).

**Authentication:** Optional (Basic Auth or JWT)

**Request:**
```http
GET /api/metrics HTTP/1.1
Host: 127.0.0.1:8000
Accept: application/json
```

**Response:** `200 OK`
```json
{
  "temp": 22.5,
  "humidity": 45,
  "power": 320,
  "ts": "2024-01-15T14:30:00Z"
}
```

**C# Client:**
```csharp
var metrics = await apiClient.GetMetricsAsync();
Console.WriteLine($"Temperature: {metrics.Temp}°C");
```

---

### 2. Get Devices

**Endpoint:** `GET /api/devices`

**Description:** Returns list of all smart home devices.

**Authentication:** Required

**Request:**
```http
GET /api/devices HTTP/1.1
Host: 127.0.0.1:8000
Authorization: Basic YWRtaW46YWRtaW4=
Accept: application/json
```

**Response:** `200 OK`
```json
[
  {
    "id": "lamp",
    "name": "Living Room Light",
    "isOn": true,
    "lastSeen": "2024-01-15T14:29:55Z"
  },
  {
    "id": "fan",
    "name": "Kitchen Fan",
    "isOn": false,
    "lastSeen": "2024-01-15T14:29:50Z"
  },
  {
    "id": "hvac",
    "name": "Bedroom AC",
    "isOn": true,
    "lastSeen": "2024-01-15T14:29:58Z"
  }
]
```

**C# Client:**
```csharp
var devices = await apiClient.GetDevicesAsync();
foreach (var device in devices)
{
    Console.WriteLine($"{device.Name}: {(device.IsOn ? "ON" : "OFF")}");
}
```

---

### 3. Toggle Device

**Endpoint:** `POST /api/devices/{id}/toggle`

**Description:** Toggles device state (ON ? OFF or OFF ? ON).

**Authentication:** Required (Admin only)

**URL Parameters:**
- `id` (string): Device ID (e.g., "lamp", "fan", "hvac")

**Request:**
```http
POST /api/devices/lamp/toggle HTTP/1.1
Host: 127.0.0.1:8000
Authorization: Basic YWRtaW46YWRtaW4=
Content-Length: 0
```

**Response:** `200 OK`
```json
{
  "id": "lamp",
  "name": "Living Room Light",
  "isOn": false,
  "lastSeen": "2024-01-15T14:30:05Z"
}
```

**C# Client:**
```csharp
var updatedDevice = await apiClient.ToggleDeviceAsync("lamp");
Console.WriteLine($"{updatedDevice.Name} is now {(updatedDevice.IsOn ? "ON" : "OFF")}");
```

---

## MQTT Topics

The FastAPI server should publish to these MQTT topics:

### Metrics Topic

**Topic:** `home/{sensor_id}/metrics`

**QoS:** 0 (At most once)

**Payload:**
```json
{
  "temp": 22.5,
  "humidity": 45,
  "power": 320,
  "ts": "2024-01-15T14:30:00Z"
}
```

**Publishing Frequency:** Every 5 seconds (configurable)

**Example (Python):**
```python
import paho.mqtt.client as mqtt
import json

client = mqtt.Client()
client.connect("127.0.0.1", 8883)

payload = {
    "temp": 22.5,
    "humidity": 45,
    "power": 320,
    "ts": datetime.utcnow().isoformat() + "Z"
}

client.publish("home/sensor1/metrics", json.dumps(payload), qos=0)
```

---

### Device State Topic

**Topic:** `home/{device_id}/state`

**QoS:** 1 (At least once)

**Payload:**
```json
{
  "id": "lamp",
  "name": "Living Room Light",
  "isOn": true,
  "lastSeen": "2024-01-15T14:30:05Z"
}
```

**Trigger:** When device state changes (toggle)

**Example (Python):**
```python
def on_device_toggle(device_id, new_state):
    payload = {
        "id": device_id,
        "name": get_device_name(device_id),
        "isOn": new_state,
        "lastSeen": datetime.utcnow().isoformat() + "Z"
    }
    
    client.publish(f"home/{device_id}/state", json.dumps(payload), qos=1)
```

---

## Error Responses

### 400 Bad Request
```json
{
  "detail": "Invalid request parameters"
}
```

### 401 Unauthorized
```json
{
  "detail": "Authentication credentials were not provided"
}
```

### 403 Forbidden
```json
{
  "detail": "You do not have permission to perform this action"
}
```

### 404 Not Found
```json
{
  "detail": "Device not found"
}
```

### 500 Internal Server Error
```json
{
  "detail": "An unexpected error occurred"
}
```

---

## Authentication

### Basic Auth

**Format:** `Authorization: Basic <base64(username:password)>`

**Example:**
```http
Authorization: Basic YWRtaW46YWRtaW4=
```

**C# Implementation:**
```csharp
var credentials = Convert.ToBase64String(
    Encoding.ASCII.GetBytes($"{username}:{password}")
);
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Basic", credentials);
```

### MQTT Authentication

**Username:** `admin` or `guest`  
**Password:** As configured in `mosquitto.conf`

**C# Implementation:**
```csharp
var options = new MqttClientOptionsBuilder()
    .WithTcpServer("127.0.0.1", 8883)
    .WithCredentials("admin", "admin")
    .Build();

await mqttClient.ConnectAsync(options);
```

---

## Rate Limiting

### HTTP Endpoints
- **Rate:** 100 requests/minute per IP
- **Burst:** 20 requests/second

**Response Header:**
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1705329600
```

### MQTT Publishing
- **Rate:** Unlimited (controlled by server)
- **Client subscriptions:** 10 topics per connection

---

## Testing

### cURL Examples

**Get Metrics:**
```bash
curl http://127.0.0.1:8000/api/metrics
```

**Get Devices (with auth):**
```bash
curl -u admin:admin http://127.0.0.1:8000/api/devices
```

**Toggle Device:**
```bash
curl -X POST -u admin:admin http://127.0.0.1:8000/api/devices/lamp/toggle
```

### Postman Collection

Import this collection for testing:

```json
{
  "info": {
    "name": "SmartHome2 API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Get Metrics",
      "request": {
        "method": "GET",
        "url": "http://127.0.0.1:8000/api/metrics"
      }
    },
    {
      "name": "Get Devices",
      "request": {
        "method": "GET",
        "url": "http://127.0.0.1:8000/api/devices",
        "auth": {
          "type": "basic",
          "basic": [
            {"key": "username", "value": "admin"},
            {"key": "password", "value": "admin"}
          ]
        }
      }
    },
    {
      "name": "Toggle Device",
      "request": {
        "method": "POST",
        "url": "http://127.0.0.1:8000/api/devices/lamp/toggle",
        "auth": {
          "type": "basic",
          "basic": [
            {"key": "username", "value": "admin"},
            {"key": "password", "value": "admin"}
          ]
        }
      }
    }
  ]
}
```

---

**API Version:** 1.0  
**Last Updated:** 2024-01-15

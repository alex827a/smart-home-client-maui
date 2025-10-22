# Configuration 

## Mosquitto MQTT Broker Configuration

### mosquitto.conf (SmartHome2, Windows)

```conf
# ==================================================================
# SmartHome2 MQTT Broker Configuration (Windows Example)
# ==================================================================

per_listener_settings true

# --- mTLS listener for dev ---
listener 8883
# CA that signed client certificates (mkcert root CA)
cafile C:\mosquitto\certs\rootCA.pem
# Broker/server certificate and private key (broker cert, NOT rootCA)
certfile C:\mosquitto\certs\broker-cert.pem
keyfile C:\mosquitto\certs\broker-key.pem

# Require client certificate (mTLS)
require_certificate true
# use_identity_as_username true
# TLS version
# tls_version tlsv1.2

# Temporarily disable use_identity_as_username to test basic TLS connection
# allow_anonymous true is safe here because require_certificate blocks non-cert clients
allow_anonymous false

# Password file for username/password authentication
password_file C:\mosquitto\passwd

# ACL file for topic access control
acl_file C:\mosquitto\acl
```

---

### AppSettings.cs (Current Implementation)

```csharp
public static class AppSettings
{
    // Server
    public static string BaseUrl
    {
        get => Preferences.Get("base_url", "http://127.0.0.1:8000/");
        set => Preferences.Set("base_url", value);
    }
    
    public static int RefreshInterval
    {
        get => Preferences.Get("refresh_interval", 5);
        set => Preferences.Set("refresh_interval", value);
    }

    // MQTT
    public static string MqttBroker
    {
        get => Preferences.Get("mqtt_broker", "127.0.0.1");
        set => Preferences.Set("mqtt_broker", value);
    }

    public static int MqttPort
    {
        get => Preferences.Get("mqtt_port", 8883);
        set => Preferences.Set("mqtt_port", value);
    }

    public static bool MqttUseTls
    {
        get => Preferences.Get("mqtt_use_tls", true);
        set => Preferences.Set("mqtt_use_tls", value);
    }

    public static string MqttUsername
    {
        get => Preferences.Get("mqtt_username", "guest");
        set => Preferences.Set("mqtt_username", value);
    }

    public static string MqttPassword
    {
        get => Preferences.Get("mqtt_password", "");
        set => Preferences.Set("mqtt_password", value);
    }

    // User
    public static string CurrentUserRole
    {
        get => Preferences.Get("current_user_role", "guest");
        set => Preferences.Set("current_user_role", value);
    }

    // Localization
    public static string Language
    {
        get => Preferences.Get("language", "en");
        set
        {
            Preferences.Set("language", value);
            Resources.Strings.AppResources.Instance.CurrentLanguage = value;
        }
    }

    // Computed Properties
    public static bool IsAdmin => 
        CurrentUserRole.Equals("admin", StringComparison.OrdinalIgnoreCase);
    
    public static bool IsGuest => 
        CurrentUserRole.Equals("guest", StringComparison.OrdinalIgnoreCase);
}
```

---

## Certificate Generation Scripts

### generate_certs.sh (Linux/macOS)

```bash
#!/bin/bash
# ==================================================================
# Generate mkcert certificates for SmartHome2
# ==================================================================

set -e

echo "Installing mkcert..."
# macOS: brew install mkcert
# Linux: apt install mkcert / yum install mkcert

echo "Creating local CA..."
mkcert -install

echo "Generating server certificates..."
mkcert -cert-file server-cert.pem \
       -key-file server-key.pem \
       localhost 127.0.0.1 ::1 \
       $(hostname) \
       *.local

echo "Generating client certificates..."
mkcert -client \
       -cert-file client-cert.pem \
       -key-file client-key.pem \
       localhost 127.0.0.1 ::1

echo "Converting client cert to PFX for Windows..."
openssl pkcs12 -export \
    -out client.pfx \
    -inkey client-key.pem \
    -in client-cert.pem \
    -passout pass:

echo "Getting CA certificate..."
cp "$(mkcert -CAROOT)/rootCA.pem" ca-cert.pem

echo "✅ Certificates generated successfully!"
echo ""
echo "Files created:"
echo "  - server-cert.pem (MQTT broker)"
echo "  - server-key.pem (MQTT broker)"
echo "  - client-cert.pem (MAUI app)"
echo "  - client-key.pem (MAUI app)"
echo "  - client.pfx (MAUI app - Windows)"
echo "  - ca-cert.pem (CA certificate)"
```

---

### ConvertToPfx.ps1 (Windows)

```powershell
# ==================================================================
# Convert PEM certificates to PFX for Windows
# ==================================================================

Write-Host "Converting client certificates to PFX..." -ForegroundColor Cyan

if (-not (Test-Path "client-cert.pem")) {
    Write-Error "client-cert.pem not found!"
    exit 1
}

if (-not (Test-Path "client-key.pem")) {
    Write-Error "client-key.pem not found!"
    exit 1
}

# Using OpenSSL (requires OpenSSL for Windows)
& openssl pkcs12 -export `
    -out client.pfx `
    -inkey client-key.pem `
    -in client-cert.pem `
    -passout pass:

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ client.pfx created successfully!" -ForegroundColor Green
} else {
    Write-Error "Failed to create PFX file"
}
```

---

### acl (SmartHome2, Windows)

```conf
# ==================================================================
# SmartHome2 ACL Configuration
# ==================================================================

user admin
topic readwrite #

user guest
topic read home/+/metrics
topic read home/+/state
```

---

### Create Users (Windows)

```powershell
# Create password file with admin user
mosquitto_passwd -c C:\mosquitto\passwd admin
# Enter password: admin

# Add guest user
mosquitto_passwd C:\mosquitto\passwd guest
# Enter password: 123

# View password file
cat C:\mosquitto\passwd
# Output:
# admin:$7$101$...
# guest:$7$101$...
```

---

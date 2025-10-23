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

#!/usr/bin/env python3
"""
SSE Client Example - Demonstrates fallback mode without MQTT
?????????? ??? ???????? SSE ????? ????? Mosquitto ??????????
"""

import asyncio
import json
import httpx
from datetime import datetime


async def test_server_status():
    """????????? ??????????? ??????? ? ????????????? ?????"""
    async with httpx.AsyncClient() as client:
        try:
            response = await client.get("http://127.0.0.1:8000/api/status")
            status = response.json()
            
            print("=" * 60)
            print("?? SERVER STATUS")
            print("=" * 60)
            print(f"MQTT Available: {status['mqtt_available']}")
            print(f"MQTT Broker: {status['mqtt_broker']}:{status['mqtt_port']}")
            print(f"Recommended Mode: {status['recommended_mode'].upper()}")
            print(f"SSE Clients: {status['sse_clients_count']}")
            print()
            
            if status['mqtt_available']:
                print("? MQTT ?????: ?????? ?????????? (R/W)")
            else:
                print("??  SSE ?????: ?????? ?????? (guest mode)")
            
            return status
        except Exception as e:
            print(f"? Server not available: {e}")
            return None


async def sse_client():
    """SSE ?????? - ???????? ??????? ? ???????? ???????"""
    url = "http://127.0.0.1:8000/api/events/stream"
    
    print("\n" + "=" * 60)
    print("?? SSE CLIENT (FALLBACK MODE)")
    print("=" * 60)
    print(f"Connecting to {url}...\n")
    
    async with httpx.AsyncClient() as client:
        try:
            async with client.stream("GET", url) as response:
                if response.status_code != 200:
                    print(f"? Connection failed: {response.status_code}")
                    return
                
                print(f"? Connected to SSE stream\n")
                
                async for line in response.aiter_lines():
                    if line.startswith("data: "):
                        try:
                            event_str = line[6:]  # Remove "data: " prefix
                            event = json.loads(event_str)
                            
                            topic = event.get("topic", "unknown")
                            payload = event.get("payload", {})
                            timestamp = event.get("timestamp", "?")
                            
                            # Format timestamp
                            ts_display = timestamp.split("T")[1] if "T" in timestamp else "?"
                            
                            # Handle different event types
                            if topic == "system/initial-state":
                                print(f"[{ts_display}] ?? Initial State:")
                                if "devices" in payload:
                                    for device in payload["devices"]:
                                        status = "?? ON" if device.get("isOn") else "?? OFF"
                                        print(f"           - {device.get('name')} ({device.get('id')}): {status}")
                                print()
                            
                            elif topic == "system/keepalive":
                                mqtt_status = "?" if payload.get("mqtt_available") else "?"
                                print(f"[{ts_display}] ?? Keepalive: MQTT {mqtt_status}")
                            
                            elif "metrics" in topic:
                                print(f"[{ts_display}] ?? Metrics:")
                                print(f"           - Temp: {payload.get('temp')}°C")
                                print(f"           - Humidity: {payload.get('humidity')}%")
                                print(f"           - Power: {payload.get('power')}W")
                                print()
                            
                            elif "state" in topic:
                                device_name = payload.get("name", "Unknown")
                                is_on = "?? ON" if payload.get("isOn") else "?? OFF"
                                print(f"[{ts_display}] ?? Device State:")
                                print(f"           - {device_name} is now {is_on}")
                                print()
                            
                            else:
                                print(f"[{ts_display}] ?? Event: {topic}")
                        
                        except json.JSONDecodeError:
                            print(f"??  Failed to parse JSON: {line}")
                        except Exception as e:
                            print(f"??  Error processing event: {e}")
        
        except asyncio.CancelledError:
            print("\n?? Client stopped by user")
        except Exception as e:
            print(f"? Connection error: {e}")


async def test_http_api():
    """????????? HTTP API endpoints"""
    print("\n" + "=" * 60)
    print("?? HTTP API ENDPOINTS")
    print("=" * 60 + "\n")
    
    async with httpx.AsyncClient() as client:
        # GET /api/metrics
        print("1??  GET /api/metrics")
        try:
            response = await client.get("http://127.0.0.1:8000/api/metrics")
            if response.status_code == 200:
                metrics = response.json()
                print(f"   ? Temp: {metrics['temp']}°C, Humidity: {metrics['humidity']}%, Power: {metrics['power']}W")
            else:
                print(f"   ? Failed: {response.status_code}")
        except Exception as e:
            print(f"   ? Error: {e}")
        print()
        
        # GET /api/devices
        print("2??  GET /api/devices")
        try:
            response = await client.get("http://127.0.0.1:8000/api/devices")
            if response.status_code == 200:
                devices = response.json()
                print(f"   ? Found {len(devices)} devices:")
                for device in devices:
                    status = "??" if device['isOn'] else "??"
                    print(f"      {status} {device['name']} ({device['id']})")
            else:
                print(f"   ? Failed: {response.status_code}")
        except Exception as e:
            print(f"   ? Error: {e}")
        print()
        
        # POST /api/devices/{id}/toggle
        print("3??  POST /api/devices/lamp/toggle")
        try:
            response = await client.post("http://127.0.0.1:8000/api/devices/lamp/toggle")
            if response.status_code == 200:
                device = response.json()
                status = "?? ON" if device['isOn'] else "?? OFF"
                print(f"   ? Lamp toggled to {status}")
            else:
                print(f"   ? Failed: {response.status_code}")
        except Exception as e:
            print(f"   ? Error: {e}")


async def main():
    """Main entry point"""
    print("\n")
    print("?" + "=" * 58 + "?")
    print("?" + " " * 58 + "?")
    print("?" + " ?? SmartHome2 - SSE Fallback Client Demo ".center(58) + "?")
    print("?" + " " * 58 + "?")
    print("?" + "=" * 58 + "?")
    print()
    
    # Test server status
    status = await test_server_status()
    
    if status is None:
        print("\n? Server not available. Start FastAPI server:")
        print("   python main.py")
        return
    
    # Test HTTP API
    await test_http_api()
    
    # Start SSE client (Ctrl+C to stop)
    print("\nStarting SSE client (press Ctrl+C to stop)...\n")
    try:
        await sse_client()
    except KeyboardInterrupt:
        print("\n\n?? Goodbye!")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n\n?? Goodbye!")

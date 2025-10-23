using System.Text.Json.Serialization;

namespace SmartHome2.Models
{
    public record MetricsDto(
        [property: JsonPropertyName("temp")] double Temp,
        [property: JsonPropertyName("humidity")] int Humidity,
        [property: JsonPropertyName("power")] int Power,
        [property: JsonPropertyName("ts")] string Ts
    );

    public record DeviceDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("isOn")] bool IsOn,
        [property: JsonPropertyName("lastSeen")] string LastSeen
    );
}
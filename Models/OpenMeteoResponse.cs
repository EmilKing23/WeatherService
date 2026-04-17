using System.Text.Json.Serialization;

namespace WeatherService.Models
{
    public sealed class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public OpenMeteoDaily? Daily { get; set; }
    }

    public sealed class OpenMeteoDaily
    {
        [JsonPropertyName("time")]
        public string[] Time { get; set; } = [];

        [JsonPropertyName("temperature_2m_max")]
        public double[] TemperatureMaxC { get; set; } = [];

        [JsonPropertyName("weather_code")]
        public int[] WeatherCode { get; set; } = [];
    }

    public sealed class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public GeocodingResult[] Results { get; set; } = [];
    }

    public sealed class GeocodingResult
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }
}

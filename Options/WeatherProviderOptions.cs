namespace WeatherService.Options
{
    public sealed class WeatherProviderOptions
    {
        public const string SectionName = "WeatherProvider";
        public string ForecastBaseUrl { get; set; } = "https://api.open-meteo.com/v1";
        public string ArchiveBaseUrl { get; set; } = "https://archive-api.open-meteo.com/v1";
        public string GeocodingBaseUrl { get; set; } = "https://geocoding-api.open-meteo.com/v1";
        public int TimeoutSeconds { get; set; } = 5;
        public int RetryCount { get; set; } = 2;
    }
}

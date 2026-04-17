namespace WeatherService.Options
{
    public sealed class WeatherCacheOptions
    {
        public const string SectionName = "WeatherCache";
        public int TtlMinutes { get; set; } = 30;
    }
}

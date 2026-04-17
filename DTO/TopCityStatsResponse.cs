namespace WeatherService.DTO
{
    public sealed record TopCityStatsResponse(
        string City,
        int Count,
        int CacheHits,
        double AverageLatencyMs);
}

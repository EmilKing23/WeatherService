namespace WeatherService.DTO
{
    public sealed record RequestLogResponse(
        int Id,
        DateTime TimestampUtc,
        string Endpoint,
        string City,
        string? Date,
        bool CacheHit,
        int StatusCode,
        int LatencyMs);
}

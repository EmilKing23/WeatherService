namespace WeatherService.DTO
{
    public sealed record WeatherDayResponse(
        string City,
        string Date,
        string Condition,
        double TemperatureC,
        string IconUrl,
        string Source,
        DateTime FetchedAt);
}

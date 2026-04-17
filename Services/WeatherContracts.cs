namespace WeatherService.Services
{
    public sealed record WeatherDayResult(
        string City,
        DateOnly Date,
        string Condition,
        double TemperatureC,
        string Source,
        DateTime FetchedAtUtc);

    public sealed record WeatherWeekResult(
        string City,
        IReadOnlyCollection<WeatherDayResult> Days,
        string Source,
        DateTime FetchedAtUtc);
}

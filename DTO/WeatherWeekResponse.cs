namespace WeatherService.DTO
{
    public sealed record WeatherWeekResponse(
        string City,
        IReadOnlyCollection<WeatherWeekDayResponse> Days,
        string Source,
        DateTime FetchedAt);
}

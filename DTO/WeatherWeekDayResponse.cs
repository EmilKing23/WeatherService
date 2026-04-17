namespace WeatherService.DTO
{
    public sealed record WeatherWeekDayResponse(
        string Date,
        string Condition,
        double TemperatureC,
        string IconUrl);
}

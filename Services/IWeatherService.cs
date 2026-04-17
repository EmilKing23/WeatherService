namespace WeatherService.Services
{
    public interface IWeatherService
    {
        Task<WeatherDayResult> GetDayAsync(string city, DateOnly date, CancellationToken cancellationToken = default);
        Task<WeatherWeekResult> GetWeekAsync(string city, CancellationToken cancellationToken = default);
    }
}

using WeatherService.Models;

namespace WeatherService.Providers
{
    public interface IWeatherProvider
    {
        string SourceName { get; }
        Task<WeatherLocation> GetCoordinatesAsync(string city, CancellationToken cancellationToken = default);
        Task<WeatherDay> GetWeatherAsync(double lat, double lon, DateTime date, CancellationToken cancellationToken = default);
        Task<List<WeatherDay>> GetWeekAsync(double lat, double lon, CancellationToken cancellationToken = default);
    }

    public sealed record WeatherLocation(string Name, double Latitude, double Longitude, string? Country);
}

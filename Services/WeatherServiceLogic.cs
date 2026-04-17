using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WeatherService.Data;
using WeatherService.Models;
using WeatherService.Options;
using WeatherService.Providers;

namespace WeatherService.Services
{
    public class WeatherServiceLogic : IWeatherService
    {
        private static readonly Regex MultiSpaceRegex = new("\\s+", RegexOptions.Compiled);

        private readonly IWeatherProvider _provider;
        private readonly IMemoryCache _cache;
        private readonly AppDbContext _db;
        private readonly WeatherCacheOptions _cacheOptions;
        private readonly ILogger<WeatherServiceLogic> _logger;

        public WeatherServiceLogic(
            IWeatherProvider provider,
            IMemoryCache cache,
            AppDbContext db,
            IOptions<WeatherCacheOptions> cacheOptions,
            ILogger<WeatherServiceLogic> logger)
        {
            _provider = provider;
            _cache = cache;
            _db = db;
            _cacheOptions = cacheOptions.Value;
            _logger = logger;
        }

        public async Task<WeatherWeekResult> GetWeekAsync(string city, CancellationToken cancellationToken = default)
        {
            var normalizedCity = NormalizeCity(city);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var cacheKey = $"weather-week:{_provider.SourceName}:{normalizedCity}:{today:yyyy-MM-dd}";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (_cache.TryGetValue(cacheKey, out WeatherWeekResult? cachedWeek) && cachedWeek is not null)
                {
                    _logger.LogInformation("Cache hit for weekly weather in {City}.", normalizedCity);
                    await LogRequestAsync("week", normalizedCity, cachedWeek.City, null, true, 200, stopwatch.ElapsedMilliseconds, cancellationToken);
                    return cachedWeek;
                }

                _logger.LogInformation("Cache miss for weekly weather in {City}.", normalizedCity);
                var location = await _provider.GetCoordinatesAsync(city, cancellationToken);
                var days = await _provider.GetWeekAsync(location.Latitude, location.Longitude, cancellationToken);
                var fetchedAtUtc = DateTime.UtcNow;
                var weekResult = new WeatherWeekResult(
                    location.Name,
                    days.Select(day => MapDay(day, location.Name, fetchedAtUtc)).ToArray(),
                    _provider.SourceName,
                    fetchedAtUtc);

                _cache.Set(cacheKey, weekResult, TimeSpan.FromMinutes(_cacheOptions.TtlMinutes));
                await LogRequestAsync("week", normalizedCity, weekResult.City, null, false, 200, stopwatch.ElapsedMilliseconds, cancellationToken);
                return weekResult;
            }
            catch (CityNotFoundException)
            {
                await LogRequestAsync("week", normalizedCity, null, null, false, 404, stopwatch.ElapsedMilliseconds, cancellationToken);
                throw;
            }
            catch (ForecastNotFoundException)
            {
                await LogRequestAsync("week", normalizedCity, null, null, false, 404, stopwatch.ElapsedMilliseconds, cancellationToken);
                throw;
            }
            catch (UpstreamUnavailableException)
            {
                await LogRequestAsync("week", normalizedCity, null, null, false, 502, stopwatch.ElapsedMilliseconds, cancellationToken);
                throw;
            }
        }

        public async Task<WeatherDayResult> GetDayAsync(string city, DateOnly date, CancellationToken cancellationToken = default)
        {
            var normalizedCity = NormalizeCity(city);
            var cacheKey = $"weather:{_provider.SourceName}:{normalizedCity}:{date:yyyy-MM-dd}";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (_cache.TryGetValue(cacheKey, out WeatherDayResult? cachedDay) && cachedDay is not null)
                {
                    _logger.LogInformation("Cache hit for daily weather in {City} on {Date}.", normalizedCity, date);
                    await LogRequestAsync("day", normalizedCity, cachedDay.City, date, true, 200, stopwatch.ElapsedMilliseconds, cancellationToken);
                    return cachedDay;
                }

                _logger.LogInformation("Cache miss for daily weather in {City} on {Date}.", normalizedCity, date);
                var location = await _provider.GetCoordinatesAsync(city, cancellationToken);
                var result = await _provider.GetWeatherAsync(location.Latitude, location.Longitude, date.ToDateTime(TimeOnly.MinValue), cancellationToken);
                var fetchedAtUtc = DateTime.UtcNow;
                var dayResult = MapDay(result, location.Name, fetchedAtUtc);

                _cache.Set(cacheKey, dayResult, TimeSpan.FromMinutes(_cacheOptions.TtlMinutes));
                await LogRequestAsync("day", normalizedCity, dayResult.City, date, false, 200, stopwatch.ElapsedMilliseconds, cancellationToken);
                return dayResult;
            }
            catch (CityNotFoundException)
            {
                await LogRequestAsync("day", normalizedCity, null, date, false, 404, stopwatch.ElapsedMilliseconds, cancellationToken);
                throw;
            }
            catch (ForecastNotFoundException)
            {
                await LogRequestAsync("day", normalizedCity, null, date, false, 404, stopwatch.ElapsedMilliseconds, cancellationToken);
                throw;
            }
            catch (UpstreamUnavailableException)
            {
                await LogRequestAsync("day", normalizedCity, null, date, false, 502, stopwatch.ElapsedMilliseconds, cancellationToken);
                throw;
            }
        }

        private WeatherDayResult MapDay(WeatherDay day, string city, DateTime fetchedAtUtc) =>
            new(
                city,
                DateOnly.FromDateTime(day.Date),
                day.Summary,
                day.TemperatureC,
                _provider.SourceName,
                fetchedAtUtc);

        private static string NormalizeCity(string city)
        {
            var trimmed = city.Trim();
            var collapsed = MultiSpaceRegex.Replace(trimmed, " ");
            return collapsed.ToLowerInvariant();
        }

        private async Task LogRequestAsync(
            string endpoint,
            string normalizedCity,
            string? displayCity,
            DateOnly? date,
            bool cacheHit,
            int statusCode,
            long latencyMs,
            CancellationToken cancellationToken)
        {
            _db.Requests.Add(new RequestLog
            {
                TimestampUtc = DateTime.UtcNow,
                EndPoint = endpoint,
                City = normalizedCity,
                DisplayCity = displayCity,
                Date = date?.ToDateTime(TimeOnly.MinValue),
                CacheHit = cacheHit,
                StatusCode = statusCode,
                LatencyMs = (int)Math.Min(latencyMs, int.MaxValue)
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WeatherService.Mapping;
using WeatherService.Models;
using WeatherService.Options;
using WeatherService.Services;

namespace WeatherService.Providers
{
    public class OpenMeteoProvider : IWeatherProvider
    {
        private readonly HttpClient _http;
        private readonly WeatherProviderOptions _options;
        private readonly ILogger<OpenMeteoProvider> _logger;

        public string SourceName => "open-meteo";

        public OpenMeteoProvider(
            HttpClient http,
            IOptions<WeatherProviderOptions> options,
            ILogger<OpenMeteoProvider> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<WeatherLocation> GetCoordinatesAsync(string city, CancellationToken cancellationToken = default)
        {
            var encodedCity = WebUtility.UrlEncode(city);
            var url = $"{_options.GeocodingBaseUrl.TrimEnd('/')}/search?name={encodedCity}&count=1&language=ru&format=json";

            _logger.LogInformation("Resolving city '{City}' via upstream geocoding.", city);
            var response = await GetFromJsonWithRetryAsync<GeocodingResponse>(url, cancellationToken);
            var location = response?.Results.FirstOrDefault();

            if (location is null)
            {
                throw new CityNotFoundException(city);
            }

            return new WeatherLocation(location.Name, location.Latitude, location.Longitude, location.Country);
        }

        public async Task<List<WeatherDay>> GetWeekAsync(double lat, double lon, CancellationToken cancellationToken = default)
        {
            var latitude = lat.ToString(CultureInfo.InvariantCulture);
            var longitude = lon.ToString(CultureInfo.InvariantCulture);
            var url =
                $"{_options.ForecastBaseUrl.TrimEnd('/')}/forecast?latitude={latitude}&longitude={longitude}&daily=temperature_2m_max,weather_code&timezone=auto&forecast_days=7";

            _logger.LogInformation("Fetching weekly weather forecast for coordinates {Latitude}, {Longitude}.", latitude, longitude);
            var response = await GetFromJsonWithRetryAsync<OpenMeteoResponse>(url, cancellationToken);
            return ParseDailyWeather(response);
        }

        public async Task<WeatherDay> GetWeatherAsync(double lat, double lon, DateTime date, CancellationToken cancellationToken = default)
        {
            var requestedDate = DateOnly.FromDateTime(date);
            var latitude = lat.ToString(CultureInfo.InvariantCulture);
            var longitude = lon.ToString(CultureInfo.InvariantCulture);
            var dateText = requestedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var isHistorical = requestedDate < DateOnly.FromDateTime(DateTime.UtcNow);
            var baseUrl = (isHistorical ? _options.ArchiveBaseUrl : _options.ForecastBaseUrl).TrimEnd('/');
            var endpoint = isHistorical ? "archive" : "forecast";
            var url =
                $"{baseUrl}/{endpoint}?latitude={latitude}&longitude={longitude}&start_date={dateText}&end_date={dateText}&daily=temperature_2m_max,weather_code&timezone=auto";

            _logger.LogInformation(
                "Fetching {Kind} daily weather for coordinates {Latitude}, {Longitude} on {Date}.",
                isHistorical ? "historical" : "forecast",
                latitude,
                longitude,
                dateText);

            var response = await GetFromJsonWithRetryAsync<OpenMeteoResponse>(url, cancellationToken);
            var days = ParseDailyWeather(response);

            return days.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == requestedDate)
                ?? throw new ForecastNotFoundException(date);
        }

        private static List<WeatherDay> ParseDailyWeather(OpenMeteoResponse? response)
        {
            var daily = response?.Daily ?? throw new UpstreamUnavailableException("The upstream weather service returned an empty response.");

            if (daily.Time.Length != daily.TemperatureMaxC.Length || daily.Time.Length != daily.WeatherCode.Length)
            {
                throw new UpstreamUnavailableException("The upstream weather service returned inconsistent data.");
            }

            var result = new List<WeatherDay>(daily.Time.Length);

            for (var i = 0; i < daily.Time.Length; i++)
            {
                var code = daily.WeatherCode[i];
                result.Add(new WeatherDay
                {
                    Date = DateTime.ParseExact(daily.Time[i], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    TemperatureC = daily.TemperatureMaxC[i],
                    Code = code,
                    Summary = WeatherCodeMapper.Map(code)
                });
            }

            return result;
        }

        private async Task<T?> GetFromJsonWithRetryAsync<T>(string url, CancellationToken cancellationToken)
        {
            Exception? lastException = null;
            var attempts = Math.Max(1, _options.RetryCount + 1);

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    return await _http.GetFromJsonAsync<T>(url, cancellationToken);
                }
                catch (HttpRequestException ex) when (IsTransient(ex) && attempt < attempts)
                {
                    lastException = ex;
                    var delay = GetRetryDelay(attempt);
                    _logger.LogWarning(ex, "Transient upstream HTTP error on attempt {Attempt}. Retrying in {DelayMs} ms.", attempt, delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < attempts)
                {
                    lastException = ex;
                    var delay = GetRetryDelay(attempt);
                    _logger.LogWarning(ex, "Upstream timeout on attempt {Attempt}. Retrying in {DelayMs} ms.", attempt, delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    throw new UpstreamUnavailableException("The upstream weather service is unavailable.", ex);
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new UpstreamUnavailableException("The upstream weather service timed out.", ex);
                }
            }

            throw new UpstreamUnavailableException("The upstream weather service is unavailable.", lastException);
        }

        private static bool IsTransient(HttpRequestException exception)
        {
            return exception.StatusCode is null
                or HttpStatusCode.RequestTimeout
                or HttpStatusCode.TooManyRequests
                or >= HttpStatusCode.InternalServerError;
        }

        private static TimeSpan GetRetryDelay(int attempt)
        {
            var baseDelayMs = Math.Min(200 * attempt, 1000);
            var jitterMs = Random.Shared.Next(50, 150);
            return TimeSpan.FromMilliseconds(baseDelayMs + jitterMs);
        }
    }
}

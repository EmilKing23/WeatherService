using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherService.Data;
using WeatherService.Models;
using WeatherService.Options;
using WeatherService.Providers;
using WeatherService.Services;
using Xunit;

namespace WeatherService.Tests.Services;

public class WeatherServiceLogicTests
{
    [Fact]
    public async Task GetDayAsync_SecondRequestUsesCacheAndLogsCacheHit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(dbOptions);
        await db.Database.EnsureCreatedAsync();

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var provider = new FakeWeatherProvider();
        var service = new WeatherServiceLogic(
            provider,
            memoryCache,
            db,
            Microsoft.Extensions.Options.Options.Create(new WeatherCacheOptions { TtlMinutes = 30 }),
            NullLogger<WeatherServiceLogic>.Instance);

        var date = new DateOnly(2025, 9, 19);

        var first = await service.GetDayAsync(" Malaga ", date);
        var second = await service.GetDayAsync("malaga", date);

        Assert.Equal(first, second);
        Assert.Equal(1, provider.CoordinateCalls);
        Assert.Equal(1, provider.DayWeatherCalls);

        var logs = await db.Requests
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Equal(2, logs.Count);
        Assert.False(logs[0].CacheHit);
        Assert.True(logs[1].CacheHit);
        Assert.All(logs, log =>
        {
            Assert.Equal("day", log.EndPoint);
            Assert.Equal("malaga", log.City);
            Assert.Equal(200, log.StatusCode);
        });
    }

    private sealed class FakeWeatherProvider : IWeatherProvider
    {
        public string SourceName => "fake-provider";
        public int CoordinateCalls { get; private set; }
        public int DayWeatherCalls { get; private set; }

        public Task<WeatherLocation> GetCoordinatesAsync(string city, CancellationToken cancellationToken = default)
        {
            CoordinateCalls++;
            return Task.FromResult(new WeatherLocation("Malaga", 36.7213, -4.4214, "Spain"));
        }

        public Task<WeatherDay> GetWeatherAsync(double lat, double lon, DateTime date, CancellationToken cancellationToken = default)
        {
            DayWeatherCalls++;
            return Task.FromResult(new WeatherDay
            {
                Date = date,
                TemperatureC = 28.4,
                Code = 0,
                Summary = "clear"
            });
        }

        public Task<List<WeatherDay>> GetWeekAsync(double lat, double lon, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

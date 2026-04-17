using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherService.Data;
using WeatherService.DTO;

namespace WeatherService.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StatsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("top-cities")]
        public async Task<IActionResult> GetTopCities([FromQuery] string? from, [FromQuery] string? to, [FromQuery] int limit = 10)
        {
            if (!TryParseDateRange(from, to, out var fromDate, out var toDate, out var error))
            {
                return BadRequest(new ErrorResponse(error!));
            }

            if (limit <= 0)
            {
                return BadRequest(new ErrorResponse("'limit' must be greater than 0."));
            }

            var rows = await _db.Requests
                .AsNoTracking()
                .Where(x => x.TimestampUtc >= fromDate && x.TimestampUtc <= toDate && x.StatusCode == 200)
                .ToListAsync();

            var result = rows
                .GroupBy(x => x.City)
                .Select(group => new TopCityStatsResponse(
                    group.OrderByDescending(x => x.TimestampUtc).Select(x => x.DisplayCity ?? x.City).FirstOrDefault() ?? group.Key,
                    group.Count(),
                    group.Count(x => x.CacheHit),
                    Math.Round(group.Average(x => x.LatencyMs), 2)))
                .OrderByDescending(x => x.Count)
                .Take(limit)
                .ToList();

            return Ok(result);
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests(
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryParseDateRange(from, to, out var fromDate, out var toDate, out var error))
            {
                return BadRequest(new ErrorResponse(error!));
            }

            if (page <= 0)
            {
                return BadRequest(new ErrorResponse("'page' must be greater than 0."));
            }

            if (pageSize <= 0 || pageSize > 100)
            {
                return BadRequest(new ErrorResponse("'pageSize' must be between 1 and 100."));
            }

            var query = _db.Requests
                .AsNoTracking()
                .Where(x => x.TimestampUtc >= fromDate && x.TimestampUtc <= toDate)
                .OrderByDescending(x => x.TimestampUtc);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RequestLogResponse(
                    x.Id,
                    x.TimestampUtc,
                    x.EndPoint,
                    x.DisplayCity ?? x.City,
                    x.Date == null ? null : DateOnly.FromDateTime(x.Date.Value).ToString("yyyy-MM-dd"),
                    x.CacheHit,
                    x.StatusCode,
                    x.LatencyMs))
                .ToListAsync();

            return Ok(new RequestsPageResponse(page, pageSize, totalCount, items));
        }

        private static bool TryParseDateRange(string? from, string? to, out DateTime fromDate, out DateTime toDate, out string? error)
        {
            error = null;
            fromDate = default;
            toDate = default;

            if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromParsed) ||
                !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toParsed))
            {
                error = "Invalid date range. Use YYYY-MM-DD for 'from' and 'to'.";
                return false;
            }

            fromDate = fromParsed.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            toDate = toParsed.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            if (fromDate > toDate)
            {
                error = "'from' must be earlier than or equal to 'to'.";
                return false;
            }

            return true;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using WeatherService.DTO;
using WeatherService.Services;

namespace WeatherService.Controllers
{
    [ApiController]
    [Route("api/weather")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _service;

        public WeatherController(IWeatherService service)
        {
            _service = service;
        }

        [HttpGet("{city}")] 
        public async Task<IActionResult> GetDay(string city, [FromQuery] string? date, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return BadRequest(new ErrorResponse("City is required."));
            }

            if (string.IsNullOrWhiteSpace(date) || !DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
            {
                return BadRequest(new ErrorResponse("Invalid date. Use YYYY-MM-DD."));
            }

            try
            {
                var result = await _service.GetDayAsync(city, parsedDate, cancellationToken);

                return Ok(new WeatherDayResponse(
                    result.City,
                    result.Date.ToString("yyyy-MM-dd"),
                    result.Condition,
                    result.TemperatureC,
                    BuildIconUrl(result.Condition),
                    result.Source,
                    result.FetchedAtUtc));
            }
            catch (CityNotFoundException)
            {
                return NotFound(new ErrorResponse("City not found"));
            }
            catch (ForecastNotFoundException)
            {
                return NotFound(new ErrorResponse("Forecast not found"));
            }
            catch (UpstreamUnavailableException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Upstream unavailable"));
            }
        }

        [HttpGet("{city}/week")]
        public async Task<IActionResult> GetWeek(string city, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return BadRequest(new ErrorResponse("City is required."));
            }

            try
            {
                var result = await _service.GetWeekAsync(city, cancellationToken);

                return Ok(new WeatherWeekResponse(
                    result.City,
                    result.Days
                        .Select(day => new WeatherWeekDayResponse(
                            day.Date.ToString("yyyy-MM-dd"),
                            day.Condition,
                            day.TemperatureC,
                            BuildIconUrl(day.Condition)))
                        .ToArray(),
                    result.Source,
                    result.FetchedAtUtc));
            }
            catch (CityNotFoundException)
            {
                return NotFound(new ErrorResponse("City not found"));
            }
            catch (UpstreamUnavailableException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Upstream unavailable"));
            }
        }

        private string BuildIconUrl(string code) => $"{Request.Scheme}://{Request.Host}/static/icons/{code}.png";
    }
}

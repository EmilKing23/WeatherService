namespace WeatherService.Services
{
    public sealed class ForecastNotFoundException : Exception
    {
        public ForecastNotFoundException(DateTime date)
            : base($"Forecast not found for {date:yyyy-MM-dd}.")
        {
        }
    }
}

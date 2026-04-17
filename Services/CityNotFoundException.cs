namespace WeatherService.Services
{
    public sealed class CityNotFoundException : Exception
    {
        public CityNotFoundException(string city)
            : base($"City '{city}' was not found.")
        {
        }
    }
}

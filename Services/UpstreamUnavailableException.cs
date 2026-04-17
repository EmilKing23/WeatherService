namespace WeatherService.Services
{
    public sealed class UpstreamUnavailableException : Exception
    {
        public UpstreamUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}

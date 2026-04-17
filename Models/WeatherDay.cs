namespace WeatherService.Models
{
    public class WeatherDay
    {
        public DateTime Date { get; set; }
        public double TemperatureC { get; set; }
        public int Code { get; set; }
        public string Summary { get; set; } = string.Empty;
    }
}

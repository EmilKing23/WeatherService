namespace WeatherService.Data
{
    public class RequestLog
    {
        public int Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public required string EndPoint { get; set; }
        public required string City { get; set; }
        public string? DisplayCity { get; set; }
        public DateTime? Date { get; set; }
        public bool CacheHit { get; set; }
        public int StatusCode { get; set; }
        public int LatencyMs { get; set; }
    }
}

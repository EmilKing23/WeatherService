namespace WeatherService.DTO
{
    public sealed record RequestsPageResponse(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyCollection<RequestLogResponse> Items);
}

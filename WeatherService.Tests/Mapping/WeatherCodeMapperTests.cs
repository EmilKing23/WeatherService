using WeatherService.Mapping;
using Xunit;

namespace WeatherService.Tests.Mapping;

public class WeatherCodeMapperTests
{
    [Theory]
    [InlineData(0, "clear")]
    [InlineData(1, "cloudy")]
    [InlineData(3, "cloudy")]
    [InlineData(45, "fog")]
    [InlineData(48, "fog")]
    [InlineData(51, "rain")]
    [InlineData(67, "rain")]
    [InlineData(80, "rain")]
    [InlineData(82, "rain")]
    [InlineData(71, "snow")]
    [InlineData(77, "snow")]
    [InlineData(85, "snow")]
    [InlineData(86, "snow")]
    [InlineData(95, "thunder")]
    [InlineData(99, "thunder")]
    [InlineData(999, "cloudy")]
    public void Map_ReturnsExpectedIconCode(int providerCode, string expectedCode)
    {
        var actual = WeatherCodeMapper.Map(providerCode);

        Assert.Equal(expectedCode, actual);
    }
}

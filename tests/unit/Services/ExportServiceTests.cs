using FluentAssertions;
using Moq;
using System.Text;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;
using WeatherApi.Core.Services;

namespace WeatherApi.Tests.Unit.Services;

public class ExportServiceTests
{
    private readonly Mock<IWeatherService> _weatherServiceMock;
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly ExportService _sut;

    public ExportServiceTests()
    {
        _weatherServiceMock = new Mock<IWeatherService>();
        _locationServiceMock = new Mock<ILocationService>();
        _sut = new ExportService(_weatherServiceMock.Object, _locationServiceMock.Object);
    }

    [Fact]
    public async Task ExportAsync_ReturnsCsvWithHistoricalData()
    {
        // Arrange
        int locationId = 1;
        string city = "London";
        int days = 3;
        var location = new LocationResponse(locationId, "12345", city, "GB", 51.5, -0.1, DateTimeOffset.UtcNow);

        _locationServiceMock
            .Setup(l => l.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        _weatherServiceMock
            .Setup(w => w.GetHistoricalAsync(locationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int locId, DateTime date, CancellationToken ct) => 
                new WeatherData(city, "GB", 10 + date.Day % 10, "Cloudy", "", 9, 60, 1013, 5, 180, 10000, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new DateTimeOffset(date), new List<HourlySummary>(), new List<DailySummary>()));

        // Act
        var result = await _sut.ExportAsync(locationId, days);

        // Assert
        result.Should().NotBeNull();
        var csv = Encoding.UTF8.GetString(result);
        csv.Should().Contain("City,Country,Temperature(°C)");
        csv.Should().Contain(city);
        var lines = csv.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.Should().Be(days + 1); // Header + data lines
    }

    [Fact]
    public async Task ExportAsync_WhenLocationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        int locationId = 999;
        _locationServiceMock
            .Setup(l => l.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocationResponse?)null);

        // Act & Assert
        await FluentActions.Awaiting(() => _sut.ExportAsync(locationId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}

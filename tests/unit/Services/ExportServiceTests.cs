using Xunit;
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
    private readonly ExportService _svc;

    public ExportServiceTests()
    {
        _weatherServiceMock = new Mock<IWeatherService>();
        _svc = new ExportService(_weatherServiceMock.Object);
    }

    [Fact]
    public async Task ExportAsync_ReturnsCsvWithHistoricalData()
    {
        // Arrange
        int locationId = 1;
        string city = "London";
        int days = 3;
        var location = new LocationResponse(locationId, "12345", city, "GB", 51.5, -0.1, DateTimeOffset.UtcNow);

        _weatherServiceMock
            .Setup(w => w.GetHistoricalAsync(locationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int locId, DateTime date, CancellationToken ct) => 
                new WeatherData(city, "GB", 10 + date.Day % 10, "Cloudy", "", 9, 60, 1013, 5, 180, 10000, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new DateTimeOffset(date), new List<HourlySummary>(), new List<DailySummary>()));

        // Act
        var result = await _svc.ExportAsync(locationId, days);

        // Assert
        result.Should().NotBeNull();
        var csv = Encoding.UTF8.GetString(result);
        csv.Should().Contain("City,Country,Temperature(C)");
        csv.Should().Contain(city);
        var lines = csv.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.Should().Be(days + 1); // Header + data lines
    }

    [Fact]
    public async Task ExportAsync_WhenLocationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        int locationId = 999;
        _weatherServiceMock
            .Setup(w => w.GetHistoricalAsync(locationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Location with ID {locationId} not found."));

        // Act & Assert
        await FluentActions.Awaiting(() => _svc.ExportAsync(locationId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}

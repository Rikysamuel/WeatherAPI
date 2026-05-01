using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text;
using WeatherApi.Core.Data;
using WeatherApi.Core.Data.Entities;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;
using WeatherApi.Core.Services;

namespace WeatherApi.Tests.Unit.Services;

public class ExportServiceTests
{
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly WeatherDbContext _dbContext;
    private readonly ExportService _svc;

    public ExportServiceTests()
    {
        _locationServiceMock = new Mock<ILocationService>();

        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new WeatherDbContext(options);

        _svc = new ExportService(_locationServiceMock.Object, _dbContext);
    }

    [Fact]
    public async Task ExportAsync_ReturnsCsvWithDailyWeatherData()
    {
        // Arrange
        int locationId = 1;
        string city = "London";
        int days = 3;

        _locationServiceMock
            .Setup(x => x.GetByIdOrThrowAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocationResponse(locationId, "12345", city, "GB", 51.5, -0.1, DateTimeOffset.UtcNow));

        var today = DateTime.UtcNow.Date;
        _dbContext.DailyWeather.AddRange(
            new DailyWeatherEntity
            {
                City = city, Country = "GB", Date = today,
                ObservedTemperature = 15.5, ObservedFeelsLike = 14.0,
                ObservedHumidity = 70, ObservedPressure = 1013, ObservedWindSpeed = 5.2,
                ObservedDescription = "clear sky"
            },
            new DailyWeatherEntity
            {
                City = city, Country = "GB", Date = today.AddDays(1),
                PredictedMinTemperature = 12.0, PredictedMaxTemperature = 18.0,
                PredictedDescription = "scattered clouds"
            },
            new DailyWeatherEntity
            {
                City = city, Country = "GB", Date = today.AddDays(2),
                PredictedMinTemperature = 10.5, PredictedMaxTemperature = 16.0,
                PredictedDescription = "light rain"
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _svc.ExportAsync(locationId, days);

        // Assert
        result.Should().NotBeNull();
        var csv = Encoding.UTF8.GetString(result);
        csv.Should().Contain("City,Country,Date (SGT),Min Temp (C),Max Temp (C)");
        csv.Should().Contain(city);
        csv.Should().Contain("clear sky");
        csv.Should().Contain("scattered clouds");
        csv.Should().Contain("light rain");

        var lines = csv.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.Should().Be(days + 1);
    }

    [Fact]
    public async Task ExportAsync_WhenLocationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        int locationId = 999;
        _locationServiceMock
            .Setup(x => x.GetByIdOrThrowAsync(locationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Location with ID {locationId} not found."));

        // Act & Assert
        await FluentActions.Awaiting(() => _svc.ExportAsync(locationId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}

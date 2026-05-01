using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;
using WeatherApi.Core.Services;
using WeatherApi.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace WeatherApi.Tests.Unit.Services;

public class WeatherServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly Mock<IOwmClient> _owmClientMock;
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly Mock<ILogger<WeatherService>> _loggerMock;
    private readonly WeatherService _svc;
    private readonly WeatherDbContext _dbContext;

    public WeatherServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _owmClientMock = new Mock<IOwmClient>();
        _locationServiceMock = new Mock<ILocationService>();
        _loggerMock = new Mock<ILogger<WeatherService>>();
        
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new WeatherDbContext(options);

        _svc = new WeatherService(_cache, _owmClientMock.Object, _dbContext, _locationServiceMock.Object, _loggerMock.Object);
    }

    private void SetupLocation(int locationId, string city, string country = "GB", double lat = 51.5074, double lon = -0.1278)
    {
        var response = new LocationResponse(locationId, null, city, country, lat, lon, DateTimeOffset.UtcNow);
        _locationServiceMock
            .Setup(c => c.GetByIdAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocationResponse?)response);
        _locationServiceMock
            .Setup(c => c.GetByIdOrThrowAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupOneCall(double temp = 15.5, double feelsLike = 14.0, int humidity = 70, double windSpeed = 5.2)
    {
        var response = new OwmOneCallResponse
        {
            Current = new CurrentWeather
            {
                Temp = temp,
                FeelsLike = feelsLike,
                Humidity = humidity,
                WindSpeed = windSpeed,
                Weather = [new WeatherCondition { Description = "scattered clouds", Icon = "03d" }],
                Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            },
            Hourly = [.. Enumerable.Range(0, 24).Select(i => new HourlyForecast
            {
                Dt = DateTimeOffset.UtcNow.AddHours(i).ToUnixTimeSeconds(),
                Temp = temp + i,
                Weather = [new WeatherCondition { Description = "clear sky" }]
            })],
            Daily = [.. Enumerable.Range(0, 7).Select(i => new DailyForecast
            {
                Dt = DateTimeOffset.UtcNow.AddDays(i).ToUnixTimeSeconds(),
                Temp = new TempSummary { Min = temp - 2, Max = temp + 2, Day = temp },
                Weather = [new WeatherCondition { Description = "clear sky" }]
            })]
        };
        _owmClientMock
            .Setup(c => c.GetOneCallAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_WhenFirstCall_FetchesFromOwmAndReturnsData()
    {
        // Arrange
        int locationId = 1;
        string city = "London";
        SetupLocation(locationId, city);
        SetupOneCall();

        // Act
        await _svc.RefreshCurrentWeatherFromOwmAsync(locationId);
        var result = await _svc.GetPersistedCurrentWeatherAsync(locationId);

        // Assert
        result.Should().NotBeNull();
        result.City.Should().Be(city);
        result.Temperature.Should().Be(15.5);
        result.Humidity.Should().Be(70);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_WhenLocationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        int locationId = 999;
        _locationServiceMock
            .Setup(c => c.GetByIdOrThrowAsync(locationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Location with ID {locationId} not found."));

        // Act & Assert
        await FluentActions.Awaiting(() => _svc.GetPersistedCurrentWeatherAsync(locationId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetForecastAsync_ReturnsForecastWithCity()
    {
        // Arrange
        int locationId = 2;
        string city = "Singapore";
        var days = 5;
        SetupLocation(locationId, city);
        SetupOneCall();

        // Act
        var result = await _svc.GetForecastAsync(locationId, days);

        // Assert
        result.Should().NotBeNull();
        result.City.Should().Be(city);
        result.Forecasts.Should().HaveCount(days);
    }

    [Fact]
    public async Task GetHistoricalAsync_ReturnsWeatherDataForDate()
    {
        // Arrange
        int locationId = 3;
        string city = "Tokyo";
        var date = new DateTime(2026, 4, 10);
        SetupLocation(locationId, city);
        
        _owmClientMock
            .Setup(c => c.GetHistoricalWeatherAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OwmHistoricalResponse
            {
                Data = new[] { new CurrentWeather { Temp = 10, Dt = new DateTimeOffset(date).ToUnixTimeSeconds() } }
            });

        // Act
        var result = await _svc.GetHistoricalAsync(locationId, date);

        // Assert
        result.Should().NotBeNull();
        result.City.Should().Be(city);
        result.Temperature.Should().Be(10);
    }
}

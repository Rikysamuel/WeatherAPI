using Xunit;
using FluentAssertions;
using WeatherApi.Core.Data;
using WeatherApi.Core.Models;
using WeatherApi.Core.Services;
using Moq;
using Microsoft.Extensions.Logging;
using WeatherApi.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace WeatherApi.Tests.Unit.Services;

public class LocationServiceTests
{
    private readonly Mock<IOwmClient> _owmClientMock;
    private readonly Mock<ILogger<LocationService>> _loggerMock;
    private readonly LocationService _svc;
    private readonly WeatherDbContext _dbContext;

    public LocationServiceTests()
    {
        _owmClientMock = new Mock<IOwmClient>();
        _loggerMock = new Mock<ILogger<LocationService>>();
        
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new WeatherDbContext(options);
        
        _svc = new LocationService(_dbContext, _owmClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsEmptyListInitially()
    {
        // Act
        var result = await _svc.GetAllOrByNameAsync("Singapore");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        // Act
        var result = await _svc.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }
}

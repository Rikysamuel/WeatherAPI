using FluentAssertions;
using WeatherApi.Core.Data;
using WeatherApi.Core.Models;
using WeatherApi.Core.Services;
using Moq;
using Microsoft.Extensions.Logging;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace WeatherApi.Tests.Unit.Services;

public class LocationServiceTests
{
    private readonly Mock<IOwmClient> _owmClientMock;
    private readonly Mock<ILogger<LocationService>> _loggerMock;
    private readonly LocationService _sut;
    private readonly WeatherDbContext _dbContext;

    public LocationServiceTests()
    {
        _owmClientMock = new Mock<IOwmClient>();
        _loggerMock = new Mock<ILogger<LocationService>>();
        
        // In-memory DB setup (requires Microsoft.EntityFrameworkCore.InMemory)
        // If not available, we can mock DbSet, but using InMemory is better.
        // Assuming user will provide proper environment.
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new WeatherDbContext(options);
        
        _sut = new LocationService(_dbContext, _owmClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedLocation()
    {
        // Arrange
        var dto = new LocationDto("310116", "Singapore", "SG", 1.3521, 103.8198);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.City.Should().Be(dto.City);
        result.Latitude.Should().Be(dto.Latitude);
        result.Longitude.Should().Be(dto.Longitude);
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsEmptyListInitially()
    {
        // Act
        var result = await _sut.FindByNameAsync("Singapore");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }
}

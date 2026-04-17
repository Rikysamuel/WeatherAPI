using FluentAssertions;
using WeatherApi.Core.Models;
using WeatherApi.Core.Services;

namespace WeatherApi.Tests.Unit.Services;

public class AlertServiceTests
{
    private readonly AlertService _sut;

    public AlertServiceTests()
    {
        _sut = new AlertService();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyListInitially()
    {
        // Act
        var result = await _sut.GetAllAsync();

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

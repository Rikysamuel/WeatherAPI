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
    public async Task CreateAsync_ReturnsCreatedAlert()
    {
        // Arrange
        var dto = new AlertDto("Singapore", "Heavy rain expected", AlertSeverity.High);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.City.Should().Be(dto.City);
        result.Message.Should().Be(dto.Message);
        result.Severity.Should().Be(dto.Severity);
        result.IsActive.Should().BeTrue();
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

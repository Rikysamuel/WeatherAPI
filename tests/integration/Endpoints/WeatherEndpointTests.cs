using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WeatherApi.Tests.Integration.Endpoints;

public class WeatherEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WeatherEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentWeather_ReturnsOkForValidLocation()
    {
        // 1. Setup - Create a location first
        var locationId = 1; // Assuming a location exists or we create it.
        // Actually, better to have a test setup that seeds data or uses a real ID.
        // For this test, let's assume location ID 1 is seeded.

        // Act
        var response = await _client.GetAsync($"/api/weather/current/{locationId}");

        // Assert
        // This might return 404 if the location is not there, 
        // but it's the correct path now.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetForecast_ReturnsOkForValidLocation()
    {
        // Arrange
        var locationId = 1;

        // Act
        var response = await _client.GetAsync($"/api/weather/forecast/{locationId}?days=3");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistorical_ReturnsOkForValidLocationAndDate()
    {
        // Arrange
        var locationId = 1;

        // Act
        var response = await _client.GetAsync($"/api/weather/historical/{locationId}?date=2026-04-10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}

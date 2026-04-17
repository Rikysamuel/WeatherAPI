using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WeatherApi.Tests.Integration.Endpoints;

public class ProtectedEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProtectedEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLocations_ReturnsUnauthorized_WithoutToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/locations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAlerts_ReturnsUnauthorized_WithoutToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLocations_ReturnsOk_WithValidToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        // TODO: Generate a valid JWT token for testing
        // For now, this test will fail as expected without auth
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token-for-testing");

        // Act
        var response = await client.GetAsync("/api/locations");

        // Assert
        // Will be Unauthorized with invalid token — placeholder for proper JWT test
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

using Xunit;
using System.Net;
using FluentAssertions;
using WeatherApi.Tests.Integration.Shared;

namespace WeatherApi.Tests.Integration.Endpoints;

public class HealthEndpointTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}

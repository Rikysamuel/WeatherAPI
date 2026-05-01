using Xunit;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using WeatherApi.Tests.Integration.Shared;

namespace WeatherApi.Tests.Integration.Endpoints;

public class ProtectedEndpointTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task GetLocations_ReturnsUnauthorized_WithoutToken()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/location/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAlerts_ReturnsUnauthorized_WithoutToken()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/alert");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLocations_ReturnsOk_WithValidToken()
    {
        await Factory.MockLocation.AddOrGetByCityNameAsync("Singapore");
        var client = Factory.CreateClient();

        var registerPayload = new { username = "testuser", password = "Test1234" };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json");
        var registerResponse = await client.PostAsync("/api/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginPayload = new { username = "testuser", password = "Test1234" };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync("/api/auth/token", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<JsonElement>(loginBody).GetProperty("token").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/location/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

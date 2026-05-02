using Xunit;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherApi.Core.Data;
using WeatherApi.Core.Data.Entities;
using WeatherApi.Tests.Integration.Shared;

namespace WeatherApi.Tests.Integration.Endpoints;

public class WeatherEndpointTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetCurrentWeather_ReturnsOkForValidLocation()
    {
        await Factory.MockLocation.AddOrGetByCityNameAsync("Singapore");
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
        db.DailyWeather.Add(new DailyWeatherEntity
        {
            LocationId = 1, Date = DateTime.UtcNow.Date,
            ObservedTemperature = 30.5, ObservedDescription = "clear sky",
            ObservedFeelsLike = 32, ObservedHumidity = 75, ObservedPressure = 1012,
            ObservedWindSpeed = 5.2, ObservedWindDirection = 180,
            ObservedVisibility = 10000, ObservedUVI = 8,
            ObservedSunrise = DateTimeOffset.UtcNow.AddHours(-6),
            ObservedSunset = DateTimeOffset.UtcNow.AddHours(6),
            ObservedTimestamp = DateTimeOffset.UtcNow
        });
        db.SaveChanges();

        var client = Factory.CreateClient();
        var response = await client.GetAsync($"/api/weather/current/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetForecast_ReturnsOkForValidLocation()
    {
        await Factory.MockLocation.AddOrGetByCityNameAsync("Tokyo");
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
        db.DailyWeather.Add(new DailyWeatherEntity
        {
            LocationId = 1,
            Date = DateTime.UtcNow.Date.AddDays(1),
            PredictedDescription = "sunny",
            PredictedMinTemperature = 20, PredictedMaxTemperature = 28,
            PredictedTemperature = 24
        });
        db.SaveChanges();

        var client = Factory.CreateClient();
        var response = await client.GetAsync($"/api/weather/forecast/1?days=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHistorical_ReturnsOkForValidLocationAndDate()
    {
        await Factory.MockLocation.AddOrGetByCityNameAsync("London");
        var client = Factory.CreateClient();

        var response = await client.GetAsync($"/api/weather/historical/1?date=2026-04-10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

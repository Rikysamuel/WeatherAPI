using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeatherApi.Core.Data;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WeatherApi.Tests.Integration.Shared;

[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory> { }

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public MockOwmClient MockOwm { get; } = new();
    public MockLocationService MockLocation { get; } = new();

    public async Task ResetStateAsync()
    {
        MockLocation.Reset();
        MockOwm.Reset();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<WeatherDbContext>>();
            services.RemoveAll<WeatherDbContext>();
            services.AddDbContext<WeatherDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestDb");
            });

            services.RemoveAll<IOwmClient>();
            services.AddSingleton<IOwmClient>(MockOwm);

            services.RemoveAll<ILocationService>();
            services.AddSingleton<ILocationService>(MockLocation);
        });
    }
}

public class MockOwmClient : IOwmClient
{
    public void Reset()
    {
        // No mutable state to reset currently
    }

    public Task<OwmGeoResult[]?> GeocodeAsync(string city, CancellationToken ct = default)
        => Task.FromResult<OwmGeoResult[]?>(
        [
            new OwmGeoResult { Name = city, Country = "SG", Lat = 1.35, Lon = 103.82 }
        ]);

    public Task<OwmOneCallResponse?> GetOneCallAsync(double lat, double lon, CancellationToken ct = default)
        => Task.FromResult<OwmOneCallResponse?>(new OwmOneCallResponse
        {
            Lat = lat,
            Lon = lon,
            Current = new CurrentWeather
            {
                Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Temp = 30.5,
                FeelsLike = 32.0,
                Humidity = 75,
                Pressure = 1012,
                WindSpeed = 5.2,
                WindDeg = 180,
                Visibility = 10000,
                Uvi = 8.0,
                Sunrise = DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeSeconds(),
                Sunset = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
                Weather = [new WeatherCondition { Main = "Clear", Description = "clear sky", Icon = "01d" }]
            },
            Hourly = [],
            Daily = [],
            Alerts = []
        });

    public Task<OwmHistoricalResponse?> GetHistoricalWeatherAsync(double lat, double lon, long dt, CancellationToken ct = default)
        => Task.FromResult<OwmHistoricalResponse?>(new OwmHistoricalResponse
        {
            Lat = lat,
            Lon = lon,
            Data =
            [
                new CurrentWeather
                {
                    Dt = dt,
                    Temp = 28.0,
                    FeelsLike = 30.0,
                    Humidity = 70,
                    Pressure = 1013,
                    WindSpeed = 4.0,
                    WindDeg = 160,
                    Visibility = 10000,
                    Uvi = 6.0,
                    Sunrise = DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeSeconds(),
                    Sunset = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
                    Weather = [new WeatherCondition { Main = "Clouds", Description = "scattered clouds", Icon = "03d" }]
                }
            ]
        });
}

public class MockLocationService : ILocationService
{
    public List<LocationResponse> Locations { get; } = new();
    private int _nextId = 1;

    public void Reset()
    {
        Locations.Clear();
        _nextId = 1;
    }

    public Task<IEnumerable<LocationResponse>> GetAllOrByNameAsync(string? cityName, CancellationToken ct = default)
        => Task.FromResult(cityName == null
            ? Locations.AsEnumerable()
            : Locations.Where(l => l.City.Contains(cityName, StringComparison.OrdinalIgnoreCase)).AsEnumerable());

    public Task<LocationResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(Locations.FirstOrDefault(l => l.Id == id));

    public Task<LocationResponse> GetByIdOrThrowAsync(int id, CancellationToken ct = default)
    {
        var location = Locations.FirstOrDefault(l => l.Id == id);
        if (location == null) throw new KeyNotFoundException($"Location with ID {id} not found.");
        return Task.FromResult(location);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Locations.RemoveAll(l => l.Id == id);
        return Task.CompletedTask;
    }

    public Task<LocationResponse?> AddOrGetByCityNameAsync(string cityName, CancellationToken ct = default)
    {
        var existing = Locations.FirstOrDefault(l => l.City.Equals(cityName, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return Task.FromResult<LocationResponse?>(existing);

        var newLoc = new LocationResponse(
            Id: _nextId++,
            ZipCode: null,
            City: cityName,
            Country: "SG",
            Latitude: 1.35,
            Longitude: 103.82,
            CreatedAt: DateTimeOffset.UtcNow
        );
        Locations.Add(newLoc);
        return Task.FromResult<LocationResponse?>(newLoc);
    }
}

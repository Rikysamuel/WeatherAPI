using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherApi.Core.Data;
using WeatherApi.Core.Data.Entities;
using WeatherApi.Core.Models;
using WeatherApi.Core.Services;

namespace WeatherApi.Tests.Unit.Services;

public class AlertServiceTests
{
    private static WeatherDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WeatherDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyListInitially()
    {
        using var ctx = CreateDbContext();
        var svc = new AlertService(ctx, NullLogger<AlertService>.Instance);

        var now = DateTimeOffset.UtcNow;
        var result = await svc.GetAlertsAsync(now.AddDays(-1), now.AddDays(1));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAlertsWithinDateRange()
    {
        using var ctx = CreateDbContext();
        ctx.Locations.Add(new LocationEntity { City = "Tokyo", Country = "JP", Latitude = 35.68, Longitude = 139.69 });
        ctx.SaveChanges();

        var location = ctx.Locations.First();
        ctx.Alerts.Add(new AlertEntity
        {
            LocationId = location.Id,
            Message = "Alert 1",
            Severity = AlertSeverity.Low,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
            IsActive = true
        });
        ctx.Alerts.Add(new AlertEntity
        {
            LocationId = location.Id,
            Message = "Alert 2",
            Severity = AlertSeverity.High,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            IsActive = true
        });
        ctx.SaveChanges();

        var svc = new AlertService(ctx, NullLogger<AlertService>.Instance);

        var now = DateTimeOffset.UtcNow;
        var result = await svc.GetAlertsAsync(now.AddDays(-1), now);

        result.Should().HaveCount(2);
        result[0].Message.Should().Be("Alert 2");
        result[1].Message.Should().Be("Alert 1");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByLocationId()
    {
        using var ctx = CreateDbContext();
        ctx.Locations.Add(new LocationEntity { City = "Tokyo", Country = "JP", Latitude = 35.68, Longitude = 139.69 });
        ctx.Locations.Add(new LocationEntity { City = "London", Country = "GB", Latitude = 51.5, Longitude = -0.13 });
        ctx.SaveChanges();

        var locations = ctx.Locations.ToList();
        ctx.Alerts.Add(new AlertEntity
        {
            LocationId = locations[0].Id,
            Message = "Tokyo alert",
            Severity = AlertSeverity.High,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            IsActive = true
        });
        ctx.Alerts.Add(new AlertEntity
        {
            LocationId = locations[1].Id,
            Message = "London alert",
            Severity = AlertSeverity.Low,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            IsActive = true
        });
        ctx.SaveChanges();

        var svc = new AlertService(ctx, NullLogger<AlertService>.Instance);

        var now = DateTimeOffset.UtcNow;
        var result = await svc.GetAlertsAsync(now.AddDays(-1), now, locations[0].Id);

        result.Should().HaveCount(1);
        result[0].Message.Should().Be("Tokyo alert");
    }

    [Fact]
    public async Task GetAllAsync_ExcludesAlertsOutsideDateRange()
    {
        using var ctx = CreateDbContext();
        ctx.Locations.Add(new LocationEntity { City = "Tokyo", Country = "JP", Latitude = 35.68, Longitude = 139.69 });
        ctx.SaveChanges();

        var location = ctx.Locations.First();
        ctx.Alerts.Add(new AlertEntity
        {
            LocationId = location.Id,
            Message = "Old alert",
            Severity = AlertSeverity.High,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
            IsActive = true
        });
        ctx.SaveChanges();

        var svc = new AlertService(ctx, NullLogger<AlertService>.Instance);

        var now = DateTimeOffset.UtcNow;
        var result = await svc.GetAlertsAsync(now.AddDays(-1), now);

        result.Should().BeEmpty();
    }
}

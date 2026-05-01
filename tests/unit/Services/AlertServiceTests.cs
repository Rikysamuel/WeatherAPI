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

        var result = await svc.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        using var ctx = CreateDbContext();
        var svc = new AlertService(ctx, NullLogger<AlertService>.Instance);

        var result = await svc.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAlertWhenExists()
    {
        using var ctx = CreateDbContext();
        ctx.Locations.Add(new LocationEntity { City = "Singapore", Country = "SG", Latitude = 1.35, Longitude = 103.82 });
        ctx.SaveChanges();

        var location = ctx.Locations.First();
        var alert = new AlertEntity
        {
            LocationId = location.Id,
            Message = "Heavy rain warning",
            Severity = AlertSeverity.High,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
        ctx.Alerts.Add(alert);
        ctx.SaveChanges();

        var svc = new AlertService(ctx, NullLogger<AlertService>.Instance);

        var result = await svc.GetByIdAsync(alert.Id);

        result.Should().NotBeNull();
        result!.Message.Should().Be("Heavy rain warning");
        result.Severity.Should().Be(AlertSeverity.High);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveAlertsOrderedByDate()
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

        var result = await svc.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Message.Should().Be("Alert 2");
        result[1].Message.Should().Be("Alert 1");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenNotFound()
    {
        using var ctx = CreateDbContext();
        var svc = new AlertService(ctx, NullLogger<AlertService>.Instance);

        var result = await svc.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesAlertAndReturnsTrue()
    {
        using var ctx = CreateDbContext();
        ctx.Locations.Add(new LocationEntity { City = "London", Country = "GB", Latitude = 51.5, Longitude = -0.13 });
        ctx.SaveChanges();

        var location = ctx.Locations.First();
        var alert = new AlertEntity
        {
            LocationId = location.Id,
            Message = "Storm warning",
            Severity = AlertSeverity.Critical,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
        ctx.Alerts.Add(alert);
        ctx.SaveChanges();

        var svc = new AlertService(ctx, NullLogger<AlertService>.Instance);

        var result = await svc.DeleteAsync(alert.Id);

        result.Should().BeTrue();
        (await ctx.Alerts.AnyAsync()).Should().BeFalse();
    }
}

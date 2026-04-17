using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WeatherApi.Core.Data;
using WeatherApi.Core.Data.Entities;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.Core.Services;

public class AlertService : IAlertService
{
    private readonly WeatherDbContext _dbContext;
    private readonly ILogger<AlertService> _logger;

    public AlertService() { }
    public AlertService(WeatherDbContext dbContext, ILogger<AlertService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AlertResponse>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Getting all active alerts");
        var alerts = await _dbContext.Alerts
            .Include(a => a.Location)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return alerts.Select(MapToResponse).ToList();
    }

    public async Task<AlertResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting alert by ID: {Id}", id);
        var entity = await _dbContext.Alerts
            .Include(a => a.Location)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return entity == null ? null : MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        _logger.LogWarning("Deleting alert by ID: {Id}", id);
        var entity = await _dbContext.Alerts.FindAsync(new object[] { id }, ct);

        if (entity == null)
        {
            _logger.LogInformation("Alert ID {Id} not found for deletion", id);
            return false;
        }

        _dbContext.Alerts.Remove(entity);
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Successfully deleted alert ID: {Id}", id);
        return true;
    }

    public async Task<AlertSubscriptionResponse> SubscribeAsync(AlertSubscriptionDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating alert subscription for {Email} at LocationId {LocationId}", dto.Email, dto.LocationId);

        var location = await _dbContext.Locations.FirstOrDefaultAsync(x => x.Id == dto.LocationId, ct)
            ?? throw new KeyNotFoundException($"Location with ID {dto.LocationId} not found.");

        var existing = await _dbContext.AlertSubscriptions
            .FirstOrDefaultAsync(s => s.LocationId == dto.LocationId && s.Email == dto.Email, ct);

        if (existing != null)
        {
            _logger.LogInformation("Subscription already exists for {Email} at {City}", dto.Email, location.City);
            return MapToSubscriptionResponse(existing, location.City);
        }

        var entity = new AlertSubscriptionEntity
        {
            LocationId = dto.LocationId,
            Email = dto.Email,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _dbContext.AlertSubscriptions.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Successfully subscribed {Email} to alerts for {City} (ID: {Id})", dto.Email, location.City, entity.Id);
        return MapToSubscriptionResponse(entity, location.City);
    }

    public async Task UnsubscribeAsync(int subscriptionId, CancellationToken ct = default)
    {
        _logger.LogWarning("Unsubscribing ID: {Id}", subscriptionId);
        var entity = await _dbContext.AlertSubscriptions.FirstOrDefaultAsync(x => x.Id == subscriptionId, ct);

        if (entity != null)
        {
            _dbContext.AlertSubscriptions.Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully removed subscription ID: {Id}", subscriptionId);
        }
    }

    public async Task<IReadOnlyList<AlertSubscriptionResponse>> GetSubscriptionsAsync(string? email = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting subscriptions for {Email}", email ?? "ALL");

        var query = _dbContext.AlertSubscriptions.Include(s => s.Location).AsQueryable();
        if (!string.IsNullOrEmpty(email))
        {
            query = query.Where(s => s.Email.ToLower() == email.ToLower());
        }

        var subs = await query.ToListAsync(ct);
        return subs.Select(s => MapToSubscriptionResponse(s, s.Location.City)).ToList();
    }

    private AlertResponse MapToResponse(AlertEntity entity)
    {
        return new AlertResponse(
            Id: entity.Id,
            LocationId: entity.LocationId,
            City: entity.Location?.City ?? "N/A",
            Message: entity.Message,
            Severity: entity.Severity,
            CreatedAt: entity.CreatedAt,
            IsActive: entity.IsActive
        );
    }

    private AlertSubscriptionResponse MapToSubscriptionResponse(AlertSubscriptionEntity entity, string city)
    {
        return new AlertSubscriptionResponse(
            Id: entity.Id,
            LocationId: entity.LocationId,
            City: city,
            Email: entity.Email,
            CreatedAt: entity.CreatedAt
        );
    }
}

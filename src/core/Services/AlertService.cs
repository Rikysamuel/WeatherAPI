using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.Core.Services;

public class AlertService : IAlertService
{
    // TODO: Inject DbContext when Infrastructure is ready
    // private readonly WeatherDbContext _context;

    public Task<IReadOnlyList<AlertResponse>> GetAllAsync(CancellationToken ct = default)
    {
        // TODO: Query from database (alerts are DB-only per assignment spec)
        return Task.FromResult<IReadOnlyList<AlertResponse>>(Array.Empty<AlertResponse>());
    }

    public Task<AlertResponse> CreateAsync(AlertDto dto, CancellationToken ct = default)
    {
        // TODO: Save to database
        return Task.FromResult(new AlertResponse(
            Id: 1,
            City: dto.City,
            Message: dto.Message,
            Severity: dto.Severity,
            CreatedAt: DateTime.UtcNow,
            IsActive: true
        ));
    }

    public Task<AlertResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // TODO: Query from database
        return Task.FromResult<AlertResponse?>(null);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        // TODO: Delete from database
        return Task.FromResult(false);
    }
}

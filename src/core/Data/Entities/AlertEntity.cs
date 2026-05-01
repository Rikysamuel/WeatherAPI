using WeatherApi.Core.Models;

namespace WeatherApi.Core.Data.Entities;

public class AlertEntity
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public LocationEntity Location { get; set; } = null!;
    
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
}

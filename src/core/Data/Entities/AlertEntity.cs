using WeatherApi.Core.Models;
using WeatherApi.Core.Data.Entities;

namespace WeatherApi.Core.Entities;

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

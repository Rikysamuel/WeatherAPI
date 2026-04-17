namespace WeatherApi.Core.Data.Entities;

public class AlertSubscriptionEntity
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public LocationEntity Location { get; set; } = null!;
    
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

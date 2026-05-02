namespace WeatherApi.Core.Data.Entities;

public class HourlySummaryEntity
{
    public int Id { get; set; }

    public int LocationId { get; set; }
    public LocationEntity Location { get; set; } = null!;

    public DateTimeOffset Timestamp { get; set; }
    public double Temperature { get; set; }
    public string Description { get; set; } = string.Empty;
}

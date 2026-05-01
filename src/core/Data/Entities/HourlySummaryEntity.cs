namespace WeatherApi.Core.Data.Entities;

public class HourlySummaryEntity
{
    public int Id { get; set; }
    
    // Decoupled from WeatherDataId to allow independent persistence/upsert
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    
    public DateTimeOffset Timestamp { get; set; }
    public double Temperature { get; set; }
    public string Description { get; set; } = string.Empty;
}

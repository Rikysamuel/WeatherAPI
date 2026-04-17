namespace WeatherApi.Core.Data.Entities;

public class WeatherDataEntity
{
    public int Id { get; set; }
    
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string Description { get; set; } = string.Empty;
    public string NextEventNote { get; set; } = string.Empty;
    public double FeelsLike { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public double WindSpeed { get; set; }
    public double WindDirection { get; set; }
    public int Visibility { get; set; }
    public double UVI { get; set; }
    public DateTimeOffset Sunrise { get; set; }
    public DateTimeOffset Sunset { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
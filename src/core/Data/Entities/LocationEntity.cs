namespace WeatherApi.Core.Data.Entities;

public class LocationEntity
{
    public int Id { get; set; }
    public string? ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now.LocalDateTime;
}

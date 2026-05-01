namespace WeatherApi.Core.Data.Entities;

public class DailyWeatherEntity
{
    public int Id { get; set; }

    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    // Observed (actual weather)
    public double? ObservedTemperature { get; set; }
    public string? ObservedDescription { get; set; }
    public double? ObservedFeelsLike { get; set; }
    public double? ObservedHumidity { get; set; }
    public double? ObservedPressure { get; set; }
    public double? ObservedWindSpeed { get; set; }
    public double? ObservedWindDirection { get; set; }
    public int? ObservedVisibility { get; set; }
    public double? ObservedUVI { get; set; }
    public DateTimeOffset? ObservedSunrise { get; set; }
    public DateTimeOffset? ObservedSunset { get; set; }
    public string? ObservedNextEventNote { get; set; }
    public DateTimeOffset? ObservedTimestamp { get; set; }

    // Predicted (forecast)
    public double? PredictedTemperature { get; set; }
    public string? PredictedDescription { get; set; }
    public double? PredictedMinTemperature { get; set; }
    public double? PredictedMaxTemperature { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

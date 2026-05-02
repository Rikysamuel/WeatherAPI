using System.ComponentModel.DataAnnotations;

namespace WeatherApi.Core.Models;

public record WeatherData(
    string City,
    string Country,
    double Temperature,
    string Description,
    string NextEventNote,
    double FeelsLike,
    double Humidity,
    double Pressure,
    double WindSpeed,
    double WindDirection,
    int Visibility,
    double UVI,
    DateTimeOffset Sunrise,
    DateTimeOffset Sunset,
    DateTimeOffset Timestamp,
    IEnumerable<HourlySummary> HourlySummary,
    IEnumerable<DailySummary> DailySummary
);

public record HourlySummary(
    DateTimeOffset Timestamp,
    double Temperature,
    string Description
);

public record DailySummary(
    DateTimeOffset Timestamp,
    string Description,
    double MinTemperature,
    double MaxTemperature
);

public record ForecastDayResponse(
    string City,
    string Country,
    DateOnly Date,
    string Description,
    double? Temperature,
    double? MinTemperature,
    double? MaxTemperature
);

public record WeatherForecast(
    string City,
    IReadOnlyList<ForecastDayResponse> Forecasts
);

public record LocationDto(
    string? ZipCode,
    string? City,
    string? Country,
    double Latitude,
    double Longitude,
    int PageNumber = 1,
    int PageSize = 10
);

public record LocationResponse(
    int Id,
    string? ZipCode,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    DateTimeOffset CreatedAt
);

public record AlertDto(
    [Required] int LocationId,
    [Required][MaxLength(1000)] string Message,
    AlertSeverity Severity
);

public record AlertResponse(
    int Id,
    int LocationId,
    string City,
    string Message,
    AlertSeverity Severity,
    DateTimeOffset CreatedAt,
    bool IsActive
);

public record AlertSubscriptionDto(
    [Required] int LocationId,
    [Required][EmailAddress][MaxLength(255)] string Email
);

public record AlertSubscriptionResponse(
    int Id,
    int LocationId,
    string City,
    string Email,
    DateTimeOffset CreatedAt
);

public enum AlertSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public record ExportData(
    string City,
    IReadOnlyList<WeatherData> Data
);
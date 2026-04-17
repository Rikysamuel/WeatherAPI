using System.Text.Json.Serialization;
using WeatherApi.Core.Models;

namespace WeatherApi.Core.Interfaces;

public interface IWeatherService
{
    Task<WeatherData> GetPersistedCurrentWeatherAsync(int locationId, CancellationToken ct = default);
    Task RetrieveCurrentWeatherToOwmAsync(int locationId, CancellationToken ct = default);
    Task<WeatherForecast> GetForecastAsync(int locationId, int days = 5, CancellationToken ct = default);
    Task<WeatherData> GetHistoricalAsync(int locationId, DateTime date, CancellationToken ct = default);
    Task PruneOldDataAsync(CancellationToken ct = default);
    Task<int> DeleteByLocationIdAsync(int locationId, CancellationToken ct = default);
}

public interface ILocationService
{
    Task<IEnumerable<LocationResponse>> FindByNameAsync(string cityName, CancellationToken ct = default);
    Task<LocationResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task DeleteByCityNameAsync(string cityName, CancellationToken ct = default);
    Task<LocationResponse?> AddOrGetByCityNameAsync(string cityName, CancellationToken ct = default);
}

public interface IAlertService
{
    Task<IReadOnlyList<AlertResponse>> GetAllAsync(CancellationToken ct = default);
    Task<AlertResponse> CreateAsync(AlertDto dto, CancellationToken ct = default);
    Task<AlertResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IExportService
{
    Task<byte[]> ExportAsync(string city, string format = "csv", CancellationToken ct = default);
}

public interface IOwmClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);
    Task<OwmGeoResult[]?> GeocodeAsync(string city, CancellationToken ct = default);
    Task<OwmGeoResult[]?> GeocodeAsync(string zipCode, string countryCode, CancellationToken ct = default);
    Task<OwmOneCallResponse?> GetOneCallAsync(double lat, double lon, CancellationToken ct = default);
    Task<OwmHistoricalResponse?> GetHistoricalWeatherAsync(double lat, double lon, long dt, CancellationToken ct = default);
}

public class OwmHistoricalResponse
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("timezone_offset")]
    public int TimezoneOffset { get; set; }

    [JsonPropertyName("data")]
    public CurrentWeather[] Data { get; set; } = Array.Empty<CurrentWeather>();
}

// OWM Geocoding API response
public class OwmGeoResult
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}

// OWM One Call 3.0 response
public class OwmOneCallResponse
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("timezone_offset")]
    public int TimezoneOffset { get; set; }

    [JsonPropertyName("current")]
    public CurrentWeather Current { get; set; } = new();

    [JsonPropertyName("hourly")]
    public HourlyForecast[] Hourly { get; set; } = Array.Empty<HourlyForecast>();

    [JsonPropertyName("daily")]
    public DailyForecast[] Daily { get; set; } = Array.Empty<DailyForecast>();
}

public class CurrentWeather
{
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("sunrise")]
    public long Sunrise { get; set; }

    [JsonPropertyName("sunset")]
    public long Sunset { get; set; }

    [JsonPropertyName("temp")]
    public double Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }

    [JsonPropertyName("pressure")]
    public int Pressure { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("dew_point")]
    public double DewPoint { get; set; }

    [JsonPropertyName("uvi")]
    public double Uvi { get; set; }

    [JsonPropertyName("clouds")]
    public int Clouds { get; set; }

    [JsonPropertyName("visibility")]
    public int Visibility { get; set; }

    [JsonPropertyName("wind_speed")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("wind_deg")]
    public int WindDeg { get; set; }

    [JsonPropertyName("wind_gust")]
    public double WindGust { get; set; }

    [JsonPropertyName("weather")]
    public WeatherCondition[] Weather { get; set; } = Array.Empty<WeatherCondition>();
}

public class HourlyForecast
{
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("temp")]
    public double Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }

    [JsonPropertyName("pressure")]
    public int Pressure { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("dew_point")]
    public double DewPoint { get; set; }

    [JsonPropertyName("uvi")]
    public double Uvi { get; set; }

    [JsonPropertyName("clouds")]
    public int Clouds { get; set; }

    [JsonPropertyName("visibility")]
    public int Visibility { get; set; }

    [JsonPropertyName("wind_speed")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("wind_deg")]
    public int WindDeg { get; set; }

    [JsonPropertyName("wind_gust")]
    public double WindGust { get; set; }

    [JsonPropertyName("weather")]
    public WeatherCondition[] Weather { get; set; } = Array.Empty<WeatherCondition>();

    [JsonPropertyName("pop")]
    public double Pop { get; set; }

    [JsonPropertyName("rain")]
    public Rain? Rain { get; set; }
}

public class DailyForecast
{
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("sunrise")]
    public long Sunrise { get; set; }

    [JsonPropertyName("sunset")]
    public long Sunset { get; set; }

    [JsonPropertyName("temp")]
    public TempSummary Temp { get; set; } = new();

    [JsonPropertyName("feels_like")]
    public FeelsLikeDaily? FeelsLike { get; set; }

    [JsonPropertyName("pressure")]
    public int Pressure { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("dew_point")]
    public double DewPoint { get; set; }

    [JsonPropertyName("uvi")]
    public double Uvi { get; set; }

    [JsonPropertyName("clouds")]
    public int Clouds { get; set; }

    [JsonPropertyName("visibility")]
    public int Visibility { get; set; }

    [JsonPropertyName("wind_speed")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("wind_deg")]
    public int WindDeg { get; set; }

    [JsonPropertyName("wind_gust")]
    public double WindGust { get; set; }

    [JsonPropertyName("weather")]
    public WeatherCondition[] Weather { get; set; } = Array.Empty<WeatherCondition>();

    [JsonPropertyName("pop")]
    public double Pop { get; set; }

    [JsonPropertyName("rain")]
    public double? Rain { get; set; }
}

public class TempSummary
{
    [JsonPropertyName("day")]
    public double Day { get; set; }

    [JsonPropertyName("min")]
    public double Min { get; set; }

    [JsonPropertyName("max")]
    public double Max { get; set; }

    [JsonPropertyName("night")]
    public double Night { get; set; }

    [JsonPropertyName("eve")]
    public double Eve { get; set; }

    [JsonPropertyName("morn")]
    public double Morn { get; set; }

    [JsonPropertyName("feels_like_day")]
    public double FeelsLikeDay { get; set; }

    [JsonPropertyName("feels_like_night")]
    public double FeelsLikeNight { get; set; }

    [JsonPropertyName("feels_like_eve")]
    public double FeelsLikeEve { get; set; }

    [JsonPropertyName("feels_like_morn")]
    public double FeelsLikeMorn { get; set; }
}

public class FeelsLikeDaily
{
    [JsonPropertyName("day")]
    public double Day { get; set; }

    [JsonPropertyName("night")]
    public double Night { get; set; }

    [JsonPropertyName("eve")]
    public double Eve { get; set; }

    [JsonPropertyName("morn")]
    public double Morn { get; set; }
}

public class WeatherCondition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("main")]
    public string Main { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;
}

public class Rain
{
    [JsonPropertyName("1h")]
    public double OneHour { get; set; }
}

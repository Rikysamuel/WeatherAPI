using System.Text.Json.Serialization;

namespace WeatherApi.Core.Models;

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

    [JsonPropertyName("alerts")]
    public OwmAlert[] Alerts { get; set; } = Array.Empty<OwmAlert>();
}

public class OwmAlert
{
    [JsonPropertyName("sender_name")]
    public string SenderName { get; set; } = string.Empty;

    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("start")]
    public long Start { get; set; }

    [JsonPropertyName("end")]
    public long End { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();
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

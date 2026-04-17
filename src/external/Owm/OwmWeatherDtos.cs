using System.Text.Json.Serialization;

namespace WeatherApi.Infrastructure.Owm;

public record OwmCurrentWeatherResponse
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("cod")]
    public int Cod { get; init; }

    [JsonPropertyName("sys")]
    public OwmSys Sys { get; init; } = new();

    [JsonPropertyName("main")]
    public OwmMain Main { get; init; } = new();

    [JsonPropertyName("wind")]
    public OwmWind Wind { get; init; } = new();

    [JsonPropertyName("weather")]
    public List<OwmWeatherCondition> Weather { get; init; } = new();

    [JsonPropertyName("dt")]
    public long Dt { get; init; }
}

public record OwmSys
{
    [JsonPropertyName("country")]
    public string Country { get; init; } = string.Empty;
}

public record OwmMain
{
    [JsonPropertyName("temp")]
    public double Temp { get; init; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; init; }

    [JsonPropertyName("humidity")]
    public double Humidity { get; init; }

    [JsonPropertyName("pressure")]
    public double Pressure { get; init; }
}

public record OwmWind
{
    [JsonPropertyName("speed")]
    public double Speed { get; init; }
}

public record OwmWeatherCondition
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("main")]
    public string Main { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; init; } = string.Empty;
}

using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.External.Owm;

public class OwmClient(HttpClient httpClient, IOptions<OwmOptions> options) : IOwmClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly OwmOptions _options = options.Value;

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        var url = $"{endpoint}{(endpoint.Contains('?') ? "&" : "?")}appid={_options.ApiKey}&units=metric";
        return await _httpClient.GetFromJsonAsync<T>(url, ct);
    }

    public async Task<OwmGeoResult[]?> GeocodeAsync(string city, CancellationToken ct = default)
    {
        var url = $"/geo/1.0/direct?q={Uri.EscapeDataString(city)}&limit=1&appid={_options.ApiKey}";
        return await _httpClient.GetFromJsonAsync<OwmGeoResult[]>(url, ct);
    }

    public async Task<OwmGeoResult[]?> GeocodeAsync(string zipCode, string countryCode, CancellationToken ct = default)
    {
        var url = $"/geo/1.0/zip?zip={Uri.EscapeDataString(zipCode)},{Uri.EscapeDataString(countryCode)}&appid={_options.ApiKey}";
        return await _httpClient.GetFromJsonAsync<OwmGeoResult[]>(url, ct);
    }

    public async Task<OwmOneCallResponse?> GetOneCallAsync(double lat, double lon, CancellationToken ct = default)
    {
        var url = $"/data/3.0/onecall?lat={lat}&lon={lon}&appid={_options.ApiKey}&units=metric";
        return await _httpClient.GetFromJsonAsync<OwmOneCallResponse>(url, ct);
    }

    public async Task<OwmHistoricalResponse?> GetHistoricalWeatherAsync(double lat, double lon, long dt, CancellationToken ct = default)
    {
        var url = $"/data/3.0/onecall/timemachine?lat={lat}&lon={lon}&dt={dt}&appid={_options.ApiKey}&units=metric";
        return await _httpClient.GetFromJsonAsync<OwmHistoricalResponse>(url, ct);
    }
}

public class OwmOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openweathermap.org";
    public int TimeoutSeconds { get; set; } = 15;
}

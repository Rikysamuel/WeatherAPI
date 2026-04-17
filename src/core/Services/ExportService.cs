using System.Text;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.Core.Services;

public class ExportService : IExportService
{
    private readonly IWeatherService _weatherService;

    public ExportService(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public async Task<byte[]> ExportAsync(string city, string format = "csv", CancellationToken ct = default)
    {
        // For now, export current weather. Can be extended to forecast/historical.
        // var weather = await _weatherService.GetCurrentWeatherAsync(city, ct);

        // var sb = new StringBuilder();
        // sb.AppendLine("City,Country,Temperature(°C),Feels Like(°C),Humidity(%),Wind Speed(m/s),Description,Timestamp (UTC)");
        // sb.AppendLine($"{weather.City},{weather.Country},{weather.Temperature},{weather.FeelsLike},{weather.Humidity},{weather.WindSpeed},{weather.Description},{weather.Timestamp:O}");

        // return Encoding.UTF8.GetBytes(sb.ToString());
        return null;
    }
}

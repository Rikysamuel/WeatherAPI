using System.Text;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.Core.Services;

public class ExportService : IExportService
{
    private readonly IWeatherService _weatherService;
    private readonly ILocationService _locationService;

    public ExportService(IWeatherService weatherService, ILocationService locationService)
    {
        _weatherService = weatherService;
        _locationService = locationService;
    }

    public async Task<byte[]> ExportAsync(int locationId, int days = 5, string format = "csv", CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdAsync(locationId, ct)
            ?? throw new KeyNotFoundException($"Location ID '{locationId}' not found.");

        var sb = new StringBuilder();
        sb.AppendLine("City,Country,Temperature(°C),Feels Like(°C),Humidity(%),Pressure(hPa),Wind Speed(m/s),Description,Timestamp (UTC)");

        // Fetch historical data daily for the requested number of days
        for (int i = 0; i < days; i++)
        {
            var date = DateTime.UtcNow.AddDays(-i);
            var weather = await _weatherService.GetHistoricalAsync(locationId, date, ct);

            if (weather != null)
            {
                sb.AppendLine($"{weather.City},{weather.Country},{weather.Temperature},{weather.FeelsLike},{weather.Humidity},{weather.Pressure},{weather.WindSpeed},\"{weather.Description}\",{weather.Timestamp:O}");
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

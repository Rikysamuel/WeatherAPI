using System.Text;
using WeatherApi.Core.Common;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.Core.Services;

public class ExportService(IWeatherService weatherService) : IExportService
{
    private readonly IWeatherService _weatherService = weatherService;

    public async Task<byte[]> ExportAsync(int locationId, int days = 5, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("City,Country,Temperature(C),Feels Like(C),Humidity(%),Pressure(hPa),Wind Speed(m/s),Description,Timestamp (SGT)");

        // Fetch historical data daily for the requested number of days
        for (int i = 0; i < days; i++)
        {
            var date = DateTime.UtcNow.AddDays(-i);
            var weather = await _weatherService.GetHistoricalAsync(locationId, date, ct);

            if (weather != null)
            {
                sb.AppendLine($"{weather.City},{weather.Country},{weather.Temperature},{weather.FeelsLike},{weather.Humidity},{weather.Pressure},{weather.WindSpeed},\"{weather.Description}\",{DateTimeUtil.ConvertToSGTime(weather.Timestamp):O}");
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

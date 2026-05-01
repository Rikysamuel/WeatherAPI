using System.Text;
using Microsoft.EntityFrameworkCore;
using WeatherApi.Core.Common;
using WeatherApi.Core.Data;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.Core.Services;

public class ExportService(ILocationService locationService, WeatherDbContext dbContext) : IExportService
{
    public async Task<byte[]> ExportAsync(int locationId, int days = 5, CancellationToken ct = default)
    {
        var location = await locationService.GetByIdOrThrowAsync(locationId, ct);

        var sb = new StringBuilder();
        sb.AppendLine("City,Country,Date (SGT),Min Temp (C),Max Temp (C),Feels Like (C),Humidity (%),Pressure (hPa),Wind Speed (m/s),Description");

        var startDate = DateTime.UtcNow.Date;
        var rows = await dbContext.DailyWeather
            .Where(x => x.City.ToLower() == location.City.ToLower() && x.Date >= startDate)
            .OrderBy(x => x.Date)
            .Take(days)
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            var date = DateTimeUtil.ConvertToSGTime(new DateTimeOffset(row.Date, TimeSpan.Zero)).ToString("yyyy-MM-dd");
            var minTemp = row.PredictedMinTemperature?.ToString("F1") ?? "";
            var maxTemp = row.PredictedMaxTemperature?.ToString("F1") ?? "";
            var feelsLike = row.ObservedFeelsLike?.ToString("F1") ?? "";
            var humidity = row.ObservedHumidity?.ToString() ?? "";
            var pressure = row.ObservedPressure?.ToString() ?? "";
            var windSpeed = row.ObservedWindSpeed?.ToString("F1") ?? "";
            var description = row.ObservedDescription ?? row.PredictedDescription ?? "N/A";

            sb.AppendLine($"{location.City},{location.Country},{date},{minTemp},{maxTemp},{feelsLike},{humidity},{pressure},{windSpeed},\"{description}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

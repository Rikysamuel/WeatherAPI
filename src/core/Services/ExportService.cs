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
        sb.AppendLine("City,Country,Date (SGT),Min Temp (C),Max Temp (C),Description");

        var startDate = DateTime.UtcNow.Date;
        var rows = await dbContext.DailyWeather
            .Where(x => x.LocationId == locationId && x.Date >= startDate)
            .OrderBy(x => x.Date)
            .Take(days)
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            var date = DateTimeUtil.ConvertToSGTime(new DateTimeOffset(row.Date, TimeSpan.Zero)).ToString("yyyy-MM-dd");
            var minTemp = row.PredictedMinTemperature?.ToString("F1") ?? "";
            var maxTemp = row.PredictedMaxTemperature?.ToString("F1") ?? "";
            var description = row.ObservedDescription ?? row.PredictedDescription ?? "N/A";

            sb.AppendLine($"{location.City},{location.Country},{date},{minTemp},{maxTemp},\"{description}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

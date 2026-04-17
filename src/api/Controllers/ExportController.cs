using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.Controllers;

[Authorize]
public class ExportController(IExportService exportService) : BaseApiController
{
    private readonly IExportService _exportService = exportService;

    [HttpGet("{locationId:int}")]
    public async Task<IActionResult> Export(int locationId, [FromQuery] int days = 5, [FromQuery] string format = "csv", CancellationToken ct = default)
    {
        if (days > 5) days = 5; // Max 5 days historical data per requirement
        var data = await _exportService.ExportAsync(locationId, days, format, ct);
        return File(data, "text/csv", $"location_{locationId}_weather.csv");
    }
}
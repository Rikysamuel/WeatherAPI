using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.Controllers;

[Authorize]
[EnableRateLimiting("WeatherPolicy")]
public class ExportController(IExportService exportService) : BaseApiController
{
    private readonly IExportService _exportService = exportService;

    [HttpGet("{locationId:int}")]
    public async Task<IActionResult> Export(int locationId, [FromQuery] int days = 5, CancellationToken ct = default)
    {
        if (days > 5) days = 5;
        var data = await _exportService.ExportAsync(locationId, days, ct);
        return File(data, "text/csv", $"location_{locationId}_weather.csv");
    }
}
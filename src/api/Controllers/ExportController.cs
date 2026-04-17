using Microsoft.AspNetCore.Mvc;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.Controllers;

public class ExportController(IExportService exportService) : BaseApiController
{
    private readonly IExportService _exportService = exportService;

    [HttpGet("export/{city}")]
    public async Task<IActionResult> Export(string city, [FromQuery] string format, CancellationToken ct)
    {
        var data = await _exportService.ExportAsync(city, format, ct);
        return File(data, "text/csv", $"{city}_weather.csv");
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherApi.Core.Common;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.Controllers;

[EnableRateLimiting(Constants.Policy.WeatherPolicy)]
public class WeatherController(IWeatherService weatherService) : BaseApiController
{
    private readonly IWeatherService _weatherService = weatherService;

    [HttpGet("current/{locationId:int}")]
    public async Task<IActionResult> GetCurrent(int locationId, CancellationToken ct)
    {
        var result = await _weatherService.GetPersistedCurrentWeatherAsync(locationId, ct);
        return Ok(result);
    }

    [HttpGet("forecast/{locationId:int}")]
    public async Task<IActionResult> GetForecast(int locationId, CancellationToken ct, [FromQuery] int days = 5)
    {
        if (days < 1 || days > 7) days = 5;
        var result = await _weatherService.GetForecastAsync(locationId, days, ct);
        return Ok(result);
    }

    [HttpGet("historical/{locationId:int}")]
    public async Task<IActionResult> GetHistorical(int locationId, [FromQuery] DateTime date, CancellationToken ct)
    {
        var result = await _weatherService.GetHistoricalAsync(locationId, date, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{locationId:int}")]
    public async Task<IActionResult> Delete(int locationId, CancellationToken ct)
    {
        var count = await _weatherService.DeleteByLocationIdAsync(locationId, ct);
        return Ok(new { message = $"Deleted {count} records for LocationId {locationId}" });
    }
}

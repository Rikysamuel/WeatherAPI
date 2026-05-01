using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.Controllers;

[Authorize]
[EnableRateLimiting("WeatherPolicy")]
public class LocationController(ILocationService locationService) : BaseApiController
{
    private readonly ILocationService _locationService = locationService;

    [HttpGet("GetLocationByName")]
    public async Task<IActionResult> GetLocationByName(string cityName, CancellationToken ct)
    {
        return Ok(await _locationService.GetAllOrByNameAsync(cityName, ct));
    }

    [HttpPost("FindLocationByName")]
    public async Task<IActionResult> FindLocationByName(string cityName, CancellationToken ct)
    {
        var location = await _locationService.AddOrGetByCityNameAsync(cityName, ct);
        if (location != null)
            return CreatedAtAction(nameof(FindLocationByName), new { id = location.Id }, location);

        throw new KeyNotFoundException($"City '{cityName}' not found.");
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var location = await _locationService.GetByIdAsync(id, ct);
        return location != null ? Ok(location) : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _locationService.DeleteAsync(id, ct);
        return NoContent();
    }
}

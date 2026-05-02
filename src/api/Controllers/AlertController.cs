using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.Controllers;

[Authorize]
[EnableRateLimiting("WeatherPolicy")]
public class AlertController(IAlertService alertService) : BaseApiController
{
    private readonly IAlertService _alertService = alertService;

    [HttpGet]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int? locationId,
        CancellationToken ct)
    {
        if (from == DateTimeOffset.MinValue || to == DateTimeOffset.MinValue)
            return BadRequest("from and to are required query parameters.");

        if (from > to)
            return BadRequest("from must be before or equal to to.");

        if ((to - from).TotalDays > 3)
            return BadRequest("Date range must not exceed 3 days.");

        var alerts = await _alertService.GetAlertsAsync(from, to, locationId, ct);
        return Ok(alerts);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAlert(int id, CancellationToken ct)
    {
        var success = await _alertService.DeleteAsync(id, ct);
        return success ? NoContent() : NotFound();
    }

    // ── Subscriptions ──

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] AlertSubscriptionDto dto, CancellationToken ct)
    {
        var sub = await _alertService.SubscribeAsync(dto, ct);
        return Ok(sub);
    }

    [HttpDelete("unsubscribe/{id:int}")]
    public async Task<IActionResult> Unsubscribe(int id, CancellationToken ct)
    {
        await _alertService.UnsubscribeAsync(id, ct);
        return NoContent();
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions([FromQuery] string? email, CancellationToken ct)
    {
        var subs = await _alertService.GetSubscriptionsAsync(email, ct);
        return Ok(subs);
    }
}

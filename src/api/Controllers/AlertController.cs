using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.Controllers;

[Authorize]
public class AlertController(IAlertService alertService) : BaseApiController
{
    private readonly IAlertService _alertService = alertService;

    [HttpGet]
    public async Task<IActionResult> GetAlerts(CancellationToken ct)
    {
        var alerts = await _alertService.GetAllAsync(ct);
        return Ok(alerts);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var alert = await _alertService.GetByIdAsync(id, ct);
        return alert != null ? Ok(alert) : NotFound();
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

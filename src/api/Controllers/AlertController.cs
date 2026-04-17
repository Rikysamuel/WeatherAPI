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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var alert = await _alertService.GetByIdAsync(id, ct);
        return alert != null ? Ok(alert) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAlerts([FromBody] AlertDto dto, CancellationToken ct)
    {
        var alert = await _alertService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = alert.Id }, alert);
    }
}
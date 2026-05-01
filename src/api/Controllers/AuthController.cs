using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherApi.Core.Common;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.Controllers;

[EnableRateLimiting(Constants.Policy.AuthPolicy)]
public class AuthController(IAuthenticationService authService) : BaseApiController
{
    private readonly IAuthenticationService _authService = authService;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request.Username, request.Password, ct);
        if (!result.Success)
            return result.ErrorMessage switch
            {
                "Username already exists." => Conflict(result.ErrorMessage),
                _ => BadRequest(result.ErrorMessage)
            };

        return Ok(new { message = "User registered successfully." });
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> GetToken([FromBody] TokenRequest request, CancellationToken ct)
    {
        var result = await _authService.GenerateTokenAsync(request.Username, request.Password, ct);
        if (result.Error != null)
        {
            if (result.Error == "Invalid username or password.")
                return Unauthorized(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(new { token = result.Token, expires_in = result.ExpiresIn });
    }
}

public record TokenRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password);

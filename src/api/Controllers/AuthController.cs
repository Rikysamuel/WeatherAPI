using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace WeatherApi.Controllers;

public class AuthController(IConfiguration configuration) : BaseApiController
{
    private readonly IConfiguration _configuration = configuration;

    [HttpPost("token")]
    [AllowAnonymous]
    public IActionResult GetToken([FromBody] TokenRequest request)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "dev-secret-key-not-for-production-use");

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username ?? "test-user"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { token = tokenString, expires_in = 3600 });
    }
}

public record TokenRequest(string? Username = null);

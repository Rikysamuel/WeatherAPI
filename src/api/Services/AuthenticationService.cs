using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WeatherApi.Core.Data;
using WeatherApi.Core.Data.Entities;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.Services;

public class AuthenticationService(WeatherDbContext dbContext, IConfiguration configuration) : IAuthenticationService
{
    private readonly WeatherDbContext _dbContext = dbContext;
    private readonly IConfiguration _configuration = configuration;

    public async Task<AuthResult> RegisterAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new AuthResult(false, "Username is required.");

        if (username.Length < 3)
            return new AuthResult(false, "Username must be at least 3 characters.");

        if (!IsPasswordStrongEnough(password))
            return new AuthResult(false, "Password must be at least 8 characters with at least one uppercase letter and one digit.");

        var exists = await _dbContext.Users.AnyAsync(u => u.Username == username, ct);
        if (exists)
            return new AuthResult(false, "Username already exists.");

        var user = new UserEntity
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        return new AuthResult(true, null);
    }

    public async Task<TokenResult> GenerateTokenAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return new TokenResult(null, 0, "Username and password are required.");

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return new TokenResult(null, 0, "Invalid username or password.");

        var jwtSettings = _configuration.GetSection("Jwt");
        var jwtKeyValue = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        var key = Encoding.UTF8.GetBytes(jwtKeyValue);
        var expiryMinutes = int.TryParse(jwtSettings["ExpiryMinutes"], out var exp) ? exp : 60;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResult(tokenString, expiryMinutes * 60, null);
    }

    private static bool IsPasswordStrongEnough(string password)
        => password.Length >= 8 
            && password.Any(char.IsUpper) 
            && password.Any(char.IsDigit);
}

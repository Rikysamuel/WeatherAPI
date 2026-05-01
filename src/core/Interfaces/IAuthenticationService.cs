namespace WeatherApi.Core.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResult> RegisterAsync(string username, string password, CancellationToken ct = default);
    Task<TokenResult> GenerateTokenAsync(string username, string password, CancellationToken ct = default);
}

public record AuthResult(bool Success, string? ErrorMessage);
public record TokenResult(string? Token, int ExpiresIn, string? Error);

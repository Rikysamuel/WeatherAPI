namespace WeatherApi.Core.Data.Entities;

public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; 
}
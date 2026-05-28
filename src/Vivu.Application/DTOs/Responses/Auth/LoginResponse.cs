namespace Vivu.Application.DTOs.Responses.Auth;

public class LoginResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
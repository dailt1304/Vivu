using Vivu.Domain.Entities;

namespace Vivu.Application.Interfaces.Auth;

public interface IAuthTokenProcess
{
    string GenerateToken(User user, string[] roles);
    string GenerateRefreshToken();
    //void WriteAuthTokenAsHttpOnlyCookie(string cookieName, string token, DateTime expiry);
    //void DeleteAuthTokenCookie(string key);
    //Task<string> GenerateEmailConfirmationTokenAsync(User user);
    //Task<string> GeneratePasswordTokenResetAsync(User user);
    //bool ValidateEmailConfirmationToken(User user, string token);
}
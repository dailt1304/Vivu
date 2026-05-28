using System.Security.Claims;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Domain.Entities;

namespace Vivu.Application.Interfaces.Auth;

public interface IAuthorizeServices
{
    Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken);
    //Task Logout(User user);
}
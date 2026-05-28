using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Services.Auth
{
    public class AuthorizeServices : IAuthorizeServices
    {
        private readonly string _clientId;
        private readonly ILogger<AuthorizeServices> _logger;

        public AuthorizeServices(
            IConfiguration configuration,
            ILogger<AuthorizeServices> logger)
        {
            _clientId = configuration["GoogleAuth:ClientId"]!;
            _logger = logger;
        }

        public async Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken)
        {
            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    validationSettings);

                if (payload == null)
                {
                    _logger.LogWarning("Google ID token validation returned null");
                    return null;
                }

                _logger.LogInformation(
                    "Successfully validated Google ID token for email: {Email}",
                    payload.Email);

                return new GoogleUserInfo
                {
                    GoogleId = payload.Subject, 
                    Email = payload.Email,
                    EmailVerified = payload.EmailVerified,
                    Name = payload.Name,
                    GivenName = payload.GivenName,
                    FamilyName = payload.FamilyName,
                    Picture = payload.Picture
                };
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogError(ex, "Invalid Google ID token");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google ID token");
                return null;
            }
        }
    }
}

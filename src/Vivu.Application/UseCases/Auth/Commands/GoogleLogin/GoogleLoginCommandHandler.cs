using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.GoogleLogin;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.GoogleLogin
{
    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<LoginResponse>>
    {
        private readonly IAuthorizeServices _googleAuthService;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IAuthTokenProcess _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<GoogleLoginCommandHandler> _logger;

        public GoogleLoginCommandHandler(
            IAuthorizeServices googleAuthService,
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IAuthTokenProcess jwtService,
            ILogger<GoogleLoginCommandHandler> logger,
            IUnitOfWork unitOfWork,
            IRoleRepository roleRepository)
        {
            _googleAuthService = googleAuthService;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtService = jwtService;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _roleRepository = roleRepository;
        }

        public async Task<Result<LoginResponse>> Handle(
            GoogleLoginCommand request,
            CancellationToken cancellationToken)
        {
            var googleUser = await _googleAuthService.ValidateIdTokenAsync(request.IdToken);

            if (googleUser == null)
            {
                return Result<LoginResponse>.Failure(DomainErrors.Auth.InvalidGoogleToken);
            }

            if (!googleUser.EmailVerified)
            {
                return Result<LoginResponse>.Failure(DomainErrors.Auth.EmailNotVerified);
            }

            var user = await _userRepository.GetByGoogleIdAsync(googleUser.GoogleId);

            if (user == null)
            {
                user = await _userRepository.FindByEmailAsync(googleUser.Email);

                if (user != null)
                {
                    user.GoogleId = googleUser.GoogleId;
                    user.IsGoogleUser = true;
                    _userRepository.Update(user);

                    _logger.LogInformation(
                        "Linked Google account to existing user {UserId}",
                        user.Id);
                }
                else
                {
                    user = User.CreateGoogle
                    (
                        email: googleUser.Email,
                        googleId: googleUser.GoogleId,
                        fullName: googleUser.Name,
                        avatarUrl: googleUser.Picture
                    );

                    var createdUser = await _userRepository.AddAsync(user);
                    var userRole = await _roleRepository.GetByNameAsync(Role.Names.User);
                    if (userRole == null)
                    {
                        _logger.LogError(
                            "Registration failed: User role not found in system. RoleName: {RoleName}",
                            Role.Names.User);
                        return Result<LoginResponse>.Failure(DomainErrors.Role.NotFoundByRole(Role.Names.User));
                    }
                    user.AssignRole(userRole.Id);
                    _logger.LogDebug("Role assigned to user. UserId: {UserId}, RoleId: {RoleId}", createdUser.Id, userRole.Id);

                    _logger.LogInformation(
                        "Created new user from Google login: {Email}",
                        user.Email);
                }

                await _unitOfWork.SaveChangesAsync();
            }

            if (user.IsBanned())
            {
                _logger.LogWarning(
                    "Google login blocked: User is banned. UserId: {UserId}, Email: {Email}",
                    user.Id, user.Email);
                return Result<LoginResponse>.Failure(DomainErrors.User.Banned);
            }

            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToArray();
            _logger.LogDebug(
                        "User roles loaded. UserId: {UserId}, Roles: {Roles}",
                        user.Id,
                        string.Join(", ", roles));
            var accessToken = _jwtService.GenerateToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();
            _logger.LogDebug(
                        "Tokens generated. UserId: {UserId}, TokenLength: {TokenLength}",
                        user.Id,
                        accessToken.Length);

            var refreshTokenEntity = Domain.Entities.RefreshToken.Create(
                userId: user.Id,
                token: refreshToken,
                expiresAt: DateTime.UtcNow.AddDays(7),
                ipAddress: request.IpAddress,
                deviceType: request.DeviceType,
                deviceName: request.DeviceName
            );

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            user.UpdateLastLogin();
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} logged in via Google from {IpAddress}",
                user.Id, request.IpAddress);
            var loginResponse = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = refreshTokenEntity.ExpiresAt,
                Id = user.Id,
                AvatarUrl = user.UserProfile.AvatarUrl,
                Email = user.Email,
                FullName = user.UserProfile.FullName,
                Roles = roles
            };
            _logger.LogInformation(
                        "Login successful. UserId: {UserId}, Email: {Email}, Roles: {Roles}, Device: {DeviceType}, IP: {IpAddress}",
                        user.Id,
                        googleUser.Email,
                        string.Join(", ", roles),
                        request.DeviceType ?? "Unknown",
                        request.IpAddress ?? "Unknown");

            return Result<LoginResponse>.Success(loginResponse);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.LoginUser
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand,Result<LoginResponse>>
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IAuthTokenProcess _tokenProcess;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoginUserCommandHandler> _logger;
        public LoginUserCommandHandler(IPasswordHasher passwordHasher, IUserRepository userRepository, 
                                        IRefreshTokenRepository refreshTokenRepository, IAuthTokenProcess tokenProcess, IUnitOfWork unitOfWork,
                                        ILogger<LoginUserCommandHandler> logger)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenProcess = tokenProcess;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<LoginResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                        "Login attempt for email: {Email}, Device: {DeviceType}/{DeviceName}, IP: {IpAddress}",
                        request.Email,
                        request.DeviceType ?? "Unknown",
                        request.DeviceName ?? "Unknown",
                        request.IpAddress ?? "Unknown");
            var user = await _userRepository.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning(
                       "Login failed: User not found. Email: {Email}, IP: {IpAddress}",
                       request.Email,
                       request.IpAddress);
                return Result<LoginResponse>.Failure(DomainErrors.User.NotFound);
            }
            _logger.LogDebug(
                        "User found for login. UserId: {UserId}, Email: {Email}",
                        user.Id,
                        request.Email);

            var matchPassword = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!matchPassword)
            {
                _logger.LogWarning(
                        "Login failed: Wrong password. UserId: {UserId}, Email: {Email}, IP: {IpAddress}",
                        user.Id,
                        request.Email,
                        request.IpAddress);
                return Result<LoginResponse>.Failure(DomainErrors.User.WrongPassword);
            }
            _logger.LogDebug("Password verified successfully for UserId: {UserId}", user.Id);


            if (user.IsBanned())
            {
                _logger.LogWarning(
                        "Login failed: User is banned. UserId: {UserId}, Email: {Email}, IP: {IpAddress}",
                        user.Id,
                        request.Email,
                        request.IpAddress);
                return Result<LoginResponse>.Failure(DomainErrors.User.Banned);
            }

            user.UpdateLastLogin();
            _logger.LogDebug("Updated last login time for UserId: {UserId}", user.Id);

            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToArray();
            _logger.LogDebug(
                        "User roles loaded. UserId: {UserId}, Roles: {Roles}",
                        user.Id,
                        string.Join(", ", roles));

            var token = _tokenProcess.GenerateToken(user,roles);
            var refreshToken = _tokenProcess.GenerateRefreshToken();
            _logger.LogDebug(
                        "Tokens generated. UserId: {UserId}, TokenLength: {TokenLength}",
                        user.Id,
                        token.Length);

            var refreshTokenEntity = Domain.Entities.RefreshToken.Create(
                userId: user.Id,
                token: refreshToken,
                expiresAt: DateTime.UtcNow.AddDays(7),
                ipAddress: request.IpAddress,
                deviceType: request.DeviceType,
                deviceName: request.DeviceName
            );
            _userRepository.Update(user);
            await _refreshTokenRepository.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogDebug(
                        "Refresh token saved. UserId: {UserId}, ExpiresAt: {ExpiresAt}",
                        user.Id,
                        refreshTokenEntity.ExpiresAt);

            var loginResponse = new LoginResponse
            {
                AccessToken = token,
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
                        request.Email,
                        string.Join(", ", roles),
                        request.DeviceType ?? "Unknown",
                        request.IpAddress ?? "Unknown");

            return Result<LoginResponse>.Success(loginResponse);
        }
    }
}

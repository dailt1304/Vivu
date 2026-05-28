using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse> >
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<LoginUserCommandHandler> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IAuthTokenProcess _tokenProcess;
        private readonly IUnitOfWork _unitOfWork;
        public RefreshTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository, ILogger<LoginUserCommandHandler> logger,
                                            IUserRepository userRepository, IAuthTokenProcess authTokenProcess, IUnitOfWork unitOfWork )
        {
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
            _userRepository = userRepository;
            _tokenProcess = authTokenProcess;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("");
            var reToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
            if (reToken == null)
            {
                _logger.LogWarning("Refresh token was not found");
                return Result<RefreshTokenResponse>.Failure(DomainErrors.Auth.InvalidRefreshToken);
            }

            if (reToken.IsExpired())
            {
                _logger.LogWarning("Refresh token has been expired in {Date}", reToken.ExpiresAt);
                return Result<RefreshTokenResponse>.Failure(DomainErrors.Auth.RefreshTokenExpired);
            }

            if (reToken.IsUsed)
            {
                _logger.LogWarning("Refresh token has been used");
                await _refreshTokenRepository.RevokeAllUserTokensAsync(reToken.UserId, "Refresh is already used");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<RefreshTokenResponse>.Failure(DomainErrors.Auth.RefreshTokenRevoked);
            }

            if (reToken.IsRevoked)
            {
                _logger.LogWarning("Refresh token has been revoked with reason: {reason}", reToken.RevokedReason);
                return Result<RefreshTokenResponse>.Failure(DomainErrors.Auth.RefreshTokenRevoked);
            }

            var user = await _userRepository.GetByIdAsync(reToken.UserId);
            if (user == null)
            {
                _logger.LogWarning("User was not found");
                return Result<RefreshTokenResponse>.Failure(DomainErrors.User.NotFound);
            }

            if (user.IsBanned())
            {
                _logger.LogWarning(
                    "Token refresh blocked: User is banned. UserId: {UserId}", user.Id);
                await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, "User is banned");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<RefreshTokenResponse>.Failure(DomainErrors.User.Banned);
            }

            reToken.MarkAsUsed();
            _refreshTokenRepository.Update(reToken);
            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToArray();
            _logger.LogDebug(
                        "User roles loaded. UserId: {UserId}, Roles: {Roles}",
                        user.Id,
                        string.Join(", ", roles));

            var token = _tokenProcess.GenerateToken(user, roles);
            var refreshtoken = _tokenProcess.GenerateRefreshToken();
            var refreshTokenEntity = Domain.Entities.RefreshToken.Create(
               userId: user.Id,
               token: refreshtoken,
               expiresAt: DateTime.UtcNow.AddDays(7),
               ipAddress: request.IpAddress,
               deviceType: request.DeviceType,
               deviceName: request.DeviceName
           );
            await _refreshTokenRepository.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                   "User {UserId} refreshed token. New token: {TokenId}",
                   user.Id, refreshTokenEntity.Id);
            var response = new RefreshTokenResponse
            {
                RefreshToken = refreshtoken,
                AccessToken = token,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
            };
            return Result<RefreshTokenResponse>.Success(response);


        }
    }
}

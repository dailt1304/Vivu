using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Users.Commands.RegisterUser;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;

        public ResetPasswordCommandHandler(
            IUserRepository userRepository,
            IOtpRepository otpRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<ResetPasswordCommandHandler> logger)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            ResetPasswordCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Password reset attempt for email: {Email}",
                request.Email);
            var otp = await _otpRepository.GetVerifiedOtpAsync(request.Email, OtpType.PasswordReset);

            if (otp is null)
            {
                _logger.LogWarning(
                    "Password reset failed: No verified OTP found. Email: {Email}",
                    request.Email);
                return Result.Failure(DomainErrors.Auth.OtpInvalid);
            }
            _logger.LogDebug("Verified OTP found. Email: {Email}, OtpId: {OtpId}", request.Email, otp.Id);


            if (otp.IsExpired())
            {
                _logger.LogWarning(
                   "Password reset failed: OTP expired. Email: {Email}, OtpId: {OtpId}, ExpiredAt: {ExpiredAt}",
                   request.Email,
                   otp.Id,
                   otp.ExpiredAt);
                return Result.Failure(DomainErrors.Auth.OtpExpired);
            }

            if (otp.IsUsed)
            {
                _logger.LogWarning(
                    "Password reset failed: OTP already used. Email: {Email}, OtpId: {OtpId}, UsedAt: {UsedAt}",
                    request.Email,
                    otp.Id,
                    otp.UsedAt);
                return Result.Failure(DomainErrors.Auth.OtpAlreadyUsed);
            }
            _logger.LogDebug("OTP validation passed. Email: {Email}", request.Email);


            var user = await _userRepository.FindByEmailAsync(request.Email);
            if (user is null)
            {
                _logger.LogError(
                   "Password reset failed: User not found (but had verified OTP). Email: {Email}, OtpId: {OtpId}",
                   request.Email,
                   otp.Id);
                return Result.Failure(DomainErrors.User.NotFoundByEmail(request.Email));
            }
            _logger.LogDebug("User found for password reset. UserId: {UserId}", user.Id);


            var hash = _passwordHasher.HashPassword(request.NewPassword);
            _logger.LogDebug("New password hashed successfully. UserId: {UserId}", user.Id);

            user.UpdatePassword(hash);
            _logger.LogDebug("Password updated in entity. UserId: {UserId}", user.Id);

            otp.MarkAsUsed();
            _logger.LogDebug("OTP marked as used. OtpId: {OtpId}", otp.Id);


            _userRepository.Update(user);
            _otpRepository.Update(otp);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Password reset successful. UserId: {UserId}, Email: {Email}",
                user.Id,
                request.Email);

            return Result.Success();
        }
    }
}

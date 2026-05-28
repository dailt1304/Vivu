using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Email;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        public ForgotPasswordCommandHandler(
            IUserRepository userRepository,
            IOtpRepository otpRepository,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<ForgotPasswordCommandHandler> logger)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            ForgotPasswordCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                    "Password reset requested for email: {Email}",
                    request.Email);
            var user = await _userRepository.FindByEmailAsync(request.Email);
            if (user is null)
            {
                _logger.LogWarning(
                        "Password reset failed: User not found. Email: {Email}",
                        request.Email);
                return Result.Failure(DomainErrors.User.NotFoundByEmail(request.Email));
            }
            _logger.LogDebug(
                    "User found for password reset. UserId: {UserId}, Email: {Email}",
                    user.Id,
                    request.Email);

            var recentRequests = await _otpRepository.CountRecentOtpRequestsAsync(
                request.Email,
                OtpType.PasswordReset,
                15);
            _logger.LogDebug(
                    "Recent OTP requests count: {Count} (limit: 3) for UserId: {UserId}",
                    recentRequests,
                    user.Id);

            if (recentRequests >= 3)
            {
                _logger.LogWarning(
                        "Password reset blocked: Rate limit exceeded. UserId: {UserId}, Email: {Email}, RecentRequests: {Count}",
                        user.Id,
                        request.Email,
                        recentRequests);
                return Result.Failure(DomainErrors.Auth.TooManyOtpRequests);
            }

            var otp = Otp.Create(request.Email, OtpType.PasswordReset, user.Id);
            _logger.LogDebug(
                    "OTP created. UserId: {UserId}, OtpId: {OtpId}, ExpiresAt: {ExpiresAt}",
                    user.Id,
                    otp.Id,
                    otp.ExpiredAt);
            await _otpRepository.AddAsync(otp);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogDebug(
                    "OTP saved to database. UserId: {UserId}, OtpId: {OtpId}",
                    user.Id,
                    otp.Id);

            await _emailService.SendPasswordResetEmailAsync(user.Email, otp.Code,user.UserProfile.FullName);
            _logger.LogInformation(
                    "Password reset email sent successfully. UserId: {UserId}, Email: {Email}, FullName: {FullName}",
                    user.Id,
                    user.Email,
                    user.UserProfile.FullName);

            return Result.Success();
        }
    }
}

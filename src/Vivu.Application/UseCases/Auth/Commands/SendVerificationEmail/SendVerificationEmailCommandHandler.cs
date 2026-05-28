using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Email;
using Vivu.Application.UseCases.Auth.Commands.ResetPassword;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.SendVerificationEmail
{
    public class SendVerificationEmailCommandHandler : IRequestHandler<SendVerificationEmailCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SendVerificationEmailCommandHandler> _logger;

        public SendVerificationEmailCommandHandler(
            IUserRepository userRepository,
            IOtpRepository otpRepository,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<SendVerificationEmailCommandHandler> logger)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            SendVerificationEmailCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Email verification requested for: {Email}",
                request.Email);
            var user = await _userRepository.FindByEmailAsync(request.Email);
            if (user != null)
            {
                _logger.LogWarning(
                   "Verification email failed: Email already registered. Email: {Email}",
                   request.Email);
                return Result.Failure(DomainErrors.User.EmailAlreadyExists);
            }
            _logger.LogDebug("Email available for registration. Email: {Email}", request.Email);


            var recentRequests = await _otpRepository.CountRecentOtpRequestsAsync(
                request.Email,
                OtpType.EmailVerification,
                15);
            _logger.LogDebug(
                "Recent verification requests: {Count} (limit: 3) for Email: {Email}",
                recentRequests,
                request.Email);

            if (recentRequests >= 3)
            {
                _logger.LogWarning(
                    "Verification email blocked: Rate limit exceeded. Email: {Email}, RecentRequests: {Count}",
                    request.Email,
                    recentRequests);
                return Result.Failure(DomainErrors.Auth.TooManyOtpRequests);
            }

            var otp = Otp.Create(request.Email, OtpType.EmailVerification, null);
            _logger.LogDebug(
                "OTP created for email verification. Email: {Email}, OtpId: {OtpId}, ExpiresAt: {ExpiresAt}",
                request.Email,
                otp.Id,
                otp.ExpiredAt);

            await _otpRepository.AddAsync(otp);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("OTP saved to database. OtpId: {OtpId}", otp.Id);

            await _emailService.SendVerificationEmailAsync(otp.Email, otp.Code);
            _logger.LogInformation(
                    "Verification email sent successfully. Email: {Email}",
                    request.Email);

            return Result.Success();
        }
    }
}


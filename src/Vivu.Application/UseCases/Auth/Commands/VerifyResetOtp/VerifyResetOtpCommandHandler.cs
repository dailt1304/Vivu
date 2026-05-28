using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.VerifyResetOtp
{
    public class VerifyResetOtpCommandHandler : IRequestHandler<VerifyResetOtpCommand, Result>
    {
        private readonly IOtpRepository _otpRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VerifyResetOtpCommandHandler> _logger;

        public VerifyResetOtpCommandHandler(
            IOtpRepository otpRepository,
            IUnitOfWork unitOfWork,
            ILogger<VerifyResetOtpCommandHandler> logger)
        {
            _otpRepository = otpRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            VerifyResetOtpCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Verifying password reset OTP for email: {Email}", request.Email);
            var otp = await _otpRepository.GetValidOtpAsync(
                request.Email,
                request.Code,
                OtpType.PasswordReset);

            if (otp is null)
            {
                _logger.LogWarning(
                        "Verify Reset OTP failed: Invalid OTP or Email mismatch for {Email}. Code: {Code}",
                        request.Email, request.Code);
                return Result.Failure(DomainErrors.Auth.OtpInvalid);
            }
            if (otp.IsVerified)
            {
                _logger.LogWarning("Verify Reset Otp failed: otp {otpcode} was verified", otp.Code);
                return Result.Failure(DomainErrors.Auth.OtpAlreadyUsed);
            }

            var verifyResult = otp.VerifyCode(request.Code);

            if (verifyResult.IsFailure)
            {
                _logger.LogWarning(
                        "Verify Reset OTP failed for {Email}. Reason: {ErrorMessage}",
                        request.Email, verifyResult.Error.Message);
                return verifyResult;
            }

            _otpRepository.Update(otp);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Password reset OTP verified successfully for {Email}.", request.Email);
            return Result.Success();
        }
    }
}

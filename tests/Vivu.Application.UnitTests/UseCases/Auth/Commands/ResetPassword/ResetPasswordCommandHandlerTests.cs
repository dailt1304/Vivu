using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.ResetPassword;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IOtpRepository> _otpRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<ResetPasswordCommandHandler>> _loggerMock;
        private readonly ResetPasswordCommandHandler _handler;

        public ResetPasswordCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _otpRepositoryMock = new Mock<IOtpRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<ResetPasswordCommandHandler>>();

            _handler = new ResetPasswordCommandHandler(
                _userRepositoryMock.Object,
                _otpRepositoryMock.Object,
                _passwordHasherMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidResetWithVerifiedOtp_ReturnsSuccess()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "NewPassword@123");
            var user = CreateUserWithProfile("test@example.com");
            var otp = CreateVerifiedOtp("test@example.com", user.Id);

            _otpRepositoryMock
                .Setup(x => x.GetVerifiedOtpAsync(command.Email, OtpType.PasswordReset))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.NewPassword))
                .Returns("hashedNewPassword");

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            // Verify interactions
            _otpRepositoryMock.Verify(
                x => x.GetVerifiedOtpAsync(command.Email, OtpType.PasswordReset),
                Times.Once);
            _userRepositoryMock.Verify(
                x => x.FindByEmailAsync(command.Email),
                Times.Once);
            _passwordHasherMock.Verify(
                x => x.HashPassword(command.NewPassword),
                Times.Once);
            _userRepositoryMock.Verify(
                x => x.Update(user),
                Times.Once);
            _otpRepositoryMock.Verify(
                x => x.Update(otp),
                Times.Once);
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidReset_MarksOtpAsUsed()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "NewPassword@123");
            var user = CreateUserWithProfile("test@example.com");
            var otp = CreateVerifiedOtp("test@example.com", user.Id);
            Otp capturedOtp = null;

            _otpRepositoryMock
                .Setup(x => x.GetVerifiedOtpAsync(command.Email, OtpType.PasswordReset))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.NewPassword))
                .Returns("hashedNewPassword");

            _otpRepositoryMock
                .Setup(x => x.Update(It.IsAny<Otp>()))
                .Callback<Otp>(o => capturedOtp = o);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedOtp.Should().NotBeNull();
            capturedOtp.IsUsed.Should().BeTrue();
        }

        #endregion

        #region Failure Path Tests

        [Fact]
        public async Task Handle_NoVerifiedOtp_ReturnsFailure()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "NewPassword@123");

            _otpRepositoryMock
                .Setup(x => x.GetVerifiedOtpAsync(command.Email, OtpType.PasswordReset))
                .ReturnsAsync((Otp)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.OtpInvalid);

            _userRepositoryMock.Verify(
                x => x.FindByEmailAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_OtpExpired_ReturnsFailure()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "NewPassword@123");
            var user = CreateUserWithProfile("test@example.com");
            var expiredOtp = CreateExpiredOtp("test@example.com", user.Id);

            _otpRepositoryMock
                .Setup(x => x.GetVerifiedOtpAsync(command.Email, OtpType.PasswordReset))
                .ReturnsAsync(expiredOtp);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.OtpExpired);
        }

        [Fact]
        public async Task Handle_OtpAlreadyUsed_ReturnsFailure()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "NewPassword@123");
            var user = CreateUserWithProfile("test@example.com");
            var usedOtp = CreateUsedOtp("test@example.com", user.Id);

            _otpRepositoryMock
                .Setup(x => x.GetVerifiedOtpAsync(command.Email, OtpType.PasswordReset))
                .ReturnsAsync(usedOtp);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.OtpAlreadyUsed);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var command = new ResetPasswordCommand("nonexistent@example.com", "NewPassword@123");
            var otp = CreateVerifiedOtp("nonexistent@example.com", Guid.NewGuid());

            _otpRepositoryMock
                .Setup(x => x.GetVerifiedOtpAsync(command.Email, OtpType.PasswordReset))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.User.NotFoundByEmail(command.Email));

            _passwordHasherMock.Verify(
                x => x.HashPassword(It.IsAny<string>()),
                Times.Never);
        }

        #endregion

        #region Helper Methods

        private User CreateUserWithProfile(string email)
        {
            var user = User.Create(
                email,
                "oldhashedpassword",
                "1234567890",
                "Test User"
            );
            return user;
        }

        private Otp CreateVerifiedOtp(string email, Guid userId)
        {
            var otp = Otp.Create(email, OtpType.PasswordReset, userId);
            // Mark as verified by calling VerifyCode
            otp.VerifyCode(otp.Code);
            return otp;
        }

        private Otp CreateExpiredOtp(string email, Guid userId)
        {
            var otp = Otp.Create(email, OtpType.PasswordReset, userId);
            // Manually set ExpiredAt to past time to simulate expired OTP
            var reflection = typeof(Otp).GetProperty(nameof(Otp.ExpiredAt));
            reflection?.SetValue(otp, DateTime.UtcNow.AddMinutes(-5));
            return otp;
        }

        private Otp CreateUsedOtp(string email, Guid userId)
        {
            var otp = Otp.Create(email, OtpType.PasswordReset, userId);
            otp.MarkAsUsed();
            return otp;
        }

        #endregion
    }
}

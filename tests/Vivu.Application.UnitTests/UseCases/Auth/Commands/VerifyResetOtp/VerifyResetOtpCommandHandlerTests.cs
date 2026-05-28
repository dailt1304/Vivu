using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.UseCases.Auth.Commands.VerifyResetOtp;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.VerifyResetOtp
{
    public class VerifyResetOtpCommandHandlerTests
    {
        private readonly Mock<IOtpRepository> _otpRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<VerifyResetOtpCommandHandler>> _loggerMock;
        private readonly VerifyResetOtpCommandHandler _handler;

        public VerifyResetOtpCommandHandlerTests()
        {
            _otpRepositoryMock = new Mock<IOtpRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<VerifyResetOtpCommandHandler>>();

            _handler = new VerifyResetOtpCommandHandler(
                _otpRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidOtpCode_ReturnsSuccess()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "123456");
            var otp = CreateValidOtp("test@example.com", "123456");

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.Code, OtpType.PasswordReset))
                .ReturnsAsync(otp);

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
                x => x.GetValidOtpAsync(command.Email, command.Code, OtpType.PasswordReset),
                Times.Once);
            _otpRepositoryMock.Verify(
                x => x.Update(otp),
                Times.Once);
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidOtp_MarksAsVerified()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "123456");
            var otp = CreateValidOtp("test@example.com", "123456");
            Otp capturedOtp = null;

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.Code, OtpType.PasswordReset))
                .ReturnsAsync(otp);

            _otpRepositoryMock
                .Setup(x => x.Update(It.IsAny<Otp>()))
                .Callback<Otp>(o => capturedOtp = o);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedOtp.Should().NotBeNull();
            capturedOtp.IsVerified.Should().BeTrue();
        }

        #endregion

        #region Failure Path Tests

        [Fact]
        public async Task Handle_InvalidOtpCode_ReturnsFailure()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "654321");

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.Code, OtpType.PasswordReset))
                .ReturnsAsync((Otp)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.OtpInvalid);

            _otpRepositoryMock.Verify(
                x => x.Update(It.IsAny<Otp>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_NoOtpFoundForEmail_ReturnsFailure()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("nonexistent@example.com", "123456");

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.Code, OtpType.PasswordReset))
                .ReturnsAsync((Otp)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.OtpInvalid);
        }

        [Fact]
        public async Task Handle_VerifyCodeFails_ReturnsFailure()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "123456");
            var otp = CreateValidOtp("test@example.com", "654321"); // Different code

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.Code, OtpType.PasswordReset))
                .ReturnsAsync(otp);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();

            _otpRepositoryMock.Verify(
                x => x.Update(It.IsAny<Otp>()),
                Times.Never);
        }

        #endregion

        #region Helper Methods

        private Otp CreateValidOtp(string email, string code)
        {
            var otp = Otp.Create(email, OtpType.PasswordReset, Guid.NewGuid());
            // Update the code to match the expected value
            var codeField = typeof(Otp).GetProperty(nameof(Otp.Code));
            codeField?.SetValue(otp, code);
            return otp;
        }

        #endregion
    }
}

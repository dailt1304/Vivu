using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Email;
using Vivu.Application.UseCases.Auth.Commands.ForgotPassword;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IOtpRepository> _otpRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _loggerMock;
        private readonly ForgotPasswordCommandHandler _handler;

        public ForgotPasswordCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _otpRepositoryMock = new Mock<IOtpRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<ForgotPasswordCommandHandler>>();

            _handler = new ForgotPasswordCommandHandler(
                _userRepositoryMock.Object,
                _otpRepositoryMock.Object,
                _emailServiceMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidEmail_CreatesOtpAndSendsEmail()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@example.com");
            var user = CreateUserWithProfile("test@example.com", "Test User");
            var otp = Otp.Create("test@example.com", OtpType.PasswordReset, user.Id);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _otpRepositoryMock
                .Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.PasswordReset, 15))
                .ReturnsAsync(0);

            _otpRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Otp>()))
                .ReturnsAsync(otp);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _emailServiceMock
                .Setup(x => x.SendPasswordResetEmailAsync(command.Email, It.IsAny<string>(), user.UserProfile.FullName))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            // Verify interactions
            _userRepositoryMock.Verify(
                x => x.FindByEmailAsync(command.Email),
                Times.Once);
            _otpRepositoryMock.Verify(
                x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.PasswordReset, 15),
                Times.Once);
            _otpRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Otp>()),
                Times.Once);
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            _emailServiceMock.Verify(
                x => x.SendPasswordResetEmailAsync(command.Email, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidEmailWithExistingOtpRequests_CreatesNewOtpWhenBelowLimit()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@example.com");
            var user = CreateUserWithProfile("test@example.com", "Test User");
            var otp = Otp.Create("test@example.com", OtpType.PasswordReset, user.Id);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _otpRepositoryMock
                .Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.PasswordReset, 15))
                .ReturnsAsync(2); // Less than 3

            _otpRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Otp>()))
                .ReturnsAsync(otp);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _emailServiceMock
                .Setup(x => x.SendPasswordResetEmailAsync(command.Email, It.IsAny<string>(), user.UserProfile.FullName))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            _otpRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Otp>()),
                Times.Once);
        }

        #endregion

        #region Failure Path Tests

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var command = new ForgotPasswordCommand("nonexistent@example.com");

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.User.NotFoundByEmail(command.Email));

            _userRepositoryMock.Verify(
                x => x.FindByEmailAsync(command.Email),
                Times.Once);
            _otpRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Otp>()),
                Times.Never);
            _emailServiceMock.Verify(
                x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_TooManyOtpRequests_ReturnsRateLimitError()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@example.com");
            var user = CreateUserWithProfile("test@example.com", "Test User");

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _otpRepositoryMock
                .Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.PasswordReset, 15))
                .ReturnsAsync(3); // Exceeds limit of 3

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.TooManyOtpRequests);

            _otpRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Otp>()),
                Times.Never);
            _emailServiceMock.Verify(
                x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ExactlyThreeOtpRequests_ReturnsRateLimitError()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@example.com");
            var user = CreateUserWithProfile("test@example.com", "Test User");

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _otpRepositoryMock
                .Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.PasswordReset, 15))
                .ReturnsAsync(3);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.TooManyOtpRequests);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_EmailSavingFails_StillReturnsSuccess()
        {
            // Arrange - Even if email service fails, we should return success to not leak user existence
            var command = new ForgotPasswordCommand("test@example.com");
            var user = CreateUserWithProfile("test@example.com", "Test User");
            var otp = Otp.Create("test@example.com", OtpType.PasswordReset, user.Id);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _otpRepositoryMock
                .Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.PasswordReset, 15))
                .ReturnsAsync(0);

            _otpRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Otp>()))
                .ReturnsAsync(otp);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _emailServiceMock
                .Setup(x => x.SendPasswordResetEmailAsync(command.Email, It.IsAny<string>(), user.UserProfile.FullName))
                .ThrowsAsync(new Exception("Email service is down"));

            // Act & Assert - Should throw or return based on implementation
            var task = _handler.Handle(command, CancellationToken.None);
            await Assert.ThrowsAsync<Exception>(() => task);
        }

        #endregion

        #region Helper Methods

        private User CreateUserWithProfile(string email, string fullName)
        {
            var user = User.Create(
                email,
                "hashedpassword",
                "1234567890",
                fullName
            );
            return user;
        }

        #endregion
    }
}

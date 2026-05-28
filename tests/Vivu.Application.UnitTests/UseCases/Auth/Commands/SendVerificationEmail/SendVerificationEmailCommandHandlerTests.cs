using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Email;
using Vivu.Application.UseCases.Auth.Commands.SendVerificationEmail;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;

namespace Vivu.Application.UnitTests.UseCases.Auth.Commands.SendVerificationEmail
{
    public class SendVerificationEmailCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IOtpRepository> _otpRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<SendVerificationEmailCommandHandler>> _loggerMock;
        private readonly SendVerificationEmailCommandHandler _handler;

        public SendVerificationEmailCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _otpRepositoryMock = new Mock<IOtpRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<SendVerificationEmailCommandHandler>>();

            _handler = new SendVerificationEmailCommandHandler(
                _userRepositoryMock.Object,
                _otpRepositoryMock.Object,
                _emailServiceMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );
        }

        #region Success Scenarios

        [Fact]
        public async Task Handle_ValidEmailNotRegistered_ReturnsSuccess()
        {
            var command = new SendVerificationEmailCommand("newuser@example.com");

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _otpRepositoryMock
                .Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15))
                .ReturnsAsync(0);

            _otpRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Otp>()))
                .ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _emailServiceMock
                .Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            _userRepositoryMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
            _otpRepositoryMock.Verify(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15), Times.Once);
            _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(command.Email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidEmail_CreatesOtpWithCorrectData()
        {
            var command = new SendVerificationEmailCommand("test@example.com");
            Otp capturedOtp = null;

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(0);

            _otpRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Otp>()))
                .Callback<Otp>(otp => capturedOtp = otp)
                .ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));

            _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            await _handler.Handle(command, CancellationToken.None);

            capturedOtp.Should().NotBeNull();
            capturedOtp.Email.Should().Be(command.Email);
            capturedOtp.Type.Should().Be(OtpType.EmailVerification);
            capturedOtp.UserId.Should().BeNull();
            capturedOtp.Code.Should().NotBeNullOrEmpty();
            capturedOtp.Code.Length.Should().Be(6);
            capturedOtp.ExpiredAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(5));
            capturedOtp.IsUsed.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ValidEmail_SendsEmailWithCorrectOtpCode()
        {
            var command = new SendVerificationEmailCommand("test@example.com");
            string capturedEmail = null;
            string capturedCode = null;

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(0);
            _otpRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Otp>())).ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));

            _emailServiceMock
                .Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((email, code) =>
                {
                    capturedEmail = email;
                    capturedCode = code;
                })
                .Returns(Task.CompletedTask);

            await _handler.Handle(command, CancellationToken.None);

            capturedEmail.Should().Be(command.Email);
            capturedCode.Should().NotBeNullOrEmpty();
            capturedCode.Should().MatchRegex("^[0-9]{6}$"); 
        }

        [Fact]
        public async Task Handle_NoRecentRequests_AllowsOtpCreation()
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(0);
            _otpRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Otp>())).ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));
            _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TwoRecentRequests_AllowsThirdRequest()
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(2);
            _otpRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Otp>())).ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));
            _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Once);
        }

        #endregion

        #region Failure Scenarios

        [Fact]
        public async Task Handle_EmailAlreadyRegistered_ReturnsFailure()
        {
            var command = new SendVerificationEmailCommand("existing@example.com");
            var existingUser = User.Create(command.Email, "Password123@");

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(existingUser);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.User.EmailAlreadyExists);

            _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_RateLimitExceeded_ReturnsFailure()
        {
            var command = new SendVerificationEmailCommand("spam@example.com");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(3);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.TooManyOtpRequests);

            _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_FourRecentRequests_ReturnsFailure()
        {
            var command = new SendVerificationEmailCommand("spam@example.com");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(4);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.TooManyOtpRequests);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_EmailServiceThrowsException_PropagatesException()
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(0);
            _otpRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Otp>())).ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));

            _emailServiceMock
                .Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service unavailable"));

            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

            _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DatabaseSaveThrowsException_PropagatesException()
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(0);
            _otpRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Otp>())).ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

            _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CancellationRequested_ThrowsOperationCanceledException()
        {
            var command = new SendVerificationEmailCommand("test@example.com");
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ThrowsAsync(new OperationCanceledException());

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cancellationTokenSource.Token));
        }

        [Theory]
        [InlineData("test@example.com")]
        [InlineData("TEST@EXAMPLE.COM")]
        [InlineData("Test@Example.Com")]
        public async Task Handle_EmailWithDifferentCasing_ChecksCorrectly(string email)
        {
            var command = new SendVerificationEmailCommand(email);

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(email, OtpType.EmailVerification, 15)).ReturnsAsync(0);
            _otpRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Otp>())).ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));
            _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _userRepositoryMock.Verify(x => x.FindByEmailAsync(email), Times.Once);
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task Handle_SuccessScenario_LogsAppropriateMessages()
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(0);
            _otpRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Otp>())).ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));
            _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            await _handler.Handle(command, CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Email verification requested")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Verification email sent successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_EmailAlreadyExists_LogsWarning()
        {
            var command = new SendVerificationEmailCommand("existing@example.com");
            var existingUser = User.Create(command.Email, "Password123@");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(existingUser);

            await _handler.Handle(command, CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Email already registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RateLimitExceeded_LogsWarning()
        {
            var command = new SendVerificationEmailCommand("spam@example.com");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(3);

            await _handler.Handle(command, CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Rate limit exceeded")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Rate Limiting Tests

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(5, false)]
        public async Task Handle_VariousRecentRequestCounts_BehavesCorrectly(int recentCount, bool shouldSucceed)
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);
            _otpRepositoryMock.Setup(x => x.CountRecentOtpRequestsAsync(command.Email, OtpType.EmailVerification, 15)).ReturnsAsync(recentCount);
            _otpRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Otp>())).ReturnsAsync(Otp.Create(command.Email, OtpType.EmailVerification, null));
            _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().Be(shouldSucceed);

            if (shouldSucceed)
            {
                _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Once);
                _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
            else
            {
                result.Error.Should().Be(DomainErrors.Auth.TooManyOtpRequests);
                _otpRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
                _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
        }

        #endregion
    }
}

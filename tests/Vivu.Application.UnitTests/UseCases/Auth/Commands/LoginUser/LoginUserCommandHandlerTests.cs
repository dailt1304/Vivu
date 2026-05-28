using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.LoginUser
{
    public class LoginUserCommandHandlerTests
    {
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
        private readonly Mock<IAuthTokenProcess> _tokenProcessMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<LoginUserCommandHandler>> _loggerMock;
        private readonly LoginUserCommandHandler _handler;

        public LoginUserCommandHandlerTests()
        {
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            _tokenProcessMock = new Mock<IAuthTokenProcess>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<LoginUserCommandHandler>>();

            _handler = new LoginUserCommandHandler(
                _passwordHasherMock.Object,
                _userRepositoryMock.Object,
                _refreshTokenRepositoryMock.Object,
                _tokenProcessMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidCredentials_ReturnsSuccessWithTokens()
        {
            var command = CreateValidLoginCommand();
            var user = CreateActiveUser();
            var accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test";
            var refreshToken = Guid.NewGuid().ToString();

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
                .Returns(true);

            _tokenProcessMock
                .Setup(x => x.GenerateToken(user, It.IsAny<string[]>()))
                .Returns(accessToken);

            _tokenProcessMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);

            _refreshTokenRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
                .ReturnsAsync(new RefreshToken()); ;

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.AccessToken.Should().Be(accessToken);
            result.Value.RefreshToken.Should().Be(refreshToken);
            result.Value.Email.Should().Be(user.Email);
            result.Value.Id.Should().Be(user.Id);
            result.Value.Roles.Should().NotBeEmpty();

            _userRepositoryMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
            _passwordHasherMock.Verify(x => x.VerifyPassword(command.Password, user.PasswordHash), Times.Once);
            _tokenProcessMock.Verify(x => x.GenerateToken(user, It.IsAny<string[]>()), Times.Once);
            _tokenProcessMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
            _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCredentials_UpdatesLastLoginTime()
        {
            var command = CreateValidLoginCommand();
            var user = CreateActiveUser();
            var originalLastLogin = user.LastLoginAt;

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(true);
            _tokenProcessMock.Setup(x => x.GenerateToken(user, It.IsAny<string[]>())).Returns("token");
            _tokenProcessMock.Setup(x => x.GenerateRefreshToken()).Returns(Guid.NewGuid().ToString());

            await _handler.Handle(command, CancellationToken.None);

            user.LastLoginAt.Should().BeAfter(originalLastLogin.Value);
        }

        [Fact]
        public async Task Handle_ValidCredentials_CreatesRefreshTokenWithCorrectData()
        {
            var command = CreateValidLoginCommand();
            var user = CreateActiveUser();
            RefreshToken capturedRefreshToken = null;

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(true);
            _tokenProcessMock.Setup(x => x.GenerateToken(user, It.IsAny<string[]>())).Returns("token");
            _tokenProcessMock.Setup(x => x.GenerateRefreshToken()).Returns(Guid.NewGuid().ToString());

            _refreshTokenRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
                .Callback<RefreshToken>(rt => capturedRefreshToken = rt)
                .ReturnsAsync(new RefreshToken()); ;

            await _handler.Handle(command, CancellationToken.None);

            capturedRefreshToken.Should().NotBeNull();
            capturedRefreshToken.UserId.Should().Be(user.Id);
            capturedRefreshToken.IpAddress.Should().Be(command.IpAddress);
            capturedRefreshToken.DeviceType.Should().Be(command.DeviceType);
            capturedRefreshToken.DeviceName.Should().Be(command.DeviceName);
            capturedRefreshToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
            capturedRefreshToken.IsUsed.Should().BeFalse();
            capturedRefreshToken.IsRevoked.Should().BeFalse();
        }

        #endregion

        #region Failure Tests - User Not Found

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailureWithNotFoundError()
        {
            var command = CreateValidLoginCommand();

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.User.NotFound);

            _passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _tokenProcessMock.Verify(x => x.GenerateToken(It.IsAny<User>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UserNotFound_LogsWarning()
        {
            var command = CreateValidLoginCommand();
            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User)null);

            await _handler.Handle(command, CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Login failed: User not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Tests - Wrong Password

        [Fact]
        public async Task Handle_WrongPassword_ReturnsFailureWithWrongPasswordError()
        {
            var command = CreateValidLoginCommand();
            var user = CreateActiveUser();

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.User.WrongPassword);

            _tokenProcessMock.Verify(x => x.GenerateToken(It.IsAny<User>(), It.IsAny<string[]>()), Times.Never);
            _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WrongPassword_LogsWarningWithUserInfo()
        {
            var command = CreateValidLoginCommand();
            var user = CreateActiveUser();

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(false);

            await _handler.Handle(command, CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Login failed: Wrong password")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Tests - Banned User

        [Fact]
        public async Task Handle_BannedUser_ReturnsFailureWithBannedError()
        {
            var command = CreateValidLoginCommand();
            var user = CreateBannedUser();

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.User.Banned);

            _tokenProcessMock.Verify(x => x.GenerateToken(It.IsAny<User>(), It.IsAny<string[]>()), Times.Never);
            _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_BannedUser_LogsWarning()
        {
            var command = CreateValidLoginCommand();
            var user = CreateBannedUser();

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(true);

            await _handler.Handle(command, CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Login failed: User is banned")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_UserWithMultipleRoles_ReturnsAllRoles()
        {
            var command = CreateValidLoginCommand();
            var user = CreateUserWithMultipleRoles();

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(true);
            _tokenProcessMock.Setup(x => x.GenerateToken(user, It.IsAny<string[]>())).Returns("token");
            _tokenProcessMock.Setup(x => x.GenerateRefreshToken()).Returns(Guid.NewGuid().ToString());

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Roles.Should().HaveCount(3);
            result.Value.Roles.Should().Contain(new[] { "Admin", "User", "Moderator" });
        }

        [Fact]
        public async Task Handle_CommandWithoutDeviceInfo_SucceedsWithNullDeviceData()
        {
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                IpAddress = "192.168.1.1",
                DeviceType = null,
                DeviceName = null
            };
            var user = CreateActiveUser();

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(true);
            _tokenProcessMock.Setup(x => x.GenerateToken(user, It.IsAny<string[]>())).Returns("token");
            _tokenProcessMock.Setup(x => x.GenerateRefreshToken()).Returns(Guid.NewGuid().ToString());

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_CancellationRequested_ThrowsOperationCanceledException()
        {
            var command = CreateValidLoginCommand();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ThrowsAsync(new OperationCanceledException());

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cancellationTokenSource.Token));
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task Handle_SuccessfulLogin_LogsInformationMessages()
        {
            var command = CreateValidLoginCommand();
            var user = CreateActiveUser();

            _userRepositoryMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash)).Returns(true);
            _tokenProcessMock.Setup(x => x.GenerateToken(user, It.IsAny<string[]>())).Returns("token");
            _tokenProcessMock.Setup(x => x.GenerateRefreshToken()).Returns(Guid.NewGuid().ToString());

            await _handler.Handle(command, CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Login attempt for email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Login successful")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Helper Methods

        private LoginUserCommand CreateValidLoginCommand()
        {
            return new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                IpAddress = "192.168.1.1",
                DeviceType = "Mobile",
                DeviceName = "iPhone 14"
            };
        }

        private User CreateActiveUser()
        {
            var user = User.Create(
                email: "test@example.com",
                passwordHash: "$2a$11$hashedpassword",
                fullName: "Test User",
                avatarUrl: "https://example.com/avatar.jpg"
            );

            user.IsEmailVerified = true;
            user.Status = "active"; 
            user.CreatedDate = DateTime.UtcNow.AddMonths(-1);
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            user.UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    UserId = user.Id, // QUAN TRỌNG: Lấy ID từ user vừa tạo
                    RoleId = Guid.NewGuid(),
                    Role = new Role
                    {
                        Id = Guid.NewGuid(),
                        RoleName = "User"
                    }
                }
            };

            return user;
        }

        private User CreateBannedUser()
        {
            var user = CreateActiveUser();
            user.Status = "banned";
            return user;
        }

        private User CreateUserWithMultipleRoles()
        {
            var user = CreateActiveUser();
            user.UserRoles = new List<UserRole>
            {
                new UserRole { Role = new Role { RoleName = "Admin" } },
                new UserRole { Role = new Role { RoleName = "User" } },
                new UserRole { Role = new Role { RoleName = "Moderator" } }
            };
            return user;
        }

        #endregion
    }
}

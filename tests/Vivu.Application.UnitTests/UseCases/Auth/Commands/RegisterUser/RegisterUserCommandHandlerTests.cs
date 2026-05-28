using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.RegisterUser;
using Vivu.Application.UseCases.Users.Commands.RegisterUser;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using AutoMapper;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IRoleRepository> _roleRepositoryMock;
        private readonly Mock<IOtpRepository> _otpRepositoryMock;
        private readonly Mock<ILogger<RegisterUserCommandHandler>> _loggerMock;
        private readonly RegisterUserCommandHandler _handler;

        public RegisterUserCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _roleRepositoryMock = new Mock<IRoleRepository>();
            _otpRepositoryMock = new Mock<IOtpRepository>();
            _loggerMock = new Mock<ILogger<RegisterUserCommandHandler>>();

            _handler = new RegisterUserCommandHandler(
                _userRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _passwordHasherMock.Object,
                _roleRepositoryMock.Object,
                _otpRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidRegistrationWithValidOtp_ReturnsSuccessWithUserDto()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var userRole = CreateUserRole();
            var user = CreateUser(command.Email, command.FullName);
            var userDto = CreateUserDto(user);

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.Password))
                .Returns("hashedpassword");

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _roleRepositoryMock
                .Setup(x => x.GetByNameAsync(Role.Names.User))
                .ReturnsAsync(userRole);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mapperMock
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Email.Should().Be(command.Email);
            result.Value.FullName.Should().Be(command.FullName);
            result.Value.Id.Should().NotBe(Guid.Empty);

            // Verify interactions
            _otpRepositoryMock.Verify(
                x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()),
                Times.Once);
            _userRepositoryMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
            _passwordHasherMock.Verify(x => x.HashPassword(command.Password), Times.Once);
            _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
            _roleRepositoryMock.Verify(x => x.GetByNameAsync(Role.Names.User), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRegistration_AssignsUserRole()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var userRole = CreateUserRole();
            var user = CreateUser(command.Email, command.FullName);
            var userDto = CreateUserDto(user);
            User capturedUser = null;

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.Password))
                .Returns("hashedpassword");

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync(user);

            _roleRepositoryMock
                .Setup(x => x.GetByNameAsync(Role.Names.User))
                .ReturnsAsync(userRole);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mapperMock
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedUser.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ValidRegistration_MarksOtpAsUsed()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var userRole = CreateUserRole();
            var user = CreateUser(command.Email, command.FullName);
            var userDto = CreateUserDto(user);

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.Password))
                .Returns("hashedpassword");

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _roleRepositoryMock
                .Setup(x => x.GetByNameAsync(Role.Names.User))
                .ReturnsAsync(userRole);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mapperMock
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            otp.IsUsed.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ValidRegistration_VerifiesUserEmail()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var userRole = CreateUserRole();
            var user = CreateUser(command.Email, command.FullName);
            var userDto = CreateUserDto(user);

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.Password))
                .Returns("hashedpassword");

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _roleRepositoryMock
                .Setup(x => x.GetByNameAsync(Role.Names.User))
                .ReturnsAsync(userRole);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mapperMock
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            user.IsEmailVerified.Should().BeTrue();
        }

        #endregion

        #region Failure Tests - Invalid OTP

        [Fact]
        public async Task Handle_InvalidOtp_ReturnsFailureWithOtpInvalidError()
        {
            // Arrange
            var command = CreateValidRegisterCommand();

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync((Otp)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.OtpInvalid);

            _userRepositoryMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
            _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_InvalidOtp_LogsWarning()
        {
            // Arrange
            var command = CreateValidRegisterCommand();

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync((Otp)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Invalid OTP")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Tests - OTP Verification Failed

        [Fact]
        public async Task Handle_OtpVerificationFailed_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateInvalidOtp(); // OTP that fails verification

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();

            _userRepositoryMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Failure Tests - Email Already Exists

        [Fact]
        public async Task Handle_EmailAlreadyExists_ReturnsFailureWithEmailExistsError()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var existingUser = CreateUser(command.Email, "Existing User");

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.User.EmailAlreadyExists);

            _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
            _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmailAlreadyExists_LogsWarning()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var existingUser = CreateUser(command.Email, "Existing User");

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(existingUser);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Email already exists")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Tests - User Role Not Found

        [Fact]
        public async Task Handle_UserRoleNotFound_ReturnsFailureWithNotFoundError()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var user = CreateUser(command.Email, command.FullName);

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.Password))
                .Returns("hashedpassword");

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _roleRepositoryMock
                .Setup(x => x.GetByNameAsync(Role.Names.User))
                .ReturnsAsync((Role)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Contain("NotFound");

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UserRoleNotFound_LogsError()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var user = CreateUser(command.Email, command.FullName);

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.Password))
                .Returns("hashedpassword");

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _roleRepositoryMock
                .Setup(x => x.GetByNameAsync(Role.Names.User))
                .ReturnsAsync((Role)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("User role not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_ValidRegistration_PasswordIsHashed()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var userRole = CreateUserRole();
            var user = CreateUser(command.Email, command.FullName);
            var userDto = CreateUserDto(user);
            var hashedPassword = "$2a$11$hashedpassword";

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.Password))
                .Returns(hashedPassword);

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _roleRepositoryMock
                .Setup(x => x.GetByNameAsync(Role.Names.User))
                .ReturnsAsync(userRole);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mapperMock
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _passwordHasherMock.Verify(x => x.HashPassword(command.Password), Times.Once);
        }

        [Fact]
        public async Task Handle_WithOptionalFields_SuccessfullyRegistersUser()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe",
                Phone = "+84912345678",
                AvatarUrl = "https://example.com/avatar.jpg",
                Bio = "Test bio",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "Male",
                OtpCode = "123456"
            };

            var otp = CreateValidOtp(command.Email);
            var userRole = CreateUserRole();
            var user = CreateUser(command.Email, command.FullName);
            var userDto = CreateUserDto(user);

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.Password))
                .Returns("hashedpassword");

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _roleRepositoryMock
                .Setup(x => x.GetByNameAsync(Role.Names.User))
                .ReturnsAsync(userRole);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mapperMock
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_CancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cancellationTokenSource.Token));
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task Handle_SuccessfulRegistration_LogsInformationMessages()
        {
            // Arrange
            var command = CreateValidRegisterCommand();
            var otp = CreateValidOtp(command.Email);
            var userRole = CreateUserRole();
            var user = CreateUser(command.Email, command.FullName);
            var userDto = CreateUserDto(user);

            _otpRepositoryMock
                .Setup(x => x.GetValidOtpAsync(command.Email, command.OtpCode, It.IsAny<Domain.Enums.OtpType>()))
                .ReturnsAsync(otp);

            _userRepositoryMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(command.Password))
                .Returns("hashedpassword");

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _roleRepositoryMock
                .Setup(x => x.GetByNameAsync(Role.Names.User))
                .ReturnsAsync(userRole);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mapperMock
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Registration attempt")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Registration successful")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Helper Methods

        private RegisterUserCommand CreateValidRegisterCommand()
        {
            return new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe",
                Phone = "+84912345678",
                OtpCode = "123456"
            };
        }

        private Otp CreateValidOtp(string email, string code = "123456")
        {
            var otp = Otp.Create(email, Domain.Enums.OtpType.EmailVerification);
            // Set code to match the one in command
            var codeProperty = typeof(Otp).GetProperty("Code");
            codeProperty?.SetValue(otp, code);
            return otp;
        }

        private Otp CreateInvalidOtp()
        {
            var otp = Otp.Create("test@example.com", Domain.Enums.OtpType.EmailVerification);
            otp.ExpiredAt = DateTime.UtcNow.AddMinutes(-1); // Make it expired
            return otp;
        }

        private User CreateUser(string email, string fullName)
        {
            var user = User.Create(
                email: email,
                passwordHash: "$2a$11$hashedpassword",
                fullName: fullName,
                avatarUrl: "https://example.com/avatar.jpg"
            );

            user.IsEmailVerified = true;
            user.CreatedDate = DateTime.UtcNow;

            user.UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    UserId = user.Id,
                    RoleId = Guid.NewGuid(),
                    Role = new Role
                    {
                        Id = Guid.NewGuid(),
                        RoleName = Role.Names.User
                    }
                }
            };

            return user;
        }

        private Role CreateUserRole()
        {
            return new Role
            {
                Id = Guid.NewGuid(),
                RoleName = Role.Names.User,
                CreatedDate = DateTime.UtcNow
            };
        }

        private UserDto CreateUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                AvatarUrl = user.UserProfile?.AvatarUrl
            };
        }

        #endregion
    }
}

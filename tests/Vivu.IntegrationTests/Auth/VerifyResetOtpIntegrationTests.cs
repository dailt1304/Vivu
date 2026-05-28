using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.ForgotPassword;
using Vivu.Application.UseCases.Auth.Commands.VerifyResetOtp;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.Auth
{
    [Collection("Integration Tests")]
    public class VerifyResetOtpIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public VerifyResetOtpIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        }

        public async Task InitializeAsync()
        {
            await CleanupDatabaseAsync();
            await SeedRolesAndUserAsync();
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<Domain.Entities.UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Otp>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
            await _dbContext.SaveChangesAsync();
        }

        private async Task SeedRolesAndUserAsync()
        {
            var userRole = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = Role.Names.User,
                CreatedDate = DateTime.UtcNow
            };

            _dbContext.Set<Role>().Add(userRole);
            
            // Create a test user
            var user = User.Create(
                "testuser@example.com",
                _passwordHasher.HashPassword("OldPassword@123"),
                "0123456789",
                "Test User"
            );

            _dbContext.Set<User>().Add(user);
            var userRoleEntity = new Domain.Entities.UserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id
            };
            _dbContext.Set<Domain.Entities.UserRole>().Add(userRoleEntity);

            await _dbContext.SaveChangesAsync();
        }

        #region Happy Path Tests

        [Fact]
        public async Task VerifyResetOtp_WithValidCode_SuccessfullyVerifies()
        {
            // Arrange
            const string email = "testuser@example.com";

            // Step 1: Request password reset to generate OTP
            var forgotCommand = new ForgotPasswordCommand(email);
            await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotCommand);

            // Step 2: Get OTP from database
            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset);
            otp.Should().NotBeNull();

            // Step 3: Verify OTP
            var command = new VerifyResetOtpCommand(email, otp.Code);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify OTP is marked as verified
            _dbContext.ChangeTracker.Clear();
            var verifiedOtp = await _dbContext.Set<Otp>()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset && o.Id == otp.Id);
            verifiedOtp.IsVerified.Should().BeTrue();
        }

        #endregion

        #region Failure Path Tests

        [Fact]
        public async Task VerifyResetOtp_WithInvalidCode_ReturnsBadRequest()
        {
            // Arrange
            const string email = "testuser@example.com";
            var command = new VerifyResetOtpCommand(email, "000000");

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task VerifyResetOtp_WithInvalidEmailFormat_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("invalidemail", "123456");

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task VerifyResetOtp_WithExpiredOtp_ReturnsBadRequest()
        {
            // Arrange
            const string email = "testuser@example.com";

            // Create an expired OTP manually
            var userId = _dbContext.Set<User>()
                .First(u => u.Email == email).Id;
            var expiredOtp = Otp.Create(email, OtpType.PasswordReset, userId);
            var reflection = typeof(Otp).GetProperty(nameof(Otp.ExpiredAt));
            reflection?.SetValue(expiredOtp, DateTime.UtcNow.AddMinutes(-5));
            _dbContext.Set<Otp>().Add(expiredOtp);
            await _dbContext.SaveChangesAsync();

            // Try to verify expired OTP
            var command = new VerifyResetOtpCommand(email, "123456");

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task VerifyResetOtp_WithNonExistentEmail_ReturnsBadRequest()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("nonexistent@example.com", "123456");

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task VerifyResetOtp_WithEmptyCode_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("testuser@example.com", "");

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task VerifyResetOtp_WithWrongOtpType_ReturnsBadRequest()
        {
            // Arrange
            const string email = "testuser@example.com";

            // Create email verification OTP instead of password reset OTP
            var userId = _dbContext.Set<User>()
                .First(u => u.Email == email).Id;
            var wrongTypeOtp = Otp.Create(email, OtpType.EmailVerification, userId);
            _dbContext.Set<Otp>().Add(wrongTypeOtp);
            await _dbContext.SaveChangesAsync();

            // Try to verify with wrong OTP type
            var command = new VerifyResetOtpCommand(email, wrongTypeOtp.Code);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task VerifyResetOtp_CannotReverifySameOtp()
        {
            // Arrange
            const string email = "testuser@example.com";

            // Step 1: Request password reset
            var forgotCommand = new ForgotPasswordCommand(email);
            await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotCommand);

            // Step 2: Get OTP and verify it once
            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset);
            var command = new VerifyResetOtpCommand(email, otp.Code);
            var firstVerify = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);
            firstVerify.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 3: Try to verify again with same OTP
            var secondVerify = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert - Should fail because OTP is already verified/used
            secondVerify.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task VerifyResetOtp_WithCodeTooShort_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("testuser@example.com", "12345"); // Only 5 digits

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task VerifyResetOtp_WithCodeTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("testuser@example.com", "1234567"); // 7 digits

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task VerifyResetOtp_WithNonNumericCode_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("testuser@example.com", "ABCDEF");

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion
    }
}

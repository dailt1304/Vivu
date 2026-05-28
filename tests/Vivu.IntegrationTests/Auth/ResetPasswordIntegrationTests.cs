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
using Vivu.Application.UseCases.Auth.Commands.ResetPassword;
using Vivu.Application.UseCases.Auth.Commands.VerifyResetOtp;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.Auth
{
    [Collection("Integration Tests")]
    public class ResetPasswordIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public ResetPasswordIntegrationTests(IntegrationTestWebAppFactory factory)
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
        public async Task ResetPassword_WithValidVerifiedOtp_SuccessfullyResetsPassword()
        {
            // Arrange
            const string email = "testuser@example.com";
            const string newPassword = "NewPassword@456";

            // Step 1: Request password reset
            var forgotCommand = new ForgotPasswordCommand(email);
            await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotCommand);

            // Step 2: Get OTP and verify it
            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset);
            var verifyCommand = new VerifyResetOtpCommand(email, otp.Code);
            await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", verifyCommand);

            // Step 3: Reset password
            var resetCommand = new ResetPasswordCommand(email, newPassword);
            var response = await _client.PostAsJsonAsync("/api/auth/reset-password", resetCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify password changed
            _dbContext.ChangeTracker.Clear();
            var user = await _dbContext.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
            var passwordVerified = _passwordHasher.VerifyPassword(newPassword, user.PasswordHash);
            passwordVerified.Should().BeTrue();
        }

        #endregion

        #region Failure Path Tests

        [Fact]
        public async Task ResetPassword_WithoutVerifyingOtp_ReturnsBadRequest()
        {
            // Arrange
            const string email = "testuser@example.com";
            const string newPassword = "NewPassword@456";

            // Try to reset without verifying OTP first
            var command = new ResetPasswordCommand(email, newPassword);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/reset-password", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Verify original password is unchanged
            var user = await _dbContext.Set<User>()
                .FirstOrDefaultAsync(u => u.Email == email);
            var oldPasswordStillValid = _passwordHasher.VerifyPassword("OldPassword@123", user.PasswordHash);
            oldPasswordStillValid.Should().BeTrue();
        }

        [Fact]
        public async Task ResetPassword_WithExpiredOtp_ReturnsBadRequest()
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

            // Try to reset with expired OTP
            const string newPassword = "NewPassword@456";
            var command = new ResetPasswordCommand(email, newPassword);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/reset-password", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ResetPassword_InvalidPassword_ReturnsUnprocessableEntity()
        {
            // Arrange
            const string email = "testuser@example.com";
            var weakPassword = "weak"; // Too short
            var command = new ResetPasswordCommand(email, weakPassword);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/reset-password", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task ResetPassword_WithoutEmail_ReturnsBadRequest()
        {
            // Arrange
            var command = new ResetPasswordCommand("", "NewPassword@456");

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/reset-password", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ResetPassword_AllowsSamePasswordAfterVerification()
        {
            // Arrange
            const string email = "testuser@example.com";
            const string samePassword = "OldPassword@123";

            // Step 1: Request password reset
            var forgotCommand = new ForgotPasswordCommand(email);
            await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotCommand);

            // Step 2: Get OTP and verify it
            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset);
            var verifyCommand = new VerifyResetOtpCommand(email, otp.Code);
            await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", verifyCommand);

            // Step 3: Reset to same password
            var resetCommand = new ResetPasswordCommand(email, samePassword);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/reset-password", resetCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ResetPassword_MarksOtpAsUsed()
        {
            // Arrange
            const string email = "testuser@example.com";
            const string newPassword = "NewPassword@456";

            // Step 1: Request password reset
            var forgotCommand = new ForgotPasswordCommand(email);
            await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotCommand);

            // Step 2: Get OTP and verify it
            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset);
            var verifyCommand = new VerifyResetOtpCommand(email, otp.Code);
            await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", verifyCommand);
            var otpId = otp.Id;

            // Step 3: Reset password
            var resetCommand = new ResetPasswordCommand(email, newPassword);
            await _client.PostAsJsonAsync("/api/auth/reset-password", resetCommand);

            // Assert
            _dbContext.ChangeTracker.Clear();
            var usedOtp = await _dbContext.Set<Otp>()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == otpId);
            usedOtp.IsUsed.Should().BeTrue();
            usedOtp.UsedAt.Should().NotBeNull();
        }

        #endregion
    }
}

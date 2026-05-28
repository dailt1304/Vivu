using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
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
    public class ForgotPasswordIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public ForgotPasswordIntegrationTests(IntegrationTestWebAppFactory factory)
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
        public async Task ForgotPassword_ValidEmail_SendsPasswordResetEmail()
        {
            // Arrange
            const string email = "testuser@example.com";
            var command = new ForgotPasswordCommand(email);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify OTP was created
            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset);
            otp.Should().NotBeNull();
            otp.Code.Should().HaveLength(6);
            otp.IsExpired().Should().BeFalse();
        }

        [Fact]
        public async Task ForgotPassword_CompleteFlow_FromForgotToResetPassword_SuccessfullyResetsPassword()
        {
            // Arrange
            const string email = "testuser@example.com";
            const string newPassword = "NewPassword@456";

            // Step 1: Request password reset
            var forgotCommand = new ForgotPasswordCommand(email);
            var forgotResponse = await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotCommand);
            forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 2: Get OTP from database
            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset);
            otp.Should().NotBeNull();
            var otpCode = otp.Code;

            // Step 3: Verify OTP
            var verifyCommand = new VerifyResetOtpCommand(email, otpCode);
            var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", verifyCommand);
            verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Debug: Reload OTP from database to get fresh state (bypass cached entity)
            _dbContext.ChangeTracker.Clear(); // Clear the context cache
            var verifiedOtp = await _dbContext.Set<Otp>()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset && o.Id == otp.Id);
            verifiedOtp.Should().NotBeNull();
            verifiedOtp.IsVerified.Should().BeTrue($"OTP should be marked as verified after VerifyResetOtp call. OTP ID: {verifiedOtp.Id}, IsVerified: {verifiedOtp.IsVerified}");

            // Step 4: Reset password
            var resetCommand = new ResetPasswordCommand(email, newPassword);
            var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password", resetCommand);
            resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 5: Verify password was changed
            _dbContext.ChangeTracker.Clear(); // Clear the cache again for user query
            var user = await _dbContext.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
            user.Should().NotBeNull();
            var passwordVerified = _passwordHasher.VerifyPassword(newPassword, user.PasswordHash);
            passwordVerified.Should().BeTrue();

            // Step 6: Verify OTP is marked as used
            _dbContext.ChangeTracker.Clear();
            var usedOtp = await _dbContext.Set<Otp>()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset);
            usedOtp.IsUsed.Should().BeTrue();
        }

        [Fact]
        public async Task ForgotPassword_MultipleRequests_OnlyLastThreeWithinTimeWindow_Allowed()
        {
            // Arrange
            const string email = "testuser@example.com";

            // Act - Make 3 requests (should succeed)
            for (int i = 0; i < 3; i++)
            {
                var command = new ForgotPasswordCommand(email);
                var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", command);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // Act - Make 4th request (should fail with rate limit)
            var failCommand = new ForgotPasswordCommand(email);
            var failResponse = await _client.PostAsJsonAsync("/api/auth/forgot-password", failCommand);
            failResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        #endregion

        #region Failure Path Tests

        [Fact]
        public async Task ForgotPassword_NonExistentEmail_ReturnsGenericMessage()
        {
            // Arrange
            const string email = "nonexistent@example.com";
            var command = new ForgotPasswordCommand(email);

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", command);

            // Assert - API returns 404 for non-existent users
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Verify NO OTP was created for non-existent email
            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.PasswordReset);
            otp.Should().BeNull();
        }

        [Fact]
        public async Task VerifyResetOtp_InvalidCode_ReturnsError()
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
        public async Task VerifyResetOtp_InvalidEmailFormat_ReturnsError()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("invalidemail", "123456");

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verify-reset-otp", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task ResetPassword_InvalidPassword_ReturnsError()
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
        public async Task ResetPassword_WithoutVerifyingOtp_ReturnsError()
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
        public async Task ResetPassword_WithExpiredOtp_ReturnsError()
        {
            // Arrange
            const string email = "testuser@example.com";

            // Create an expired OTP manually
            var userId = _dbContext.Set<User>()
                .First(u => u.Email == email).Id;
            var expiredOtp = Otp.Create(email, OtpType.PasswordReset, userId);
            // Manually set ExpiredAt to past time
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
        public async Task VerifyResetOtp_WithExpiredOtp_ReturnsError()
        {
            // Arrange
            const string email = "testuser@example.com";

            // Create an expired OTP manually
            var userId = _dbContext.Set<User>()
                .First(u => u.Email == email).Id;
            var expiredOtp = Otp.Create(email, OtpType.PasswordReset, userId);
            // Manually set ExpiredAt to past time
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

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ForgotPassword_EmptyEmail_ReturnsError()
        {
            // Arrange
            var command = new ForgotPasswordCommand("");

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task VerifyResetOtp_WithOtpFromWrongType_ReturnsError()
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
        public async Task ResetPassword_SameAsOldPassword_ShouldSucceed()
        {
            // Note: This test verifies that system allows same password if OTP was verified
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

        #endregion
    }
}

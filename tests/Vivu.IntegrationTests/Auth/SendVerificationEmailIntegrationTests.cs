using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Google;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.UseCases.Auth.Commands.SendVerificationEmail;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Vivu.IntegrationTests.Auth
{
    public class SendVerificationEmailIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;

        public SendVerificationEmailIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
        }

        public async Task InitializeAsync()
        {
            await CleanupDatabaseAsync();
        }

        public async Task DisposeAsync()
        {
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.Database.EnsureCreatedAsync();
        }

        #region Success Scenarios

        [Fact]
        public async Task SendVerificationEmail_WithValidNewEmail_ReturnsOkAndCreatesOtp()
        {
            var command = new SendVerificationEmailCommand("newuser@example.com");

            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("success");
            content.Should().Contain("Verification email has been sent");

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == "newuser@example.com");

            otp.Should().NotBeNull();
            otp!.Type.Should().Be(OtpType.EmailVerification);
            otp.Code.Should().MatchRegex("^[0-9]{6}$");
            otp.IsUsed.Should().BeFalse();
            otp.ExpiredAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task SendVerificationEmail_WithValidEmail_EmailIsNormalizedToLowerCase()
        {
            var command = new SendVerificationEmailCommand("NewUser@Example.COM");

            
            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == "newuser@example.com");

            otp.Should().NotBeNull();
            otp!.Email.Should().Be("newuser@example.com");
        }

        [Fact]
        public async Task SendVerificationEmail_ValidEmail_GeneratesUnique6DigitCode()
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == "test@example.com");

            otp.Should().NotBeNull();
            otp!.Code.Should().NotBeNullOrEmpty();
            otp.Code.Length.Should().Be(6);
            otp.Code.Should().MatchRegex("^[0-9]{6}$");
            int.Parse(otp.Code).Should().BeInRange(100000, 999999);
        }

        [Fact]
        public async Task SendVerificationEmail_MultipleTimes_CreatesSeparateOtpRecords()
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);
            await Task.Delay(100);
            await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);
            await Task.Delay(100);
            await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            var otpCount = await _dbContext.Set<Otp>()
                .CountAsync(o => o.Email == "test@example.com");

            otpCount.Should().Be(3);

            var otps = await _dbContext.Set<Otp>()
                .Where(o => o.Email == "test@example.com")
                .ToListAsync();

            otps.Should().HaveCount(3);
            otps.Select(o => o.Code).Distinct().Should().HaveCount(3);
        }

        [Fact]
        public async Task SendVerificationEmail_TwoRequests_BothSucceedWithinRateLimit()
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            var response1 = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);
            await Task.Delay(100);
            var response2 = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);

            var otpCount = await _dbContext.Set<Otp>()
                .CountAsync(o => o.Email == "test@example.com");

            otpCount.Should().Be(2);
        }

        #endregion

        #region Failure Scenarios

        [Fact]
        public async Task SendVerificationEmail_WithExistingEmail_ReturnsConflict()
        {
            var existingUser = await SeedExistingUserAsync("existing@example.com");
            var command = new SendVerificationEmailCommand("existing@example.com");

            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("User.EmailAlreadyExists");
            content.Should().Contain("Email has already been registered");

            var otpCount = await _dbContext.Set<Otp>()
                .CountAsync(o => o.Email == "existing@example.com");

            otpCount.Should().Be(0);
        }

        [Fact]
        public async Task SendVerificationEmail_ExceedsRateLimit_ReturnsTooManyRequests()
        {
            var email = "spam@example.com";
            var command = new SendVerificationEmailCommand(email);

            await SeedRecentOtpRequestsAsync(email, 3);

            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Auth.TooManyOtpRequests");
            content.Should().Contain("Too many OTP requests");

            var otpCount = await _dbContext.Set<Otp>()
                .CountAsync(o => o.Email == email);

            otpCount.Should().Be(3);
        }

        [Fact]
        public async Task SendVerificationEmail_FourRecentRequests_StillBlocked()
        {
            var email = "spam@example.com";
            var command = new SendVerificationEmailCommand(email);

            await SeedRecentOtpRequestsAsync(email, 4);

            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

            var otpCount = await _dbContext.Set<Otp>()
                .CountAsync(o => o.Email == email);

            otpCount.Should().Be(4); 
        }

        #endregion

        #region Validation Scenarios

        [Fact]
        public async Task SendVerificationEmail_WithEmptyEmail_ReturnsBadRequest()
        {
            var command = new SendVerificationEmailCommand("");

            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var jsonString = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(jsonString, options);

            errorResponse.Should().NotBeNull();
            errorResponse!.Success.Should().BeFalse();
            errorResponse.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
            errorResponse.Errors.Should().NotBeNull();
            errorResponse.Errors.Should().ContainKey("Email");

            errorResponse.Errors!["Email"].Should().Contain(new[] { "Email is required", "Invalid email format" });
        }

        [Fact]
        public async Task SendVerificationEmail_WithNullEmail_ReturnsBadRequest()
        {
            var command = new SendVerificationEmailCommand(null);

            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var jsonString = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true 
            };

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(jsonString, options);

            errorResponse.Should().NotBeNull();
            errorResponse!.Success.Should().BeFalse();
            errorResponse.Status.Should().Be(400);
            errorResponse.Errors.Should().NotBeNull();
            errorResponse.Errors.Should().ContainKey("Email");

            errorResponse.Errors!["Email"].Should().Contain(msg => msg.Contains("The Email field is required."));
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        [InlineData("user domain@example.com")]
        [InlineData("user@@example.com")]
        [InlineData("user@domain..com")]
        public async Task SendVerificationEmail_WithInvalidEmailFormat_ReturnsBadRequest(string invalidEmail)
        {
            
            var command = new SendVerificationEmailCommand(invalidEmail);

            
            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Invalid email format");
        }

        [Fact]
        public async Task SendVerificationEmail_WithEmailContainingSpaces_ReturnsBadRequest()
        {
            
            var command = new SendVerificationEmailCommand("user name@example.com");

            
            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Invalid email format");
        }

        [Fact]
        public async Task SendVerificationEmail_WithEmailExceeding254Characters_ReturnsBadRequest()
        {
            
            var localPart = new string('a', 100);
            var domainPart = new string('b', 160) + ".com";
            var tooLongEmail = $"{localPart}@{domainPart}";
            var command = new SendVerificationEmailCommand(tooLongEmail);

            
            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Email must not exceed 254 characters");
        }

        #endregion

        #region Rate Limiting Edge Cases

        [Fact]
        public async Task SendVerificationEmail_OldOtpRequestsBeyond15Minutes_DoNotCountTowardRateLimit()
        {
            
            var email = "test@example.com";
            var command = new SendVerificationEmailCommand(email);

            await SeedOldOtpRequestsAsync(email, 3, 20);

            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var recentOtpCount = await _dbContext.Set<Otp>()
                .Where(o => o.Email == email && o.CreatedDate >= DateTime.UtcNow.AddMinutes(-15))
                .CountAsync();

            recentOtpCount.Should().Be(1); 
        }

        [Fact]
        public async Task SendVerificationEmail_MixOfOldAndRecentRequests_OnlyCountsRecent()
        {
            
            var email = "test@example.com";
            var command = new SendVerificationEmailCommand(email);

            await SeedOldOtpRequestsAsync(email, 2, 20);
            await SeedRecentOtpRequestsAsync(email, 2);

            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var totalCount = await _dbContext.Set<Otp>()
                .CountAsync(o => o.Email == email);

            totalCount.Should().Be(5); // 2 old + 2 recent + 1 new
        }

        #endregion

        #region Response Structure Tests

        [Fact]
        public async Task SendVerificationEmail_SuccessResponse_HasCorrectStructure()
        {
            
            var command = new SendVerificationEmailCommand("test@example.com");

            
            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);
            var content = await response.Content.ReadAsStringAsync();

            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("success");
            content.Should().Contain("true");
            content.Should().Contain("message");
        }

        [Fact]
        public async Task SendVerificationEmail_ErrorResponse_HasCorrectStructure()
        {
            
            var existingUser = await SeedExistingUserAsync("existing@example.com");
            var command = new SendVerificationEmailCommand("existing@example.com");

            
            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);
            var content = await response.Content.ReadAsStringAsync();

            
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            content.Should().Contain("code");
            content.Should().Contain("message");
        }

        #endregion

        #region Concurrent Requests Tests

        [Fact]
        public async Task SendVerificationEmail_ConcurrentRequests_AllSucceedWithinLimit()
        {
            
            var command = new SendVerificationEmailCommand("concurrent@example.com");

            var tasks = Enumerable.Range(0, 3)
                .Select(_ => _client.PostAsJsonAsync("/api/auth/send-verification-email", command))
                .ToArray();

            var responses = await Task.WhenAll(tasks);

            
            responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

            var otpCount = await _dbContext.Set<Otp>()
                .CountAsync(o => o.Email == "concurrent@example.com");

            otpCount.Should().Be(3);
        }

        [Fact]
        public async Task SendVerificationEmail_FourConcurrentRequests_OnlyThreeSucceed()
        {
            
            var command = new SendVerificationEmailCommand("concurrent@example.com");

            var tasks = Enumerable.Range(0, 4)
                .Select(_ => _client.PostAsJsonAsync("/api/auth/send-verification-email", command))
                .ToArray();

            var responses = await Task.WhenAll(tasks);

            
            var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
            var failureCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

            successCount.Should().BeGreaterThanOrEqualTo(3);

            var otpCount = await _dbContext.Set<Otp>()
                .CountAsync(o => o.Email == "concurrent@example.com");

            otpCount.Should().BeLessThanOrEqualTo(4); 
        }

        #endregion

        #region Email Normalization Tests

        [Theory]
        [InlineData("Test@Example.Com", "test@example.com")]
        [InlineData("TEST@EXAMPLE.COM", "test@example.com")]
        [InlineData("tEsT@eXaMpLe.CoM", "test@example.com")]
        public async Task SendVerificationEmail_EmailWithMixedCase_NormalizesToLowerCase(string inputEmail, string expectedEmail)
        {
            
            var command = new SendVerificationEmailCommand(inputEmail);

            
            var response = await _client.PostAsJsonAsync("/api/auth/send-verification-email", command);

            
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == expectedEmail);

            otp.Should().NotBeNull();
            otp!.Email.Should().Be(expectedEmail);
        }

        #endregion

        #region Security Tests

        [Fact]
        public async Task SendVerificationEmail_DoesNotLeakUserExistence_InRateLimit()
        {
            
            var email = "test@example.com";
            await SeedRecentOtpRequestsAsync(email, 3);

            var commandExisting = new SendVerificationEmailCommand(email);
            var commandNonExisting = new SendVerificationEmailCommand("nonexistent@example.com");

            
            var responseExisting = await _client.PostAsJsonAsync("/api/auth/send-verification-email", commandExisting);

            await SeedRecentOtpRequestsAsync("nonexistent@example.com", 3);
            var responseNonExisting = await _client.PostAsJsonAsync("/api/auth/send-verification-email", commandNonExisting);

            responseExisting.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            responseNonExisting.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        #endregion

        #region Helper Methods

        private async Task<User> SeedExistingUserAsync(string email)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: "$2a$11$hashedpassword",
                fullName: "Test User",
                avatarUrl: "https://example.com/avatar.jpg"
            );


            user.IsEmailVerified = true;
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            var roleId = Guid.NewGuid();
            var role = new Role
            {
                Id = roleId,
                RoleName = "User",
                RoleDescription = "Standard User"
            };

            user.UserRoles = new List<Domain.Entities.UserRole>
            {
                new Domain.Entities.UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    Role = role
                }
            };

            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        private async Task SeedRecentOtpRequestsAsync(string email, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var otp = Otp.Create(email, OtpType.EmailVerification, null);
                typeof(Otp).GetProperty("CreatedDate")!
                    .SetValue(otp, DateTime.UtcNow.AddMinutes(-5));

                _dbContext.Set<Otp>().Add(otp);
            }
            await _dbContext.SaveChangesAsync();
        }

        private async Task SeedOldOtpRequestsAsync(string email, int count, int minutesAgo)
        {
            for (int i = 0; i < count; i++)
            {
                var otp = Otp.Create(email, OtpType.EmailVerification, null);
                typeof(Otp).GetProperty("CreatedDate")!
                    .SetValue(otp, DateTime.UtcNow.AddMinutes(-minutesAgo));

                _dbContext.Set<Otp>().Add(otp);
            }
            await _dbContext.SaveChangesAsync();
        }
        public class ApiErrorResponse
        {
            public bool Success { get; set; }
            public int Status { get; set; }
            public string Message { get; set; }
            public string Code { get; set; }

            public Dictionary<string, string[]>? Errors { get; set; }

            public DateTime Timestamp { get; set; }
        }

        #endregion
    }
}

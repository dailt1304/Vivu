using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.UseCases.Auth.Commands.RegisterUser;
using Vivu.Application.UseCases.Auth.Commands.SendVerificationEmail;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Application.Interfaces.Auth;
using FluentAssertions;

namespace Vivu.IntegrationTests.Auth
{
    [Collection("Integration Tests")]
    public class RegisterIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterIntegrationTests(IntegrationTestWebAppFactory factory)
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
            await SeedRolesAsync();
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

        private async Task SeedRolesAsync()
        {
            var userRole = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = Role.Names.User,
                CreatedDate = DateTime.UtcNow
            };

            _dbContext.Set<Role>().Add(userRole);
            await _dbContext.SaveChangesAsync();
        }

        #region Happy Path Tests

        [Fact]
        public async Task Register_CompleteFlow_FromSendEmailToRegister_SuccessfullyCreatesUser()
        {
            const string email = "newuser@example.com";
            const string password = "SecurePassword123!";
            const string fullName = "Test User";

            // Step 1: Send verification email
            var sendEmailCommand = new SendVerificationEmailCommand(email);
            var sendEmailResponse = await _client.PostAsJsonAsync("/api/auth/send-verification-email", sendEmailCommand);
            sendEmailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 2: Get OTP from database
            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.EmailVerification);
            otp.Should().NotBeNull();

            // Step 3: Register user with OTP
            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FullName = fullName,
                Phone = "+84912345678",
                OtpCode = otp!.Code
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await registerResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserDto>>(jsonString, options);

            apiResponse!.Data.Should().NotBeNull();
            var userResponse = apiResponse.Data;
            userResponse!.Email.Should().Be(email);
            userResponse.FullName.Should().Be(fullName);
            userResponse.Id.Should().NotBe(Guid.Empty);

            // Step 4: Verify user is in database with correct data
            var createdUser = await _dbContext.Set<User>()
                .Include(u => u.UserProfile)
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == email);

            createdUser.Should().NotBeNull();
            createdUser!.Email.Should().Be(email);
            createdUser.IsEmailVerified.Should().BeTrue();
            createdUser.UserProfile!.FullName.Should().Be(fullName);
            createdUser.Phone.Should().Be("+84912345678");
            createdUser.UserRoles.Should().HaveCount(1);
            createdUser.UserRoles.First().Role!.RoleName.Should().Be(Role.Names.User);
        }

        [Fact]
        public async Task Register_WithAllOptionalFields_SuccessfullyCreatesUserWithAllData()
        {
            const string email = "fulluser@example.com";
            const string password = "SecurePassword123!";
            const string fullName = "Full Test User";
            const string phone = "+84912345678";

            // Send verification email
            var sendEmailCommand = new SendVerificationEmailCommand(email);
            await _client.PostAsJsonAsync("/api/auth/send-verification-email", sendEmailCommand);

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.EmailVerification);

            otp.Should().NotBeNull();

            // Register with optional phone field
            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FullName = fullName,
                Phone = phone,
                OtpCode = otp!.Code
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await _dbContext.Set<User>()
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);

            user.Should().NotBeNull();
            user!.Email.Should().Be(email);
            user.Phone.Should().Be(phone);
            user.IsEmailVerified.Should().BeTrue();
            user.UserProfile!.FullName.Should().Be(fullName);
        }

        [Fact]
        public async Task Register_WithGender_SuccessfullyCreatesUserWithGender()
        {
            const string email = "genderuser@example.com";
            const string password = "SecurePassword123!";
            const string fullName = "Gender User";
            const string gender = "Male";

            // Send verification email
            var sendEmailCommand = new SendVerificationEmailCommand(email);
            await _client.PostAsJsonAsync("/api/auth/send-verification-email", sendEmailCommand);

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.EmailVerification);

            otp.Should().NotBeNull();

            // Register with gender field
            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FullName = fullName,
                Gender = gender,
                OtpCode = otp!.Code
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await _dbContext.Set<User>()
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);

            user.Should().NotBeNull();
            user!.UserProfile!.Gender.Should().Be(gender);
        }

        [Fact]
        public async Task Register_WithValidCredentials_PasswordIsHashedNotStored()
        {
            const string email = "hashtest@example.com";
            const string password = "SecurePassword123!";

            var sendEmailCommand = new SendVerificationEmailCommand(email);
            await _client.PostAsJsonAsync("/api/auth/send-verification-email", sendEmailCommand);

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email);

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FullName = "Hash Test",
                OtpCode = otp!.Code
            };

            await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

            var user = await _dbContext.Set<User>().FirstOrDefaultAsync(u => u.Email == email);
            user!.PasswordHash.Should().NotBe(password);
            user.PasswordHash.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Failure Tests - Invalid OTP

        [Fact]
        public async Task Register_WithInvalidOtp_ReturnsBadRequest()
        {
            const string email = "invalidotp@example.com";

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = "SecurePassword123!",
                FullName = "Test User",
                OtpCode = "000000"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithoutSendingVerificationEmail_ReturnsBadRequest()
        {
            const string email = "noemailsent@example.com";

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = "SecurePassword123!",
                FullName = "Test User",
                OtpCode = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithExpiredOtp_ReturnsBadRequest()
        {
            const string email = "expiredotp@example.com";

            // Create an expired OTP manually
            var expiredOtp = Otp.Create(email, OtpType.EmailVerification);
            expiredOtp.ExpiredAt = DateTime.UtcNow.AddMinutes(-1);

            _dbContext.Set<Otp>().Add(expiredOtp);
            await _dbContext.SaveChangesAsync();

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = "SecurePassword123!",
                FullName = "Test User",
                OtpCode = expiredOtp.Code
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Failure Tests - Validation

        [Fact]
        public async Task Register_WithInvalidEmail_ReturnsBadRequest()
        {
            var registerCommand = new RegisterUserCommand
            {
                Email = "notanemailformat",
                Password = "SecurePassword123!",
                FullName = "Test User",
                OtpCode = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task Register_WithWeakPassword_ReturnsBadRequest()
        {
            const string email = "weakpass@example.com";

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = "weak",
                FullName = "Test User",
                OtpCode = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task Register_WithoutFullName_ReturnsBadRequest()
        {
            const string email = "nofullname@example.com";

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = "SecurePassword123!",
                FullName = "",
                OtpCode = "123456"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task Register_WithInvalidPhone_ReturnsBadRequest()
        {
            const string email = "invalidphone@example.com";

            // Send verification email first
            var sendEmailCommand = new SendVerificationEmailCommand(email);
            await _client.PostAsJsonAsync("/api/auth/send-verification-email", sendEmailCommand);

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email);

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = "SecurePassword123!",
                FullName = "Test User",
                Phone = "123",
                OtpCode = otp!.Code
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Register_ReuseOtp_SecondAttemptFails()
        {
            const string email1 = "reusetest1@example.com";
            const string email2 = "reusetest2@example.com";

            // Send verification email and get OTP
            var sendEmailCommand = new SendVerificationEmailCommand(email1);
            await _client.PostAsJsonAsync("/api/auth/send-verification-email", sendEmailCommand);

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email1);

            // First registration
            var registerCommand1 = new RegisterUserCommand
            {
                Email = email1,
                Password = "SecurePassword123!",
                FullName = "Test User 1",
                OtpCode = otp!.Code
            };

            var response1 = await _client.PostAsJsonAsync("/api/auth/register", registerCommand1);
            response1.StatusCode.Should().Be(HttpStatusCode.OK);

            // Try to reuse the same OTP for another email
            var registerCommand2 = new RegisterUserCommand
            {
                Email = email2,
                Password = "SecurePassword123!",
                FullName = "Test User 2",
                OtpCode = otp.Code
            };

            var response2 = await _client.PostAsJsonAsync("/api/auth/register", registerCommand2);
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_MultipleUsersSequentially_AllSucceed()
        {
            var users = new List<(string email, string fullName)>
            {
                ("user1@example.com", "User One"),
                ("user2@example.com", "User Two"),
                ("user3@example.com", "User Three")
            };

            foreach (var (email, fullName) in users)
            {
                // Send verification email
                var sendEmailCommand = new SendVerificationEmailCommand(email);
                await _client.PostAsJsonAsync("/api/auth/send-verification-email", sendEmailCommand);

                var otp = await _dbContext.Set<Otp>()
                    .FirstOrDefaultAsync(o => o.Email == email);

                // Register
                var registerCommand = new RegisterUserCommand
                {
                    Email = email,
                    Password = "SecurePassword123!",
                    FullName = fullName,
                    OtpCode = otp!.Code
                };

                var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // Verify all users are in database
            var userCount = await _dbContext.Set<User>().CountAsync();
            userCount.Should().Be(3);

            var allUsers = await _dbContext.Set<User>()
                .Include(u => u.UserProfile)
                .ToListAsync();

            allUsers.Should().AllSatisfy(u => u.IsEmailVerified.Should().BeTrue());
            allUsers.Select(u => u.UserProfile!.FullName).Should().Contain(users.Select(u => u.fullName));
        }

        [Fact]
        public async Task Register_EmailNormalizedToLowerCase()
        {
            const string emailNormalized = "test@example.com";
            const string emailMixed = "Test@Example.COM";

            var sendEmailCommand = new SendVerificationEmailCommand(emailMixed);
            await _client.PostAsJsonAsync("/api/auth/send-verification-email", sendEmailCommand);

            var otp = await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == emailNormalized);

            otp.Should().NotBeNull();

            var registerCommand = new RegisterUserCommand
            {
                Email = emailNormalized,
                Password = "SecurePassword123!",
                FullName = "Test User",
                OtpCode = otp!.Code
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await _dbContext.Set<User>()
                .FirstOrDefaultAsync(u => u.Email == emailNormalized);

            user.Should().NotBeNull();
            user!.Email.Should().Be(emailNormalized);
        }

        #endregion

        #region Helper Methods

        private async Task<Otp> GetOtpByEmailAsync(string email)
        {
            return await _dbContext.Set<Otp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.Type == OtpType.EmailVerification)
                ?? throw new InvalidOperationException($"OTP not found for email: {email}");
        }

        #endregion
    }


}

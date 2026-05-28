using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Google;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Domain.Entities;
using Vivu.Application.Interfaces.Auth;
using FluentAssertions;

namespace Vivu.IntegrationTests.Auth
{

    [Collection("Integration Tests")]
    public class LoginIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public LoginIntegrationTests(IntegrationTestWebAppFactory factory)
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
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Happy Path Integration Tests

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithTokens()
        {
            var testUser = await SeedTestUserAsync("testuser@example.com", "Password123!");
            var loginRequest = new LoginUserCommand
            {
                Email = "testuser@example.com",
                Password = "Password123!",
                IpAddress = "192.168.1.1",
                DeviceType = "Desktop",
                DeviceName = "Test Device"
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var jsonString = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(jsonString, options);

            var loginResponse = apiResponse!.Data;
            loginResponse.Should().NotBeNull();
            loginResponse!.AccessToken.Should().NotBeNullOrEmpty();
            loginResponse.RefreshToken.Should().NotBeNullOrEmpty();
            loginResponse.Email.Should().Be("testuser@example.com");
            loginResponse.Id.Should().Be(testUser.Id);
            loginResponse.Roles.Should().NotBeEmpty();
            loginResponse.RefreshTokenExpiryTime.Should().BeCloseTo(
                DateTime.UtcNow.AddDays(7),
                TimeSpan.FromMinutes(1)
            );
        }

        [Fact]
        public async Task Login_WithValidCredentials_CreatesRefreshTokenInDatabase()
        {
            var testUser = await SeedTestUserAsync("user@example.com", "Password123!");
            var loginRequest = new LoginUserCommand
            {
                Email = "user@example.com",
                Password = "Password123!",
                IpAddress = "10.0.0.1",
                DeviceType = "Mobile",
                DeviceName = "iPhone 14"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            var loginResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

            var refreshToken = await _dbContext.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.UserId == testUser.Id);

            refreshToken.Should().NotBeNull();
            refreshToken!.Token.Should().Be(loginResponse!.Data.RefreshToken);
            refreshToken.IsUsed.Should().BeFalse();
            refreshToken.IsRevoked.Should().BeFalse();
            refreshToken.ExpiresAt.Should().BeCloseTo(
                DateTime.UtcNow.AddDays(7),
                TimeSpan.FromMinutes(1)
            );
        }

        [Fact]
        public async Task Login_WithValidCredentials_UpdatesLastLoginTime()
        {
            var testUser = await SeedTestUserAsync("lastlogin@example.com", "Password123!");
            var originalLastLogin = testUser.LastLoginAt;
            await Task.Delay(1000); 

            var loginRequest = new LoginUserCommand
            {
                Email = "lastlogin@example.com",
                Password = "Password123!"
            };

            await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            _dbContext.Entry(testUser).State = EntityState.Detached;

            var updatedUser = await _dbContext.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == testUser.Id);

            updatedUser.Should().NotBeNull();
            updatedUser!.LastLoginAt.Should().BeAfter(originalLastLogin.Value);
        }

        [Fact]
        public async Task Login_MultipleTimes_CreatesMultipleRefreshTokens()
        {
            var testUser = await SeedTestUserAsync("multi@example.com", "Password123!");
            var loginRequest = new LoginUserCommand
            {
                Email = "multi@example.com",
                Password = "Password123!"
            };

            await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            var refreshTokenCount = await _dbContext.Set<RefreshToken>()
                .CountAsync(rt => rt.UserId == testUser.Id);

            refreshTokenCount.Should().Be(3);
        }

        #endregion

        #region Failure Integration Tests


        

        [Fact]
        public async Task Login_WithBannedUser_ReturnsForbidden()
        {
            var bannedUser = await SeedTestUserAsync("banned@example.com", "Password123!");
            bannedUser.Status = "banned";
            await _dbContext.SaveChangesAsync();

            var loginRequest = new LoginUserCommand
            {
                Email = "banned@example.com",
                Password = "Password123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var errorContent = await response.Content.ReadAsStringAsync();
            errorContent.Should().Contain("User account has been banned");
        }

        [Fact]
        public async Task Login_WithWrongPassword_DoesNotCreateRefreshToken()
        {
            var testUser = await SeedTestUserAsync("notoken@example.com", "Password123!");
            var loginRequest = new LoginUserCommand
            {
                Email = "notoken@example.com",
                Password = "WrongPassword!"
            };

            await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            var refreshTokenCount = await _dbContext.Set<RefreshToken>()
                .CountAsync(rt => rt.UserId == testUser.Id);

            refreshTokenCount.Should().Be(0);
        }

        #endregion

        #region Validation Integration Tests

        

        [Fact]
        public async Task Login_WithNullEmailAndPassword_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginUserCommand
            {
                Email = null,
                Password = null
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Security Integration Tests

        [Fact]
        public async Task Login_EmailIsCaseInsensitive_SucceedsWithDifferentCase()
        {
            // Arrange
            await SeedTestUserAsync("CaseInsensitive@Example.COM", "Password123!");
            var loginRequest = new LoginUserCommand
            {
                Email = "caseinsensitive@example.com", // Different case
                Password = "Password123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

       

        #endregion

        #region Database Constraints Tests (Only possible with real PostgreSQL)

        [Fact]
        public async Task Login_EmailUniqueness_EnforcedByDatabase()
        {
            // Arrange
            var user1 = await SeedTestUserAsync("duplicate@example.com", "Password123!");

            // Act & Assert - Try to create duplicate email
            var duplicateUser = CreateUserEntity("duplicate@example.com", "Password456!");
            _dbContext.Set<User>().Add(duplicateUser);

            // Should throw DbUpdateException due to UNIQUE constraint
            await Assert.ThrowsAsync<DbUpdateException>(
                async () => await _dbContext.SaveChangesAsync()
            );
        }

        [Fact]
        public async Task Login_ForeignKeyConstraint_EnforcedByDatabase()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();
            var refreshToken = RefreshToken.Create(
                userId: nonExistentUserId,
                token: Guid.NewGuid().ToString(),
                expiresAt: DateTime.UtcNow.AddDays(7),
                ipAddress: "127.0.0.1",
                deviceType: "Test",
                deviceName: "Test"
            );

            // Act & Assert - Should throw due to foreign key constraint
            _dbContext.Set<RefreshToken>().Add(refreshToken);

            await Assert.ThrowsAsync<DbUpdateException>(
                async () => await _dbContext.SaveChangesAsync()
            );
        }

        #endregion

        #region Response Format Tests

        
        [Fact]
        public async Task Login_ErrorResponse_HasCorrectStructure()
        {
            // Arrange
            var loginRequest = new LoginUserCommand
            {
                Email = "notfound@example.com",
                Password = "Password123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            // Assert
            json.RootElement.TryGetProperty("code", out _).Should().BeTrue();
            json.RootElement.TryGetProperty("message", out _).Should().BeTrue();
        }

        #endregion

        #region Concurrent Login Tests

        [Fact]
        public async Task Login_ConcurrentRequests_AllSucceedWithIsolation()
        {
            // Arrange
            var testUser = await SeedTestUserAsync("concurrent@example.com", "Password123!");
            var loginRequest = new LoginUserCommand
            {
                Email = "concurrent@example.com",
                Password = "Password123!"
            };

            // Act - Simulate 5 concurrent logins
            var tasks = Enumerable.Range(0, 5)
                .Select(_ => _client.PostAsJsonAsync("/api/auth/login", loginRequest))
                .ToArray();

            var responses = await Task.WhenAll(tasks);

            // Assert
            responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

            // Verify all 5 refresh tokens created in database
            var refreshTokenCount = await _dbContext.Set<RefreshToken>()
                .CountAsync(rt => rt.UserId == testUser.Id);

            refreshTokenCount.Should().Be(5);
        }

        #endregion


        

        #region Helper Methods
        private async Task<User> SeedTestUserAsync(string email, string password)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword(password),
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

            user.UserRoles = new List<UserRole>
            {
                new UserRole
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

        private User CreateUserEntity(string email, string password)
        {
            // 1. Tạo User bằng Factory Method
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword(password),
                fullName: "Test User",
                avatarUrl: "https://example.com/avatar.jpg"
            );

            // 2. Override các property cho test
            user.IsEmailVerified = true;
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            // 3. Setup Role giả
            var roleId = Guid.NewGuid();

            // 4. Gán Role
            user.UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    Role = new Role
                    {
                        Id = roleId,
                        RoleName = "User",
                        RoleDescription = "Standard User"
                    }
                }
            };

            return user;
        }

        #endregion
    }
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
    }
}

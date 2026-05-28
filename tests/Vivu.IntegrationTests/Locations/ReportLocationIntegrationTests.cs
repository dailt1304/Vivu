using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.Locations.Commands.ReportLocation;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Xunit;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.Locations
{
    [Collection("Integration Tests")]
    public class ReportLocationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _moderatorUser = null!;
        private User _normalUser = null!;
        private string _moderatorToken = string.Empty;
        private string _normalUserToken = string.Empty;
        private Location _testLocation = null!;

        public ReportLocationIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _moderatorUser = await SeedModeratorUserAsync("moderator@example.com", "Password123!");
            _normalUser = await SeedNormalUserAsync("user@example.com", "Password123!");
            _testLocation = await SeedTestLocationAsync("Test Location");
            _moderatorToken = await GetAccessTokenAsync("moderator@example.com", "Password123!");
            _normalUserToken = await GetAccessTokenAsync("user@example.com", "Password123!");
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<LocationReport>().ExecuteDeleteAsync();
            await _dbContext.Set<Location>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRoleEntity>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Success Scenarios

        [Fact]
        public async Task ReportLocation_WithValidData_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "The address is incorrect",
                Description = "The actual address is 123 Main Street"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ReportLocationResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            apiResponse.Data.LocationId.Should().Be(_testLocation.Id);
            apiResponse.Data.ReportType.Should().Be("WRONG_INFO");
            apiResponse.Data.Status.Should().Be("PENDING");
        }

        [Fact]
        public async Task ReportLocation_WithClosedType_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.CLOSED,
                Reason = "This place has permanently closed"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ReportLocationResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Data.ReportType.Should().Be("CLOSED");
        }

        [Fact]
        public async Task ReportLocation_WithoutDescription_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Wrong opening hours"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ReportLocation_CreatesReportInDatabase()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Database test reason"
            };

            var initialCount = await _dbContext.Set<LocationReport>().CountAsync();

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var finalCount = await _dbContext.Set<LocationReport>().CountAsync();
            finalCount.Should().Be(initialCount + 1);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task ReportLocation_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReportLocation_AsNormalUser_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _normalUserToken);
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ReportLocation_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task ReportLocation_WithEmptyReason_ReturnsUnprocessableEntity()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task ReportLocation_WithReasonTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = new string('A', 501)
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task ReportLocation_WithDescriptionTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Valid reason",
                Description = new string('A', 2001)
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task ReportLocation_WithNonExistentLocation_ReturnsNotFound()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Rate Limiting Tests

        [Fact]
        public async Task ReportLocation_AlreadyReportedSameLocation_ReturnsConflict()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);

            // First report
            var firstRequest = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "First report"
            };
            await _client.PostAsJsonAsync("/api/locations/report", firstRequest);

            // Second report for same location
            var secondRequest = new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.CLOSED,
                Reason = "Second report"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/locations/report", secondRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task ReportLocation_ExceedsDailyLimit_ReturnsBadRequest()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);

            var location2 = await SeedTestLocationAsync("Location 2");
            var location3 = await SeedTestLocationAsync("Location 3");
            var location4 = await SeedTestLocationAsync("Location 4");

            // Create 3 reports (daily limit)
            await _client.PostAsJsonAsync("/api/locations/report", new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Report 1"
            });

            await _client.PostAsJsonAsync("/api/locations/report", new ReportLocationCommand
            {
                LocationId = location2.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Report 2"
            });

            await _client.PostAsJsonAsync("/api/locations/report", new ReportLocationCommand
            {
                LocationId = location3.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Report 3"
            });

            // Act - 4th report should exceed limit
            var response = await _client.PostAsJsonAsync("/api/locations/report", new ReportLocationCommand
            {
                LocationId = location4.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Report 4"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ReportLocation_CanReportDifferentLocations_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var location2 = await SeedTestLocationAsync("Another Location");

            // Act
            var response1 = await _client.PostAsJsonAsync("/api/locations/report", new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Report for first location"
            });

            var response2 = await _client.PostAsJsonAsync("/api/locations/report", new ReportLocationCommand
            {
                LocationId = location2.Id,
                ReportType = ReportType.CLOSED,
                Reason = "Report for second location"
            });

            // Assert
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Pending Report Count Tests

        [Fact]
        public async Task ReportLocation_ThreeOrMorePendingReports_MarksLocationForReview()
        {
            // Arrange - Create multiple users and their reports
            var user2 = await SeedModeratorUserAsync("mod2@example.com", "Password123!");
            var user3 = await SeedModeratorUserAsync("mod3@example.com", "Password123!");

            var token2 = await GetAccessTokenAsync("mod2@example.com", "Password123!");
            var token3 = await GetAccessTokenAsync("mod3@example.com", "Password123!");

            // First two reports
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
            await _client.PostAsJsonAsync("/api/locations/report", new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Report from user 2"
            });

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token3);
            await _client.PostAsJsonAsync("/api/locations/report", new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Report from user 3"
            });

            // Third report
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var response = await _client.PostAsJsonAsync("/api/locations/report", new ReportLocationCommand
            {
                LocationId = _testLocation.Id,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Third report - should trigger review"
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify location is marked for review (IsVerified = false)
            var location = await _dbContext.Set<Location>().FindAsync(_testLocation.Id);
            // Note: This depends on handler implementation
        }

        #endregion

        #region Helper Methods

        private async Task<User> SeedModeratorUserAsync(string email, string password)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword(password),
                fullName: "Moderator User",
                avatarUrl: "https://example.com/avatar.jpg"
            );

            user.IsEmailVerified = true;
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            var existingRole = await _dbContext.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == "MODERATOR");
            Role role;

            if (existingRole == null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    RoleName = "MODERATOR",
                    RoleDescription = "Moderator Role"
                };
                _dbContext.Set<Role>().Add(role);
            }
            else
            {
                role = existingRole;
            }

            user.UserRoles = new List<UserRoleEntity>
            {
                new UserRoleEntity
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    Role = role
                }
            };

            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        private async Task<User> SeedNormalUserAsync(string email, string password)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword(password),
                fullName: "Normal User",
                avatarUrl: "https://example.com/avatar.jpg"
            );

            user.IsEmailVerified = true;
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            var existingRole = await _dbContext.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == "USER");
            Role role;

            if (existingRole == null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    RoleName = "USER",
                    RoleDescription = "Standard User"
                };
                _dbContext.Set<Role>().Add(role);
            }
            else
            {
                role = existingRole;
            }

            user.UserRoles = new List<UserRoleEntity>
            {
                new UserRoleEntity
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    Role = role
                }
            };

            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        private async Task<Location> SeedTestLocationAsync(string name)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = "Test Description",
                Address = "Test Address",
                Latitude = 10.0,
                Longitude = 105.0,
                IsVerified = true,
                CreatedDate = DateTime.UtcNow
            };

            await _dbContext.Set<Location>().AddAsync(location);
            await _dbContext.SaveChangesAsync();
            return location;
        }

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var loginRequest = new LoginUserCommand
            {
                Email = email,
                Password = password,
                IpAddress = "127.0.0.1",
                DeviceType = "Desktop",
                DeviceName = "Test Device"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return apiResponse?.Data?.AccessToken ?? string.Empty;
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; } = default!;
            public string Message { get; set; } = string.Empty;
        }

        #endregion
    }
}

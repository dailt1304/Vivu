using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.Locations.Commands.ApproveLocationSuggestion;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Xunit;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.Locations
{
    [Collection("Integration Tests")]
    public class ApproveLocationSuggestionIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _adminUser = null!;
        private User _moderatorUser = null!;
        private User _normalUser = null!;
        private string _adminToken = string.Empty;
        private string _moderatorToken = string.Empty;
        private string _normalUserToken = string.Empty;

        public ApproveLocationSuggestionIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _adminUser = await SeedUserWithRoleAsync("admin_approve@example.com", "Password123!", "ADMIN");
            _moderatorUser = await SeedUserWithRoleAsync("moderator_approve@example.com", "Password123!", "MODERATOR");
            _normalUser = await SeedUserWithRoleAsync("user_approve@example.com", "Password123!", "USER");
            _adminToken = await GetAccessTokenAsync("admin_approve@example.com", "Password123!");
            _moderatorToken = await GetAccessTokenAsync("moderator_approve@example.com", "Password123!");
            _normalUserToken = await GetAccessTokenAsync("user_approve@example.com", "Password123!");
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<LocationReport>().ExecuteDeleteAsync();
            await _dbContext.Set<LocationDetail>().ExecuteDeleteAsync();
            await _dbContext.Set<Location>().ExecuteDeleteAsync();
            await _dbContext.Set<LocationCategory>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRoleEntity>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Helper Methods

        private async Task<User> SeedUserWithRoleAsync(string email, string password, string roleName)
        {
            var role = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = roleName,
                RoleDescription = $"{roleName} Role"
            };
            await _dbContext.Set<Role>().AddAsync(role);

            var hashedPassword = _passwordHasher.HashPassword(password);
            var user = User.Create(email, hashedPassword, $"{roleName} User");
            user.IsEmailVerified = true;
            await _dbContext.Users.AddAsync(user);

            var userRole = new UserRoleEntity { UserId = user.Id, RoleId = role.Id };
            await _dbContext.Set<UserRoleEntity>().AddAsync(userRole);

            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var loginRequest = new LoginUserCommand
            {
                Email = email,
                Password = password,
                IpAddress = "127.0.0.1",
                DeviceType = "Test",
                DeviceName = "Test Device"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var loginResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return loginResponse!.Data!.AccessToken;
        }

        private async Task<(Location location, LocationReport report)> SeedLocationWithPendingReportAsync(
            string name,
            Guid userId,
            bool isVerified = false)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = "Test Description",
                Address = "123 Test Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = isVerified,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

            await _dbContext.Set<Location>().AddAsync(location);

            var report = new LocationReport
            {
                Id = Guid.NewGuid(),
                LocationId = location.Id,
                UserId = userId,
                ReportType = ReportType.NEW_LOCATION.ToString(),
                ReportReason = "New location submission",
                Status = ReportStatus.PENDING.ToString(),
                CreatedDate = DateTime.UtcNow
            };

            await _dbContext.Set<LocationReport>().AddAsync(report);
            await _dbContext.SaveChangesAsync();

            return (location, report);
        }

        private async Task<Location> SeedLocationWithoutReportAsync(string name)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = "Test Description",
                Address = "456 Test Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = false,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

            await _dbContext.Set<Location>().AddAsync(location);
            await _dbContext.SaveChangesAsync();

            return location;
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task ApproveLocationSuggestion_ValidRequestByAdmin_ReturnsOkAndApprovesLocation()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Test Location 1", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = "Approved after verification"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Id.Should().Be(location.Id);
            apiResponse.Data.IsVerified.Should().BeTrue();
        }

        [Fact]
        public async Task ApproveLocationSuggestion_ValidRequestByModerator_ReturnsOkAndApprovesLocation()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Test Location 2", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = "Approved by moderator"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>();
            apiResponse!.Data.IsVerified.Should().BeTrue();
        }

        [Fact]
        public async Task ApproveLocationSuggestion_ValidRequest_UpdatesLocationInDatabase()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Database Update Test", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = "Test approval"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            _dbContext.ChangeTracker.Clear();
            var updatedLocation = await _dbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            updatedLocation.Should().NotBeNull();
            updatedLocation!.IsVerified.Should().BeTrue();
        }

        [Fact]
        public async Task ApproveLocationSuggestion_ValidRequest_UpdatesReportStatusToApproved()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Report Status Test", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = "Approved with note"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            _dbContext.ChangeTracker.Clear();
            var updatedReport = await _dbContext.Set<LocationReport>()
                .FirstOrDefaultAsync(r => r.Id == report.Id);

            updatedReport.Should().NotBeNull();
            updatedReport!.Status.Should().Be(ReportStatus.APPROVED.ToString());
            updatedReport.AdminNote.Should().Be("Approved with note");
            updatedReport.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task ApproveLocationSuggestion_WithoutAdminNote_ApprovesSuccessfully()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("No Note Test", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = null
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedReport = await _dbContext.Set<LocationReport>()
                .FirstOrDefaultAsync(r => r.Id == report.Id);

            updatedReport!.Status.Should().Be(ReportStatus.APPROVED.ToString());
            updatedReport.AdminNote.Should().BeNull();
        }

        [Fact]
        public async Task ApproveLocationSuggestion_AlreadyVerifiedLocation_StillApprovesReport()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Already Verified", _normalUser.Id, isVerified: true);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = "Re-approving"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedReport = await _dbContext.Set<LocationReport>()
                .FirstOrDefaultAsync(r => r.Id == report.Id);

            updatedReport!.Status.Should().Be(ReportStatus.APPROVED.ToString());
        }

        #endregion

        #region Authentication & Authorization Tests

        [Fact]
        public async Task ApproveLocationSuggestion_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Auth Test 1", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = null;

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ApproveLocationSuggestion_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Auth Test 2", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ApproveLocationSuggestion_WithNormalUserToken_ReturnsForbidden()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Auth Test 3", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _normalUserToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region Not Found Scenarios

        [Fact]
        public async Task ApproveLocationSuggestion_NonExistentLocation_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = nonExistentId,
                AdminNote = "Test"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{nonExistentId}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ApproveLocationSuggestion_LocationWithoutPendingReport_ReturnsBadRequest()
        {
            // Arrange
            var location = await SeedLocationWithoutReportAsync("No Report Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = "Test"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task ApproveLocationSuggestion_EmptyLocationId_ReturnsUnprocessableEntity()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.Empty,
                AdminNote = "Test"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{Guid.Empty}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ApproveLocationSuggestion_LongAdminNote_ProcessesSuccessfully()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Long Note Test", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var longNote = new string('A', 500);
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = longNote
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedReport = await _dbContext.Set<LocationReport>()
                .FirstOrDefaultAsync(r => r.Id == report.Id);

            updatedReport!.AdminNote.Should().Be(longNote);
        }

        [Fact]
        public async Task ApproveLocationSuggestion_MultipleApprovals_OnlyFirstSucceeds()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Multiple Approvals", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = "First approval"
            };

            // Act
            var response1 = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);
            
            command.AdminNote = "Second approval";
            var response2 = await _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            // Assert
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest); // NoPendingReport error returns 400
        }

        [Fact]
        public async Task ApproveLocationSuggestion_ConcurrentApprovals_HandledCorrectly()
        {
            // Arrange
            var (location, report) = await SeedLocationWithPendingReportAsync("Concurrent Test", _normalUser.Id);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = location.Id,
                AdminNote = "Concurrent approval"
            };

            // Act
            var task1 = _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);
            var task2 = _client.PutAsJsonAsync($"/api/locations/{location.Id}/approve", command);

            var responses = await Task.WhenAll(task1, task2);

            // Assert
            var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
            var badRequestCount = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

            // At least one should succeed, and any failures should be BadRequest (NoPendingReport)
            successCount.Should().BeGreaterThanOrEqualTo(1);
            (successCount + badRequestCount).Should().Be(2);
        }

        #endregion

        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; } = default!;
            public int StatusCode { get; set; }
            public string? Message { get; set; }
            public string? Code { get; set; }
        }
    }
}

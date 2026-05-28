using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Xunit;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.LocationReports
{
    [Collection("Integration Tests")]
    public class ReviewLocationReportIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
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
        private LocationReport _pendingReport = null!;
        private LocationReport _approvedReport = null!;

        public ReviewLocationReportIntegrationTests(IntegrationTestWebAppFactory factory)
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
            await SeedLocationReportsAsync();
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
        public async Task ReviewLocationReport_ApproveReport_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new
            {
                reportId = _pendingReport.Id,
                status = (int)ReportStatus.APPROVED,
                adminNote = "Report verified and approved"
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewLocationReportResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(_pendingReport.Id);
            apiResponse.Data.Status.Should().Be("APPROVED");
            apiResponse.Data.AdminNote.Should().Be("Report verified and approved");
        }

        [Fact]
        public async Task ReviewLocationReport_RejectReport_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new
            {
                reportId = _pendingReport.Id,
                status = (int)ReportStatus.REJECTED,
                adminNote = "Insufficient evidence"
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewLocationReportResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Data!.Status.Should().Be("REJECTED");
        }

        [Fact]
        public async Task ReviewLocationReport_WithoutAdminNote_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new
            {
                reportId = _pendingReport.Id,
                status = (int)ReportStatus.APPROVED
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task ReviewLocationReport_WithEmptyReportId_ReturnsUnprocessableEntity()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new
            {
                reportId = Guid.Empty,
                status = (int)ReportStatus.APPROVED
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task ReviewLocationReport_WithPendingStatus_ReturnsUnprocessableEntity()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new
            {
                reportId = _pendingReport.Id,
                status = (int)ReportStatus.PENDING
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task ReviewLocationReport_WithInvalidStatus_ReturnsUnprocessableEntity()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new
            {
                reportId = _pendingReport.Id,
                status = 999
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task ReviewLocationReport_WithAdminNoteExceedingMaxLength_ReturnsUnprocessableEntity()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new
            {
                reportId = _pendingReport.Id,
                status = (int)ReportStatus.APPROVED,
                adminNote = new string('A', 1001)
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task ReviewLocationReport_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var request = new
            {
                reportId = _pendingReport.Id,
                status = (int)ReportStatus.APPROVED
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReviewLocationReport_AsNormalUser_ReturnsForbidden()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _normalUserToken);
            var request = new
            {
                reportId = _pendingReport.Id,
                status = (int)ReportStatus.APPROVED
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region Error Cases

        [Fact]
        public async Task ReviewLocationReport_WithNonExistentReportId_ReturnsNotFound()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new
            {
                reportId = Guid.NewGuid(),
                status = (int)ReportStatus.APPROVED
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ReviewLocationReport_AlreadyProcessedReport_ReturnsConflict()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var request = new
            {
                reportId = _approvedReport.Id,
                status = (int)ReportStatus.APPROVED
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/location-reports/review", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

            var roleId = Guid.NewGuid();
            var role = new Role
            {
                Id = roleId,
                RoleName = "MODERATOR",
                RoleDescription = "Moderator Role"
            };

            user.UserRoles = new List<UserRoleEntity>
            {
                new UserRoleEntity
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

            var roleId = Guid.NewGuid();
            var role = new Role
            {
                Id = roleId,
                RoleName = "USER",
                RoleDescription = "Standard User"
            };

            user.UserRoles = new List<UserRoleEntity>
            {
                new UserRoleEntity
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
                IsVerified = true
            };

            await _dbContext.Set<Location>().AddAsync(location);
            await _dbContext.SaveChangesAsync();
            return location;
        }

        private async Task SeedLocationReportsAsync()
        {
            _pendingReport = LocationReport.Create(
                _testLocation.Id,
                _normalUser.Id,
                "WRONG_INFO",
                "Test reason",
                "Test description"
            );
            typeof(LocationReport).GetProperty("Status")!.SetValue(_pendingReport, "PENDING");
            await _dbContext.Set<LocationReport>().AddAsync(_pendingReport);

            _approvedReport = LocationReport.Create(
                _testLocation.Id,
                _normalUser.Id,
                "CLOSED",
                "Already closed",
                "This location is closed"
            );
            typeof(LocationReport).GetProperty("Status")!.SetValue(_approvedReport, "APPROVED");
            await _dbContext.Set<LocationReport>().AddAsync(_approvedReport);

            await _dbContext.SaveChangesAsync();
        }

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var loginRequest = new LoginUserCommand
            {
                Email = email,
                Password = password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var loginResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return loginResponse?.Data?.AccessToken ?? string.Empty;
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; } = default!;
            public string Message { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }

        #endregion
    }
}

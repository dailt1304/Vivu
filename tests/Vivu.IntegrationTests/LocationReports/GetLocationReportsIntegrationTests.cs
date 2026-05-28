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
using Vivu.Infrastructure.Data;
using Xunit;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.LocationReports
{
    public class TestPaginatedList<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }

    [Collection("Integration Tests")]
    public class GetLocationReportsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
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

        public GetLocationReportsIntegrationTests(IntegrationTestWebAppFactory factory)
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
        public async Task GetLocationReports_AsModerator_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);

            // Act
            var response = await _client.GetAsync("/api/location-reports");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationReportDto>>>(jsonString, options);

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Items.Should().NotBeEmpty();
            apiResponse.Data.TotalCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetLocationReports_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);

            // Act
            var response = await _client.GetAsync("/api/location-reports?pageNumber=1&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationReportDto>>>();
            result!.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(2);
            result.Data.Items.Should().HaveCountLessThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetLocationReports_FilterByStatus_ReturnsFilteredResults()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);

            // Act
            var response = await _client.GetAsync("/api/location-reports?status=PENDING");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationReportDto>>>();
            result!.Data.Items.Should().NotBeEmpty();
            result.Data.Items.Should().OnlyContain(r => r.Status.ToUpper() == "PENDING");
        }

        [Fact]
        public async Task GetLocationReports_FilterByReportType_ReturnsFilteredResults()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);

            // Act
            var response = await _client.GetAsync("/api/location-reports?reportType=WRONG_INFO");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationReportDto>>>();
            result!.Data.Items.Should().NotBeEmpty();
            result.Data.Items.Should().OnlyContain(r => r.ReportType.ToUpper() == "WRONG_INFO");
        }

        [Fact]
        public async Task GetLocationReports_ReturnsCompleteReportData()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);

            // Act
            var response = await _client.GetAsync("/api/location-reports");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationReportDto>>>();
            var firstReport = result!.Data.Items.First();

            firstReport.Id.Should().NotBeEmpty();
            firstReport.LocationId.Should().NotBeEmpty();
            firstReport.LocationName.Should().NotBeNullOrEmpty();
            firstReport.ReportType.Should().NotBeNullOrEmpty();
            firstReport.Reason.Should().NotBeNullOrEmpty();
            firstReport.Status.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task GetLocationReports_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/location-reports");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetLocationReports_AsNormalUser_ReturnsForbidden()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _normalUserToken);

            // Act
            var response = await _client.GetAsync("/api/location-reports");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetLocationReports_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.GetAsync("/api/location-reports");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
                Description = "A test location",
                Address = "123 Test Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = true
            };

            _dbContext.Set<Location>().Add(location);
            await _dbContext.SaveChangesAsync();

            return location;
        }

        private async Task SeedLocationReportsAsync()
        {
            var reports = new List<LocationReport>
            {
                new LocationReport
                {
                    Id = Guid.NewGuid(),
                    LocationId = _testLocation.Id,
                    UserId = _normalUser.Id,
                    ReportType = "WRONG_INFO",
                    ReportReason = "Incorrect address",
                    Status = "PENDING",
                    CreatedDate = DateTime.UtcNow.AddDays(-3)
                },
                new LocationReport
                {
                    Id = Guid.NewGuid(),
                    LocationId = _testLocation.Id,
                    UserId = _normalUser.Id,
                    ReportType = "CLOSED",
                    ReportReason = "Place is closed",
                    Status = "PENDING",
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                },
                new LocationReport
                {
                    Id = Guid.NewGuid(),
                    LocationId = _testLocation.Id,
                    UserId = _normalUser.Id,
                    ReportType = "WRONG_INFO",
                    ReportReason = "Wrong opening hours",
                    Status = "APPROVED",
                    AdminNote = "Verified and updated",
                    CreatedDate = DateTime.UtcNow.AddDays(-1)
                }
            };

            _dbContext.Set<LocationReport>().AddRange(reports);
            await _dbContext.SaveChangesAsync();
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
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

            return apiResponse!.Data.AccessToken;
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

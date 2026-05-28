using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Statistics;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Xunit;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.Statistics
{
    [Collection("Integration Tests")]
    public class GetLocationStatsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _adminUser = null!;
        private User _normalUser = null!;
        private string _adminToken = string.Empty;
        private string _normalUserToken = string.Empty;

        public GetLocationStatsIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _adminUser = await SeedAdminUserAsync("admin_stats@example.com", "Password123!");
            _normalUser = await SeedNormalUserAsync("user_stats@example.com", "Password123!");
            _adminToken = await GetAccessTokenAsync("admin_stats@example.com", "Password123!");
            _normalUserToken = await GetAccessTokenAsync("user_stats@example.com", "Password123!");
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

        private async Task<User> SeedAdminUserAsync(string email, string password)
        {
            var user = User.Create(email, _passwordHasher.HashPassword(password), "Admin User");
            user.IsEmailVerified = true;
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            var role = await GetOrCreateRoleAsync("ADMIN", "Administrator Role");
            user.UserRoles = new List<UserRoleEntity>
            {
                new() { UserId = user.Id, RoleId = role.Id, Role = role }
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<User> SeedNormalUserAsync(string email, string password)
        {
            var user = User.Create(email, _passwordHasher.HashPassword(password), "Normal User");
            user.IsEmailVerified = true;
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            var role = await GetOrCreateRoleAsync("USER", "Standard User");
            user.UserRoles = new List<UserRoleEntity>
            {
                new() { UserId = user.Id, RoleId = role.Id, Role = role }
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<Role> GetOrCreateRoleAsync(string roleName, string description)
        {
            var existing = await _dbContext.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (existing != null) return existing;

            var role = new Role { Id = Guid.NewGuid(), RoleName = roleName, RoleDescription = description };
            _dbContext.Set<Role>().Add(role);
            await _dbContext.SaveChangesAsync();
            return role;
        }

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var loginRequest = new LoginUserCommand { Email = email, Password = password };
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<StatsApiResponse<StatsLoginResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return apiResponse!.Data!.AccessToken;
        }

        private async Task<LocationCategory> SeedCategoryAsync(string name)
        {
            var category = new LocationCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                IconUrl = "https://example.com/icon.png"
            };
            await _dbContext.Set<LocationCategory>().AddAsync(category);
            await _dbContext.SaveChangesAsync();
            return category;
        }

        private async Task<Location> SeedLocationAsync(
            string name,
            Guid? categoryId = null,
            bool isVerified = true,
            bool isDeleted = false)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = "Test description",
                Address = "123 Test Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = isVerified,
                IsDeleted = isDeleted,
                CategoryId = categoryId,
                CreatedDate = DateTime.UtcNow
            };
            await _dbContext.Set<Location>().AddAsync(location);
            await _dbContext.SaveChangesAsync();
            return location;
        }

        private async Task SeedPendingNewLocationReportAsync(Guid locationId)
        {
            var report = LocationReport.Create(
                locationId: locationId,
                userId: _normalUser.Id,
                reportType: ReportType.NEW_LOCATION.ToString(),
                reportReason: "New location submission",
                reportDescription: null
            );
            await _dbContext.Set<LocationReport>().AddAsync(report);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<StatsApiResponse<LocationStatsDto>> GetStatsAsync()
        {
            var response = await _client.GetAsync("/api/statistics/locations");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            return (await response.Content.ReadFromJsonAsync<StatsApiResponse<LocationStatsDto>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!;
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task GetLocationStats_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/statistics/locations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetLocationStats_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

            // Act
            var response = await _client.GetAsync("/api/statistics/locations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetLocationStats_WithNormalUserToken_ReturnsForbidden()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _normalUserToken);

            // Act
            var response = await _client.GetAsync("/api/statistics/locations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetLocationStats_WithAdminToken_ReturnsOk()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/statistics/locations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task GetLocationStats_WithNoData_ReturnsZeroStats()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var apiResponse = await GetStatsAsync();

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.TotalVerifiedLocations.Should().Be(0);
            apiResponse.Data.PendingSubmissionsCount.Should().Be(0);
            apiResponse.Data.VerificationRate.Should().Be(0);
            apiResponse.Data.LocationsByCategory.Should().BeEmpty();
            apiResponse.Data.ReportsByType.Should().BeEmpty();
        }

        [Fact]
        public async Task GetLocationStats_WithVerifiedLocations_ReturnsTotalVerifiedCount()
        {
            // Arrange
            var category = await SeedCategoryAsync("Food & Drink");
            await SeedLocationAsync("Restaurant A", category.Id, isVerified: true);
            await SeedLocationAsync("Restaurant B", category.Id, isVerified: true);
            await SeedLocationAsync("Restaurant C", category.Id, isVerified: false);  // not verified

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var apiResponse = await GetStatsAsync();

            // Assert
            apiResponse.Data!.TotalVerifiedLocations.Should().Be(2);
        }

        [Fact]
        public async Task GetLocationStats_WithMixedVerification_ReturnsCorrectVerificationRate()
        {
            // Arrange
            var category = await SeedCategoryAsync("Nature");
            await SeedLocationAsync("Park A", category.Id, isVerified: true);
            await SeedLocationAsync("Park B", category.Id, isVerified: true);
            await SeedLocationAsync("Park C", category.Id, isVerified: true);
            await SeedLocationAsync("Park D", category.Id, isVerified: false); // 3/4 = 75%

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var apiResponse = await GetStatsAsync();

            // Assert
            apiResponse.Data!.VerificationRate.Should().Be(75.0m);
        }

        [Fact]
        public async Task GetLocationStats_WithDeletedLocations_ExcludesThemFromStats()
        {
            // Arrange
            var category = await SeedCategoryAsync("Entertainment");
            await SeedLocationAsync("Active Location", category.Id, isVerified: true, isDeleted: false);
            await SeedLocationAsync("Deleted Location", category.Id, isVerified: true, isDeleted: true); // should be excluded

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var apiResponse = await GetStatsAsync();

            // Assert
            apiResponse.Data!.TotalVerifiedLocations.Should().Be(1); // only non-deleted
            apiResponse.Data.VerificationRate.Should().Be(100.0m);   // 1/1 non-deleted
        }

        [Fact]
        public async Task GetLocationStats_WithLocationsByCategory_ReturnsCorrectBreakdown()
        {
            // Arrange
            var food = await SeedCategoryAsync("Food");
            var nature = await SeedCategoryAsync("Nature");

            await SeedLocationAsync("Restaurant 1", food.Id);
            await SeedLocationAsync("Restaurant 2", food.Id);
            await SeedLocationAsync("Park 1", nature.Id);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var apiResponse = await GetStatsAsync();

            // Assert
            apiResponse.Data!.LocationsByCategory.Should().ContainKey("Food")
                .WhoseValue.Should().Be(2);
            apiResponse.Data.LocationsByCategory.Should().ContainKey("Nature")
                .WhoseValue.Should().Be(1);
        }

        [Fact]
        public async Task GetLocationStats_WithPendingNewLocationReports_ReturnsCorrectCount()
        {
            // Arrange
            var category = await SeedCategoryAsync("Museums");
            var location1 = await SeedLocationAsync("Museum A", category.Id);
            var location2 = await SeedLocationAsync("Museum B", category.Id);
            await SeedPendingNewLocationReportAsync(location1.Id);
            await SeedPendingNewLocationReportAsync(location2.Id);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var apiResponse = await GetStatsAsync();

            // Assert
            apiResponse.Data!.PendingSubmissionsCount.Should().Be(2);
        }

        #endregion

        #region Response Structure Tests

        [Fact]
        public async Task GetLocationStats_ResponseHasCorrectStructure()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/statistics/locations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<StatsApiResponse<LocationStatsDto>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.LocationsByCategory.Should().NotBeNull();
            apiResponse.Data.ReportsByType.Should().NotBeNull();
        }

        #endregion
    }

    #region Response Helper Classes

    public class StatsApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class StatsLoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    #endregion
}

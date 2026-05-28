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
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Xunit;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.Locations
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
    public class GetPendingLocationsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
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

        public GetPendingLocationsIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _adminUser = await SeedUserWithRoleAsync("admin_pending@example.com", "Password123!", "ADMIN");
            _moderatorUser = await SeedUserWithRoleAsync("moderator_pending@example.com", "Password123!", "MODERATOR");
            _normalUser = await SeedUserWithRoleAsync("user_pending@example.com", "Password123!", "USER");

            await SeedPendingLocationsAsync();

            _adminToken = await GetAccessTokenAsync("admin_pending@example.com", "Password123!");
            _moderatorToken = await GetAccessTokenAsync("moderator_pending@example.com", "Password123!");
            _normalUserToken = await GetAccessTokenAsync("user_pending@example.com", "Password123!");
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

        private async Task SeedPendingLocationsAsync()
        {
            // Create 5 locations with pending reports
            for (int i = 1; i <= 5; i++)
            {
                var location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = $"Pending Location {i}",
                    Description = $"Test Description {i}",
                    Address = $"{i}23 Test Street",
                    Latitude = 10.762622 + (i * 0.001),
                    Longitude = 106.660172 + (i * 0.001),
                    IsVerified = false,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow.AddDays(-i)
                };

                await _dbContext.Set<Location>().AddAsync(location);

                var report = new LocationReport
                {
                    Id = Guid.NewGuid(),
                    LocationId = location.Id,
                    UserId = _normalUser?.Id ?? Guid.NewGuid(),
                    ReportType = ReportType.NEW_LOCATION.ToString(),
                    ReportReason = $"New location submission {i}",
                    Status = ReportStatus.PENDING.ToString(),
                    CreatedDate = DateTime.UtcNow.AddDays(-i)
                };

                await _dbContext.Set<LocationReport>().AddAsync(report);
            }

            // Create 3 locations without pending reports (should not be returned)
            for (int i = 1; i <= 3; i++)
            {
                var location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = $"Approved Location {i}",
                    Description = $"Approved Description {i}",
                    Address = $"{i}00 Approved Street",
                    Latitude = 10.762622 + (i * 0.01),
                    Longitude = 106.660172 + (i * 0.01),
                    IsVerified = true,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow.AddDays(-i)
                };

                await _dbContext.Set<Location>().AddAsync(location);

                var report = new LocationReport
                {
                    Id = Guid.NewGuid(),
                    LocationId = location.Id,
                    UserId = _normalUser?.Id ?? Guid.NewGuid(),
                    ReportType = ReportType.NEW_LOCATION.ToString(),
                    ReportReason = $"Approved location {i}",
                    Status = ReportStatus.APPROVED.ToString(),
                    CreatedDate = DateTime.UtcNow.AddDays(-i)
                };

                await _dbContext.Set<LocationReport>().AddAsync(report);
            }

            await _dbContext.SaveChangesAsync();
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task GetPendingLocations_AsAdmin_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(jsonString, options);

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().NotBeEmpty();
            apiResponse.Data.Items.Should().HaveCount(5);
            apiResponse.Data.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task GetPendingLocations_AsModerator_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetPendingLocations_WithDefaultPagination_ReturnsAllPendingLocations()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(5);
            result.Data.TotalCount.Should().Be(5);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetPendingLocations_ReturnsOnlyPendingLocations()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(5);
            result.Data.Items.Should().AllSatisfy(loc =>
            {
                loc.Name.Should().Contain("Pending Location");
                loc.IsVerified.Should().BeFalse();
            });
        }

        [Fact]
        public async Task GetPendingLocations_ReturnsCompleteLocationData()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var firstLocation = result!.Data!.Items.First();
            firstLocation.Id.Should().NotBeEmpty();
            firstLocation.Name.Should().NotBeNullOrEmpty();
            firstLocation.Description.Should().NotBeNullOrEmpty();
            firstLocation.Address.Should().NotBeNullOrEmpty();
            firstLocation.Latitude.Should().NotBe(0);
            firstLocation.Longitude.Should().NotBe(0);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task GetPendingLocations_WithCustomPageSize_ReturnsCorrectNumberOfItems()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending?pageNumber=1&pageSize=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(3);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(3);
            result.Data.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task GetPendingLocations_WithSecondPage_ReturnsRemainingItems()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending?pageNumber=2&pageSize=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(2);
            result.Data.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(3);
            result.Data.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task GetPendingLocations_WithPageSizeExceedingTotal_ReturnsAllItems()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending?pageNumber=1&pageSize=100");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(5);
            result.Data.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task GetPendingLocations_WithPageNumberExceedingTotal_ReturnsEmptyItems()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending?pageNumber=10&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(5);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task GetPendingLocations_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetPendingLocations_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token-12345");

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetPendingLocations_AsNormalUser_ReturnsForbidden()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _normalUserToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetPendingLocations_WithExpiredToken_ReturnsUnauthorized()
        {
            // Arrange
            var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTYyMzkwMjJ9.invalid";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Empty Result Tests

        [Fact]
        public async Task GetPendingLocations_WhenNoPendingLocations_ReturnsEmptyList()
        {
            // Arrange
            await CleanupDatabaseAsync();
            _adminUser = await SeedUserWithRoleAsync("admin_empty@example.com", "Password123!", "ADMIN");
            var token = await GetAccessTokenAsync("admin_empty@example.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/locations/pending");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetPendingLocations_WithPageSize1_ReturnsSingleItem()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending?pageNumber=1&pageSize=1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(1);
            result.Data.TotalCount.Should().Be(5);
            result.Data.PageSize.Should().Be(1);
        }

        [Fact]
        public async Task GetPendingLocations_MultipleRequests_ReturnsConsistentData()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response1 = await _client.GetAsync("/api/locations/pending");
            var response2 = await _client.GetAsync("/api/locations/pending");

            // Assert
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);

            var result1 = await response1.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result2 = await response2.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result1!.Data!.TotalCount.Should().Be(result2!.Data!.TotalCount);
            result1.Data.Items.Count.Should().Be(result2.Data.Items.Count);
        }

        [Fact]
        public async Task GetPendingLocations_WithInvalidPageNumber_StillReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.GetAsync("/api/locations/pending?pageNumber=0&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetPendingLocations_AdminAndModeratorGetSameResults()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
            var adminResponse = await _client.GetAsync("/api/locations/pending");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _moderatorToken);
            var moderatorResponse = await _client.GetAsync("/api/locations/pending");

            // Assert
            adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            moderatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var adminResult = await adminResponse.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var moderatorResult = await moderatorResponse.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            adminResult!.Data!.TotalCount.Should().Be(moderatorResult!.Data!.TotalCount);
            adminResult.Data.Items.Count.Should().Be(moderatorResult.Data.Items.Count);
        }

        #endregion
    }
}

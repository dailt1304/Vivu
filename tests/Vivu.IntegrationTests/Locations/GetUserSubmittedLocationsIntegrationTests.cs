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
    [Collection("Integration Tests")]
    public class GetUserSubmittedLocationsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _user1 = null!;
        private User _user2 = null!;
        private string _user1Token = string.Empty;
        private string _user2Token = string.Empty;

        public GetUserSubmittedLocationsIntegrationTests(IntegrationTestWebAppFactory factory)
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
            
            _user1 = await SeedUserAsync("user1_submitted@example.com", "Password123!");
            _user2 = await SeedUserAsync("user2_submitted@example.com", "Password123!");

            await SeedUserSubmittedLocationsAsync(_user1);
            await SeedUserSubmittedLocationsAsync(_user2);

            _user1Token = await GetAccessTokenAsync("user1_submitted@example.com", "Password123!");
            _user2Token = await GetAccessTokenAsync("user2_submitted@example.com", "Password123!");
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

        private async Task<User> SeedUserAsync(string email, string password)
        {
            var role = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = "USER",
                RoleDescription = "User Role"
            };
            await _dbContext.Set<Role>().AddAsync(role);

            var hashedPassword = _passwordHasher.HashPassword(password);
            var user = User.Create(email, hashedPassword, "Test User");
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

        private async Task SeedUserSubmittedLocationsAsync(User user)
        {
            // Create 3 pending locations submitted by this user
            for (int i = 1; i <= 3; i++)
            {
                var location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = $"Submitted Location {i} by {user.Email}",
                    Description = $"User submitted location {i}",
                    Address = $"{i}45 Submitted Street",
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
                    UserId = user.Id,
                    ReportType = ReportType.NEW_LOCATION.ToString(),
                    ReportReason = $"Submitted location {i}",
                    Status = ReportStatus.PENDING.ToString(),
                    CreatedDate = DateTime.UtcNow.AddDays(-i)
                };

                await _dbContext.Set<LocationReport>().AddAsync(report);
            }

            // Create 2 approved locations submitted by this user
            for (int i = 1; i <= 2; i++)
            {
                var location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = $"Approved Submitted Location {i} by {user.Email}",
                    Description = $"User approved location {i}",
                    Address = $"{i}50 Approved Submitted Street",
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
                    UserId = user.Id,
                    ReportType = ReportType.NEW_LOCATION.ToString(),
                    ReportReason = $"Approved location {i}",
                    Status = ReportStatus.APPROVED.ToString(),
                    CreatedDate = DateTime.UtcNow.AddDays(-i)
                };

                await _dbContext.Set<LocationReport>().AddAsync(report);
            }

            // Create 1 rejected location submitted by this user
            var rejectedLocation = new Location
            {
                Id = Guid.NewGuid(),
                Name = $"Rejected Submitted Location by {user.Email}",
                Description = "User rejected location",
                Address = "555 Rejected Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = false,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

            await _dbContext.Set<Location>().AddAsync(rejectedLocation);

            var rejectedReport = new LocationReport
            {
                Id = Guid.NewGuid(),
                LocationId = rejectedLocation.Id,
                UserId = user.Id,
                ReportType = ReportType.NEW_LOCATION.ToString(),
                ReportReason = "Rejected location",
                Status = ReportStatus.REJECTED.ToString(),
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

            await _dbContext.Set<LocationReport>().AddAsync(rejectedReport);

            await _dbContext.SaveChangesAsync();
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task GetUserSubmittedLocations_AsAuthenticatedUser_ReturnsSuccess()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
            result.Data.TotalCount.Should().Be(6); // 3 pending + 2 approved + 1 rejected
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithDefaultPagination_ReturnsAllUserLocations()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(6);
            result.Data.TotalCount.Should().Be(6);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetUserSubmittedLocations_ReturnsOnlyCurrentUserLocations()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().AllSatisfy(loc =>
            {
                loc.Name.Should().Contain("user1_submitted@example.com");
            });
        }

        [Fact]
        public async Task GetUserSubmittedLocations_ReturnsCompleteLocationData()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var firstLocation = result!.Data!.Items.First();
            firstLocation.Id.Should().NotBeEmpty();
            firstLocation.Name.Should().NotBeNullOrEmpty();
            firstLocation.Description.Should().NotBeNullOrEmpty();
            firstLocation.Address.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetUserSubmittedLocations_DifferentUsersGetDifferentLocations()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);
            var user1Response = await _client.GetAsync("/api/locations/my-submissions");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user2Token);
            var user2Response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            user1Response.StatusCode.Should().Be(HttpStatusCode.OK);
            user2Response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user1Result = await user1Response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var user2Result = await user2Response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            user1Result!.Data!.Items.Should().AllSatisfy(loc =>
            {
                loc.Name.Should().Contain("user1_submitted@example.com");
            });

            user2Result!.Data!.Items.Should().AllSatisfy(loc =>
            {
                loc.Name.Should().Contain("user2_submitted@example.com");
            });
        }

        #endregion

        #region Status Filter Tests

        [Fact]
        public async Task GetUserSubmittedLocations_WithPendingStatusFilter_ReturnsOnlyPendingLocations()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions?status=1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(3);
            result.Data.Items.Should().AllSatisfy(loc =>
            {
                loc.Name.Should().Contain("Submitted Location");
            });
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithApprovedStatusFilter_ReturnsOnlyApprovedLocations()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions?status=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(2);
            result.Data.Items.Should().AllSatisfy(loc =>
            {
                loc.Name.Should().Contain("Approved Submitted Location");
                loc.IsVerified.Should().BeTrue();
            });
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithRejectedStatusFilter_ReturnsOnlyRejectedLocations()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions?status=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().Name.Should().Contain("Rejected Submitted Location");
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithoutStatusFilter_ReturnsAllStatuses()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(6); // 3 pending + 2 approved + 1 rejected
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task GetUserSubmittedLocations_WithCustomPageSize_ReturnsCorrectNumberOfItems()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions?pageNumber=1&pageSize=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(3);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(3);
            result.Data.TotalCount.Should().Be(6);
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithSecondPage_ReturnsRemainingItems()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions?pageNumber=2&pageSize=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(3);
            result.Data.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(3);
            result.Data.TotalCount.Should().Be(6);
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithPageNumberExceedingTotal_ReturnsEmptyItems()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions?pageNumber=10&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(6);
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithPageSize1_ReturnsSingleItem()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions?pageNumber=1&pageSize=1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(1);
            result.Data.TotalCount.Should().Be(6);
            result.Data.PageSize.Should().Be(1);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task GetUserSubmittedLocations_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token-xyz");

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithExpiredToken_ReturnsUnauthorized()
        {
            // Arrange
            var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTYyMzkwMjJ9.invalid";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Empty Result Tests

        [Fact]
        public async Task GetUserSubmittedLocations_WhenUserHasNoSubmittedLocations_ReturnsEmptyList()
        {
            // Arrange
            var newUser = await SeedUserAsync("empty_user@example.com", "Password123!");
            var newUserToken = await GetAccessTokenAsync("empty_user@example.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newUserToken);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetUserSubmittedLocations_WithStatusFilterButNoResults_ReturnsEmptyList()
        {
            // Arrange
            var newUser = await SeedUserAsync("filter_empty_user@example.com", "Password123!");
            var newUserToken = await GetAccessTokenAsync("filter_empty_user@example.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newUserToken);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions?status=2");

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
        public async Task GetUserSubmittedLocations_MultipleRequests_ReturnsConsistentData()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response1 = await _client.GetAsync("/api/locations/my-submissions");
            var response2 = await _client.GetAsync("/api/locations/my-submissions");

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
        public async Task GetUserSubmittedLocations_CombineStatusFilterAndPagination_ReturnsCorrectResults()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _user1Token);

            // Act
            var response = await _client.GetAsync("/api/locations/my-submissions?status=1&pageNumber=1&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TestPaginatedList<LocationDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result!.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(3); // 3 pending locations
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(2);
        }

        #endregion
    }
}

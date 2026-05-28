using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;

namespace Vivu.IntegrationTests.Trips
{
    // Test-specific DTOs for JSON deserialization
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

    public class TestUserTripsResponseDto
    {
        public TestPaginatedList<TripDto> Trips { get; set; } = null!;
        public int NumberOfTripCreated { get; set; }
        public int TripLimit { get; set; }
    }

    [Collection("Integration Tests")]
    public class GetUserTripsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthTokenProcess _tokenProcess;

        public GetUserTripsIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            _tokenProcess = _scope.ServiceProvider.GetRequiredService<IAuthTokenProcess>();
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
            await _dbContext.Set<TripDay>().ExecuteDeleteAsync();
            await _dbContext.Set<TripMember>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Helper Methods

        private async Task<(User user, string token)> SeedTestUserWithTokenAsync(
            string email = "testuser@example.com",
            string password = "Password123!",
            bool isPremium = false)
        {
            var user = await SeedTestUserAsync(email, password, isPremium);
            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToArray();
            var token = _tokenProcess.GenerateToken(user, roles);
            return (user, token);
        }

        private async Task<User> SeedTestUserAsync(
            string email,
            string password,
            bool isPremium = false)
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
                RoleName = isPremium ? Role.Names.Premium : "User",
                RoleDescription = isPremium ? "Premium User" : "Standard User"
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

        private async Task<Trip> SeedTripAsync(
            Guid userId,
            string title,
            string status = "planning",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var trip = Trip.Create(
                userId: userId,
                title: title,
                description: $"Description for {title}",
                startDate: startDate ?? DateTime.UtcNow.AddDays(1),
                endDate: endDate ?? DateTime.UtcNow.AddDays(5),
                isPublic: false
            );

            // Set status using reflection
            typeof(Trip).GetProperty("Status")!.SetValue(trip, status);

            _dbContext.Set<Trip>().Add(trip);

            // Add TripMember for owner
            var tripMember = TripMember.Create(
                tripId: trip.Id,
                userId: userId,
                ownerId: userId,
                role: "owner"
            );
            _dbContext.Set<TripMember>().Add(tripMember);

            await _dbContext.SaveChangesAsync();
            return trip;
        }

        private async Task<List<Trip>> SeedMultipleTripsAsync(
            Guid userId,
            int count,
            string status = "planning")
        {
            var trips = new List<Trip>();
            for (int i = 0; i < count; i++)
            {
                var trip = await SeedTripAsync(userId, $"Trip {i + 1}", status);
                trips.Add(trip);
            }
            return trips;
        }

        private void SetAuthorizationHeader(string token)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task GetUserTrips_WithValidUser_ReturnsOkWithTrips()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync();
            await SeedMultipleTripsAsync(user.Id, 3);
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            result.Data.Trips.Items.Should().HaveCount(3);
            result.Data.NumberOfTripCreated.Should().BeGreaterThanOrEqualTo(3);
        }

        [Fact]
        public async Task GetUserTrips_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("paging@example.com");
            await SeedMultipleTripsAsync(user.Id, 5);
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}?pageNumber=1&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Trips.Items.Should().HaveCount(2);
            result.Data.Trips.TotalCount.Should().Be(5);
            result.Data.Trips.PageNumber.Should().Be(1);
            result.Data.Trips.PageSize.Should().Be(2);
            result.Data.Trips.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task GetUserTrips_WithStatusFilter_ReturnsFilteredTrips()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("filter@example.com");
            await SeedTripAsync(user.Id, "Planning Trip 1", "planning");
            await SeedTripAsync(user.Id, "Planning Trip 2", "planning");
            await SeedTripAsync(user.Id, "Completed Trip 1", "completed");
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}?status=completed");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Trips.Items.Should().HaveCount(1);
            result.Data.Trips.Items.Should().OnlyContain(t => t.Status.ToLower() == "completed");
        }

        [Fact]
        public async Task GetUserTrips_WithNoTrips_ReturnsEmptyList()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("notrips@example.com");
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Trips.Items.Should().BeEmpty();
            result.Data.Trips.TotalCount.Should().Be(0);
        }

        #endregion

        #region Status Filter Tests

        [Theory]
        [InlineData("planning")]
        [InlineData("ongoing")]
        [InlineData("completed")]
        public async Task GetUserTrips_WithValidStatuses_ReturnsFilteredTrips(string status)
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync($"status{status}@example.com");
            await SeedTripAsync(user.Id, "Planning Trip", "planning");
            await SeedTripAsync(user.Id, "Ongoing Trip", "ongoing");
            await SeedTripAsync(user.Id, "Completed Trip", "completed");
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}?status={status}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Trips.Items.Should().HaveCount(1);
            result.Data.Trips.Items.First().Status.ToLower().Should().Be(status.ToLower());
        }

        [Fact]
        public async Task GetUserTrips_WithInvalidStatus_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("invalidstatus@example.com");
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}?status=invalid");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task GetUserTrips_WithCaseInsensitiveStatus_ReturnsFilteredTrips()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("caseinsensitive@example.com");
            await SeedTripAsync(user.Id, "Planning Trip", "planning");
            SetAuthorizationHeader(token);

            // Act - Use uppercase status
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}?status=PLANNING");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Trips.Items.Should().HaveCount(1);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task GetUserTrips_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUserTrips_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetAuthorizationHeader("invalid-token");

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task GetUserTrips_SecondPage_ReturnsCorrectItems()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("page2@example.com");
            await SeedMultipleTripsAsync(user.Id, 5);
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}?pageNumber=2&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Trips.Items.Should().HaveCount(2);
            result.Data.Trips.PageNumber.Should().Be(2);
            result.Data.Trips.HasPreviousPage.Should().BeTrue();
            result.Data.Trips.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserTrips_LastPage_HasNoNextPage()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("lastpage@example.com");
            await SeedMultipleTripsAsync(user.Id, 5);
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}?pageNumber=3&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Trips.Items.Should().HaveCount(1); // Only 1 item on last page
            result.Data.Trips.HasNextPage.Should().BeFalse();
            result.Data.Trips.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserTrips_DefaultPagination_UsesDefaultValues()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("default@example.com");
            await SeedMultipleTripsAsync(user.Id, 3);
            SetAuthorizationHeader(token);

            // Act - No pagination params
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Trips.PageNumber.Should().Be(1);
            result.Data.Trips.PageSize.Should().Be(10); // Default page size
        }

        #endregion

        #region Trip Limit Tests

        [Fact]
        public async Task GetUserTrips_RegularUser_ReturnsCorrectTripLimit()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("regular@example.com", isPremium: false);
            await SeedMultipleTripsAsync(user.Id, 2);
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.NumberOfTripCreated.Should().Be(2);
            result.Data.TripLimit.Should().BeGreaterThan(0);
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task GetUserTrips_ReturnsTripsOrderedByCreatedDateDescending()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("sorting@example.com");
            
            // Create trips with specific order
            var trip1 = await SeedTripAsync(user.Id, "First Trip");
            await Task.Delay(100); // Small delay to ensure different timestamps
            var trip2 = await SeedTripAsync(user.Id, "Second Trip");
            await Task.Delay(100);
            var trip3 = await SeedTripAsync(user.Id, "Third Trip");
            
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{user.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            var trips = result!.Data.Trips.Items;
            trips.Should().HaveCount(3);
            
            // Most recent should be first (descending order)
            trips[0].Title.Should().Be("Third Trip");
            trips[1].Title.Should().Be("Second Trip");
            trips[2].Title.Should().Be("First Trip");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetUserTrips_NonExistentUser_ReturnsEmptyList()
        {
            // Arrange
            var (_, token) = await SeedTestUserWithTokenAsync("existinguser@example.com");
            var nonExistentUserId = Guid.NewGuid();
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{nonExistentUserId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TestUserTripsResponseDto>>(content, options);

            result.Should().NotBeNull();
            result!.Data.Trips.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserTrips_WithEmptyGuid_ReturnsBadRequest()
        {
            // Arrange
            var (_, token) = await SeedTestUserWithTokenAsync("emptyguid@example.com");
            SetAuthorizationHeader(token);

            // Act
            var response = await _client.GetAsync($"/api/Trips/user/{Guid.Empty}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.Trips
{
    public class GetUserFavoriteTripsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private string _accessToken = string.Empty;
        private Guid _currentUserId;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GetUserFavoriteTripsIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
        }

        public async Task InitializeAsync()
        {
            await CleanupDatabaseAsync();

            var (userId, token) = await CreateAndAuthenticateTestUserAsync();
            _currentUserId = userId;
            _accessToken = token;

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task DisposeAsync()
        {
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"TripFavorites\"");
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"TripMembers\"");
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Trips\"");
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Users\"");
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Roles\"");
            await _dbContext.SaveChangesAsync();
        }

        #region GET /api/users/me/favorites — Success Scenarios

        [Fact]
        public async Task GetFavoriteTrips_WhenNoFavorites_ReturnsEmptyList()
        {
            // Act
            var response = await _client.GetAsync("/api/users/me/favorites");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteTripsPaginatedResponse>>(content, JsonOptions);
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetFavoriteTrips_WithFavoritedTrips_ReturnsCorrectList()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip1 = await SeedTripAsync(owner.Id, "Trip Alpha", isPublic: true);
            var trip2 = await SeedTripAsync(owner.Id, "Trip Beta", isPublic: true);

            await SeedFavoriteAsync(_currentUserId, trip1.Id);
            await SeedFavoriteAsync(_currentUserId, trip2.Id);

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteTripsPaginatedResponse>>(content, JsonOptions);
            apiResponse!.Data.TotalCount.Should().Be(2);
            apiResponse.Data.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetFavoriteTrips_ShouldReturnOnlyCurrentUserFavorites()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var otherUser = await SeedUserAsync("other@test.com");

            var trip1 = await SeedTripAsync(owner.Id, "My Favorite Trip", isPublic: true);
            var trip2 = await SeedTripAsync(owner.Id, "Other User Trip", isPublic: true);

            await SeedFavoriteAsync(_currentUserId, trip1.Id);
            await SeedFavoriteAsync(otherUser.Id, trip2.Id);

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteTripsPaginatedResponse>>(content, JsonOptions);
            apiResponse!.Data.TotalCount.Should().Be(1);
            apiResponse.Data.Items.Should().ContainSingle();
            apiResponse.Data.Items[0].Title.Should().Be("My Favorite Trip");
        }

        [Fact]
        public async Task GetFavoriteTrips_ResponseHasCorrectTripFields()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, "Detailed Trip", isPublic: true);
            await SeedFavoriteAsync(_currentUserId, trip.Id);

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteTripsPaginatedResponse>>(content, JsonOptions);
            var item = apiResponse!.Data.Items[0];

            item.Id.Should().NotBeEmpty();
            item.Title.Should().Be("Detailed Trip");
            item.IsPublic.Should().BeTrue();
            item.OwnerId.Should().Be(owner.Id);
        }

        #endregion

        #region GET /api/users/me/favorites — Pagination Tests

        [Fact]
        public async Task GetFavoriteTrips_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            for (int i = 1; i <= 5; i++)
            {
                var trip = await SeedTripAsync(owner.Id, $"Trip {i}", isPublic: true);
                await SeedFavoriteAsync(_currentUserId, trip.Id);
            }

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites?pageNumber=2&pageSize=2");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteTripsPaginatedResponse>>(content, JsonOptions);
            apiResponse!.Data.TotalCount.Should().Be(5);
            apiResponse.Data.Items.Should().HaveCount(2);
            apiResponse.Data.PageNumber.Should().Be(2);
            apiResponse.Data.PageSize.Should().Be(2);
        }

        [Fact]
        public async Task GetFavoriteTrips_FirstPage_HasCorrectMetadata()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            for (int i = 1; i <= 3; i++)
            {
                var trip = await SeedTripAsync(owner.Id, $"Trip {i}", isPublic: true);
                await SeedFavoriteAsync(_currentUserId, trip.Id);
            }

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites?pageNumber=1&pageSize=10");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteTripsPaginatedResponse>>(content, JsonOptions);
            apiResponse!.Data.PageNumber.Should().Be(1);
            apiResponse.Data.TotalCount.Should().Be(3);
            apiResponse.Data.HasPreviousPage.Should().BeFalse();
        }

        #endregion

        #region GET /api/users/me/favorites — Failure Scenarios

        [Fact]
        public async Task GetFavoriteTrips_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetFavoriteTrips_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GET /api/users/me/favorites — Data Integrity Tests

        [Fact]
        public async Task GetFavoriteTrips_ShouldNotReturnDeletedTrips()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var activeTrip = await SeedTripAsync(owner.Id, "Active Trip", isPublic: true);
            var deletedTrip = await SeedTripAsync(owner.Id, "Deleted Trip", isPublic: true);

            await SeedFavoriteAsync(_currentUserId, activeTrip.Id);
            await SeedFavoriteAsync(_currentUserId, deletedTrip.Id);

            // Soft delete the trip
            deletedTrip.Delete();
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteTripsPaginatedResponse>>(content, JsonOptions);
            apiResponse!.Data.TotalCount.Should().Be(1);
            apiResponse.Data.Items.Should().ContainSingle();
            apiResponse.Data.Items[0].Title.Should().Be("Active Trip");
        }

        [Fact]
        public async Task GetFavoriteTrips_ShouldNotReturnPrivateTrips()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var publicTrip = await SeedTripAsync(owner.Id, "Public Trip", isPublic: true);
            var privateTrip = await SeedTripAsync(owner.Id, "Private Trip", isPublic: false);

            await SeedFavoriteAsync(_currentUserId, publicTrip.Id);
            await SeedFavoriteAsync(_currentUserId, privateTrip.Id);

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteTripsPaginatedResponse>>(content, JsonOptions);
            apiResponse!.Data.TotalCount.Should().Be(1);
            apiResponse.Data.Items[0].Title.Should().Be("Public Trip");
        }

        [Fact]
        public async Task GetFavoriteTrips_AfterUnfavoriting_DoesNotReturnRemovedTrip()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, "Will Unfavorite", isPublic: true);
            await SeedFavoriteAsync(_currentUserId, trip.Id);

            // Unfavorite
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            await _client.DeleteAsync($"/api/trips/{trip.Id}/favorite");

            // Act
            var response = await _client.GetAsync("/api/users/me/favorites");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteTripsPaginatedResponse>>(content, JsonOptions);
            apiResponse!.Data.TotalCount.Should().Be(0);
            apiResponse.Data.Items.Should().BeEmpty();
        }

        #endregion

        #region Helper Methods

        private async Task<(Guid userId, string token)> CreateAndAuthenticateTestUserAsync()
        {
            var userId = Guid.NewGuid();
            var passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var user = User.Create(
                email: "testuser@example.com",
                passwordHash: passwordHasher.HashPassword("Password123!"),
                fullName: "Test User"
            );

            typeof(User).GetProperty("Id")!.SetValue(user, userId);
            user.VerifyEmail();

            var role = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = "User",
                RoleDescription = "Standard User"
            };

            user.AssignRole(role.Id);

            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            var tokenService = _scope.ServiceProvider.GetRequiredService<IAuthTokenProcess>();
            var token = tokenService.GenerateToken(user, new[] { "User" });

            return (userId, token);
        }

        private async Task<User> SeedUserAsync(string email)
        {
            var passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var user = User.Create(
                email: email,
                passwordHash: passwordHasher.HashPassword("Password123!"),
                fullName: "Seeded User"
            );

            user.VerifyEmail();

            var userRole = await _dbContext.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == "User");
            if (userRole != null)
                user.AssignRole(userRole.Id);

            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        private async Task<Trip> SeedTripAsync(Guid ownerId, string title = "Test Trip", bool isPublic = true)
        {
            var trip = Trip.Create(
                userId: ownerId,
                title: title,
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(7),
                endDate: DateTime.UtcNow.AddDays(10),
                isPublic: isPublic
            );

            _dbContext.Set<Trip>().Add(trip);
            await _dbContext.SaveChangesAsync();

            return trip;
        }

        private async Task<TripFavorite> SeedFavoriteAsync(Guid userId, Guid tripId)
        {
            var favorite = new TripFavorite
            {
                UserId = userId,
                TripId = tripId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Set<TripFavorite>().Add(favorite);
            await _dbContext.SaveChangesAsync();

            return favorite;
        }

        #endregion

        #region Response Models

        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; } = default!;
        }

        private class FavoriteTripsPaginatedResponse
        {
            [JsonPropertyName("items")]
            public List<FavoriteTripItem> Items { get; set; } = new();

            [JsonPropertyName("pageNumber")]
            public int PageNumber { get; set; }

            [JsonPropertyName("pageSize")]
            public int PageSize { get; set; }

            [JsonPropertyName("totalCount")]
            public int TotalCount { get; set; }

            [JsonPropertyName("totalPages")]
            public int TotalPages { get; set; }

            [JsonPropertyName("hasPreviousPage")]
            public bool HasPreviousPage { get; set; }

            [JsonPropertyName("hasNextPage")]
            public bool HasNextPage { get; set; }
        }

        private class FavoriteTripItem
        {
            [JsonPropertyName("id")]
            public Guid Id { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("isPublic")]
            public bool IsPublic { get; set; }

            [JsonPropertyName("ownerId")]
            public Guid OwnerId { get; set; }

            [JsonPropertyName("ownerName")]
            public string? OwnerName { get; set; }

            [JsonPropertyName("favoritedAt")]
            public DateTime? FavoritedAt { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;
        }

        #endregion
    }
}

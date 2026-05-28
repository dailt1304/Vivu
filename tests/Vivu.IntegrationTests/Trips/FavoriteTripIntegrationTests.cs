using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    public class FavoriteTripIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private string _accessToken = string.Empty;
        private Guid _currentUserId;

        public FavoriteTripIntegrationTests(IntegrationTestWebAppFactory factory)
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

        #region POST /api/trips/{tripId}/favorite — Success Scenarios

        [Fact]
        public async Task FavoriteTrip_WithValidPublicTrip_ReturnsOk()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);

            // Act
            var response = await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var favorite = await _dbContext.Set<TripFavorite>()
                .FirstOrDefaultAsync(f => f.TripId == trip.Id && f.UserId == _currentUserId);

            favorite.Should().NotBeNull();
            favorite!.UserId.Should().Be(_currentUserId);
            favorite.TripId.Should().Be(trip.Id);
        }

        [Fact]
        public async Task FavoriteTrip_Success_ShouldSetCreatedAtTimestamp()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);

            // Act
            var before = DateTime.UtcNow;
            await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);
            var after = DateTime.UtcNow;

            // Assert
            var favorite = await _dbContext.Set<TripFavorite>()
                .FirstOrDefaultAsync(f => f.TripId == trip.Id && f.UserId == _currentUserId);

            favorite.Should().NotBeNull();
            favorite!.CreatedAt.Should().BeOnOrAfter(before);
            favorite.CreatedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public async Task FavoriteTrip_SuccessResponse_HasCorrectStructure()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);

            // Act
            var response = await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("success");
        }

        #endregion

        #region POST /api/trips/{tripId}/favorite — Failure Scenarios

        [Fact]
        public async Task FavoriteTrip_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var favoriteCount = await _dbContext.Set<TripFavorite>().CountAsync(f => f.TripId == trip.Id);
            favoriteCount.Should().Be(0);
        }

        [Fact]
        public async Task FavoriteTrip_WithNonExistentTrip_ReturnsNotFound()
        {
            // Arrange
            var nonExistentTripId = Guid.NewGuid();

            // Act
            var response = await _client.PostAsync($"/api/trips/{nonExistentTripId}/favorite", null);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            content.Should().Contain("Trip.NotFound");
        }

        [Fact]
        public async Task FavoriteTrip_WithPrivateTrip_ReturnsBadRequest()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: false);

            // Act
            var response = await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Contain("Trip.NotPublic");

            var favoriteCount = await _dbContext.Set<TripFavorite>().CountAsync(f => f.TripId == trip.Id);
            favoriteCount.Should().Be(0);
        }

        [Fact]
        public async Task FavoriteTrip_WhenAlreadyFavorited_ReturnsConflict()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);

            // First favorite
            await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);

            // Act — second favorite
            var response = await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            content.Should().Contain("Trip.AlreadyFavorited");

            var favoriteCount = await _dbContext.Set<TripFavorite>()
                .CountAsync(f => f.TripId == trip.Id && f.UserId == _currentUserId);
            favoriteCount.Should().Be(1);
        }

        [Fact]
        public async Task FavoriteTrip_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region DELETE /api/trips/{tripId}/favorite — Success Scenarios

        [Fact]
        public async Task UnfavoriteTrip_WhenFavoriteExists_ReturnsOkAndRemovesFavorite()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);

            // Seed a favorite
            await SeedFavoriteAsync(_currentUserId, trip.Id);

            // Act
            var response = await _client.DeleteAsync($"/api/trips/{trip.Id}/favorite");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var favorite = await _dbContext.Set<TripFavorite>()
                .FirstOrDefaultAsync(f => f.TripId == trip.Id && f.UserId == _currentUserId);
            favorite.Should().BeNull();
        }

        [Fact]
        public async Task UnfavoriteTrip_Success_ShouldRemoveExactlyOneFavorite()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip1 = await SeedTripAsync(owner.Id, isPublic: true);
            var trip2 = await SeedTripAsync(owner.Id, isPublic: true);

            await SeedFavoriteAsync(_currentUserId, trip1.Id);
            await SeedFavoriteAsync(_currentUserId, trip2.Id);

            // Act
            await _client.DeleteAsync($"/api/trips/{trip1.Id}/favorite");

            // Assert
            var trip1Favorite = await _dbContext.Set<TripFavorite>()
                .FirstOrDefaultAsync(f => f.TripId == trip1.Id && f.UserId == _currentUserId);
            var trip2Favorite = await _dbContext.Set<TripFavorite>()
                .FirstOrDefaultAsync(f => f.TripId == trip2.Id && f.UserId == _currentUserId);

            trip1Favorite.Should().BeNull();
            trip2Favorite.Should().NotBeNull();
        }

        #endregion

        #region DELETE /api/trips/{tripId}/favorite — Failure Scenarios

        [Fact]
        public async Task UnfavoriteTrip_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);
            await SeedFavoriteAsync(_currentUserId, trip.Id);

            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.DeleteAsync($"/api/trips/{trip.Id}/favorite");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var favorite = await _dbContext.Set<TripFavorite>()
                .FirstOrDefaultAsync(f => f.TripId == trip.Id && f.UserId == _currentUserId);
            favorite.Should().NotBeNull();
        }

        [Fact]
        public async Task UnfavoriteTrip_WhenNotFavorited_ReturnsBadRequest()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);

            // Act
            var response = await _client.DeleteAsync($"/api/trips/{trip.Id}/favorite");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Contain("Trip.NotFavorited");
        }

        #endregion

        #region Database State Tests

        [Fact]
        public async Task FavoriteAndUnfavorite_Sequence_ShouldLeaveNoDatabaseRecord()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);

            // Act
            await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);
            await _client.DeleteAsync($"/api/trips/{trip.Id}/favorite");

            // Assert
            var favorite = await _dbContext.Set<TripFavorite>()
                .FirstOrDefaultAsync(f => f.TripId == trip.Id && f.UserId == _currentUserId);
            favorite.Should().BeNull();
        }

        [Fact]
        public async Task MultipleUsers_CanFavoriteSameTrip()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@test.com");
            var trip = await SeedTripAsync(owner.Id, isPublic: true);

            var user2 = await SeedUserAsync("user2@test.com");
            var user3 = await SeedUserAsync("user3@test.com");

            await SeedFavoriteAsync(user2.Id, trip.Id);
            await SeedFavoriteAsync(user3.Id, trip.Id);

            // Act — current user also favorites
            var response = await _client.PostAsync($"/api/trips/{trip.Id}/favorite", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var count = await _dbContext.Set<TripFavorite>().CountAsync(f => f.TripId == trip.Id);
            count.Should().Be(3);
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

        private async Task<Trip> SeedTripAsync(Guid ownerId, bool isPublic = true, string title = "Test Trip")
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
    }
}

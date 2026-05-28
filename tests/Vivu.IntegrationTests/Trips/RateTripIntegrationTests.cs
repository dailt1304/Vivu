using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.RateTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.Trips
{
    [Collection("Integration Tests")]
    public class RateTripIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthTokenProcess _tokenProcess;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public RateTripIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory      = factory;
            _client       = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            _scope        = _factory.Services.CreateScope();
            _dbContext    = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            _tokenProcess = _scope.ServiceProvider.GetRequiredService<IAuthTokenProcess>();
        }

        public async Task InitializeAsync() => await CleanupDatabaseAsync();

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<TripRating>().ExecuteDeleteAsync();
            await _dbContext.Set<TripMember>().ExecuteDeleteAsync();
            await _dbContext.Set<TripDay>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Helpers

        private async Task<(User user, string token)> SeedTestUserWithTokenAsync(string email = "ratetrip@example.com")
        {
            var user = User.Create(
                email: email,
                passwordHash: _passwordHasher.HashPassword("Password123!"),
                fullName: "Test User");
            user.IsEmailVerified = true;

            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, RoleName = "USER", RoleDescription = "Standard User" };
            user.UserRoles = new List<UserRole> { new() { UserId = user.Id, RoleId = roleId, Role = role } };

            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToArray();
            var token = _tokenProcess.GenerateToken(user, roles);
            return (user, token);
        }

        private async Task<Trip> SeedCompletedTripAsync(Guid ownerId, string status = "completed")
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<VivuDbContext>();

            var trip = Trip.Create(
                userId: ownerId,
                title: "Rating Test Trip",
                description: "A trip ready for rating",
                startDate: DateTime.UtcNow.AddDays(-10),
                endDate: DateTime.UtcNow.AddDays(-1));

            typeof(Trip).GetProperty("Status")!.SetValue(trip, status);

            ctx.Set<Trip>().Add(trip);

            var member = TripMember.Create(trip.Id, ownerId, ownerId, "owner");
            ctx.Set<TripMember>().Add(member);

            await ctx.SaveChangesAsync();
            return trip;
        }

        private void SetAuth(string token)
            => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        private void ClearAuth()
            => _client.DefaultRequestHeaders.Authorization = null;

        private async Task<T?> GetFreshAsync<T>(Func<VivuDbContext, Task<T?>> query) where T : class
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            return await query(ctx);
        }

        private record ApiResponse<T>(bool Success, T? Data, int StatusCode = 200,
            string? Message = null, string? Code = null);

        #endregion

        #region Unauthorized

        [Fact]
        public async Task RateTrip_WithNoToken_Returns401()
        {
            ClearAuth();
            var tripId = Guid.NewGuid();

            var response = await _client.PostAsJsonAsync($"/api/trips/{tripId}/rate",
                new RateTripCommand { TripId = tripId, Rating = 4 });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Validation

        [Fact]
        public async Task RateTrip_WithRatingZero_Returns422()
        {
            var (_, token) = await SeedTestUserWithTokenAsync();
            SetAuth(token);
            var tripId = Guid.NewGuid();

            var response = await _client.PostAsJsonAsync($"/api/trips/{tripId}/rate",
                new RateTripCommand { TripId = tripId, Rating = 0 });

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task RateTrip_WithRatingAboveFive_Returns422()
        {
            var (_, token) = await SeedTestUserWithTokenAsync();
            SetAuth(token);
            var tripId = Guid.NewGuid();

            var response = await _client.PostAsJsonAsync($"/api/trips/{tripId}/rate",
                new RateTripCommand { TripId = tripId, Rating = 6 });

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Trip Not Found

        [Fact]
        public async Task RateTrip_WithNonExistentTrip_Returns404()
        {
            var (_, token) = await SeedTestUserWithTokenAsync();
            SetAuth(token);
            var nonExistentId = Guid.NewGuid();

            var response = await _client.PostAsJsonAsync($"/api/trips/{nonExistentId}/rate",
                new RateTripCommand { TripId = nonExistentId, Rating = 4 });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Not Trip Owner

        [Fact]
        public async Task RateTrip_WhenUserIsNotTripOwner_Returns400WithCorrectCode()
        {
            var (ownerUser, _)     = await SeedTestUserWithTokenAsync("ratetrip_owner@example.com");
            var (_, notOwnerToken) = await SeedTestUserWithTokenAsync("ratetrip_notowner@example.com");

            var trip = await SeedCompletedTripAsync(ownerUser.Id);
            SetAuth(notOwnerToken);

            var response = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/rate",
                new RateTripCommand { TripId = trip.Id, Rating = 4 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.NotOwner.Code);
        }

        #endregion

        #region Trip Not Completed

        [Theory]
        [InlineData("planning")]
        [InlineData("ongoing")]
        public async Task RateTrip_WhenTripIsNotCompleted_Returns400WithCorrectCode(string status)
        {
            var (user, token) = await SeedTestUserWithTokenAsync($"ratetrip_{status}@example.com");
            var trip = await SeedCompletedTripAsync(user.Id, status);
            SetAuth(token);

            var response = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/rate",
                new RateTripCommand { TripId = trip.Id, Rating = 4 });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.NotCompleted.Code);
        }

        #endregion

        #region Already Rated

        [Fact]
        public async Task RateTrip_WhenTripAlreadyRated_Returns409WithCorrectCode()
        {
            var (user, token) = await SeedTestUserWithTokenAsync();
            var trip = await SeedCompletedTripAsync(user.Id);
            SetAuth(token);

            // First rating — should succeed
            await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/rate",
                new RateTripCommand { TripId = trip.Id, Rating = 4, ReviewContent = "Good trip" });

            // Second rating — should fail with 409
            var response = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/rate",
                new RateTripCommand { TripId = trip.Id, Rating = 5 });

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.AlreadyRated.Code);
        }

        #endregion

        #region Happy Path

        [Fact]
        public async Task RateTrip_WithValidRequest_Returns200()
        {
            var (user, token) = await SeedTestUserWithTokenAsync();
            var trip = await SeedCompletedTripAsync(user.Id);
            SetAuth(token);

            var response = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/rate",
                new RateTripCommand { TripId = trip.Id, Rating = 5, ReviewContent = "Excellent!" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOpts);
            apiResponse!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task RateTrip_WithValidRequest_PersistsRatingInDatabase()
        {
            var (user, token) = await SeedTestUserWithTokenAsync();
            var trip = await SeedCompletedTripAsync(user.Id);
            SetAuth(token);

            await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/rate",
                new RateTripCommand { TripId = trip.Id, Rating = 5, ReviewContent = "Excellent!" });

            var rating = await GetFreshAsync(ctx =>
                ctx.Set<TripRating>().AsNoTracking()
                   .FirstOrDefaultAsync(r => r.TripId == trip.Id));

            rating.Should().NotBeNull();
            rating!.TripId.Should().Be(trip.Id);
            rating.UserId.Should().Be(user.Id);
            rating.Rating.Should().Be(5);
            rating.ReviewContent.Should().Be("Excellent!");
        }

        [Fact]
        public async Task RateTrip_WithoutReviewContent_Returns200AndPersistsNullReview()
        {
            var (user, token) = await SeedTestUserWithTokenAsync();
            var trip = await SeedCompletedTripAsync(user.Id);
            SetAuth(token);

            var response = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/rate",
                new RateTripCommand { TripId = trip.Id, Rating = 3, ReviewContent = null });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var rating = await GetFreshAsync(ctx =>
                ctx.Set<TripRating>().AsNoTracking()
                   .FirstOrDefaultAsync(r => r.TripId == trip.Id));

            rating.Should().NotBeNull();
            rating!.ReviewContent.Should().BeNull();
        }

        #endregion
    }
}

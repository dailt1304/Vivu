using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.Trips
{
    [Collection("Integration Tests")]
    public class UpdateTripStatusIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _user = null!;
        private string _accessToken = string.Empty;

        private static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNameCaseInsensitive = true };

        public UpdateTripStatusIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client  = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            _scope         = _factory.Services.CreateScope();
            _dbContext      = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        }

        public async Task InitializeAsync()
        {
            await CleanupDatabaseAsync();
            _user = await SeedUserAsync("statusupdate@test.com", "Password123!");
            _accessToken = await GetAccessTokenAsync("statusupdate@test.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        // ── Cleanup ───────────────────────────────────────────────────────

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<TripMember>().ExecuteDeleteAsync();
            await _dbContext.Set<TripDay>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        // ── Seed Helpers ──────────────────────────────────────────────────

        private async Task<User> SeedUserAsync(string email, string password)
        {
            var user = User.Create(
                email: email,
                passwordHash: _passwordHasher.HashPassword(password),
                fullName: "Test User");
            user.IsEmailVerified = true;

            var role = new Role
            {
                Id              = Guid.NewGuid(),
                RoleName        = "USER",
                RoleDescription = "Standard User"
            };
            user.UserRoles = new List<UserRole>
            {
                new() { UserId = user.Id, RoleId = role.Id, Role = role }
            };

            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<Trip> SeedTripAsync(string status, DateTime? startDate, DateTime? endDate)
        {
            var trip = Trip.Create(
                userId: _user.Id,
                title: "Trip " + Guid.NewGuid().ToString("N")[..6],
                startDate: startDate,
                endDate: endDate);
            trip.Status = status;
            _dbContext.Set<Trip>().Add(trip);
            await _dbContext.SaveChangesAsync();
            return trip;
        }

        private async Task<string> GetFreshStatus(Guid tripId)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            var trip = await ctx.Set<Trip>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId);
            return trip!.Status;
        }

        private record ApiResponse<T>(bool Success, T? Data, int StatusCode = 200,
            string? Message = null, string? Code = null);

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var loginRequest = new LoginUserCommand
            {
                Email      = email,
                Password   = password,
                IpAddress  = "127.0.0.1",
                DeviceType = "Test",
                DeviceName = "Test Device"
            };
            var response    = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOpts);
            return apiResponse!.Data.AccessToken;
        }

        // ═════════════════════════════════════════════════════════════════
        // Tests
        // ═════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateTripStatuses_NoTripsToUpdate_Returns200WithZero()
        {
            // No trips seeded
            var response = await _client.PostAsync("/api/trips/update-statuses", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(JsonOpts);
            body!.Success.Should().BeTrue();
            body.Data.Should().Be(0);
        }

        [Fact]
        public async Task UpdateTripStatuses_PlanningTripStarted_UpdatesToOngoing()
        {
            var trip = await SeedTripAsync("planning",
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(5));

            var response = await _client.PostAsync("/api/trips/update-statuses", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(JsonOpts);
            body!.Data.Should().Be(1);

            var status = await GetFreshStatus(trip.Id);
            status.Should().Be("ongoing");
        }

        [Fact]
        public async Task UpdateTripStatuses_OngoingTripEnded_UpdatesToCompleted()
        {
            var trip = await SeedTripAsync("ongoing",
                startDate: DateTime.UtcNow.AddDays(-5),
                endDate: DateTime.UtcNow.AddDays(-1));

            var response = await _client.PostAsync("/api/trips/update-statuses", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(JsonOpts);
            body!.Data.Should().Be(1);

            var status = await GetFreshStatus(trip.Id);
            status.Should().Be("completed");
        }

        [Fact]
        public async Task UpdateTripStatuses_FuturePlanningTrip_StatusUnchanged()
        {
            var trip = await SeedTripAsync("planning",
                startDate: DateTime.UtcNow.AddDays(3),
                endDate: DateTime.UtcNow.AddDays(7));

            var response = await _client.PostAsync("/api/trips/update-statuses", null);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(JsonOpts);
            body!.Data.Should().Be(0);

            var status = await GetFreshStatus(trip.Id);
            status.Should().Be("planning");
        }

        [Fact]
        public async Task UpdateTripStatuses_MixedTrips_UpdatesOnlyEligible()
        {
            var shouldGoOngoing   = await SeedTripAsync("planning",
                startDate: DateTime.UtcNow.AddDays(-2),
                endDate: DateTime.UtcNow.AddDays(4));
            var shouldComplete    = await SeedTripAsync("ongoing",
                startDate: DateTime.UtcNow.AddDays(-6),
                endDate: DateTime.UtcNow.AddDays(-1));
            var shouldNotChange   = await SeedTripAsync("planning",
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(10));

            var response = await _client.PostAsync("/api/trips/update-statuses", null);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(JsonOpts);
            body!.Data.Should().Be(2);

            (await GetFreshStatus(shouldGoOngoing.Id)).Should().Be("ongoing");
            (await GetFreshStatus(shouldComplete.Id)).Should().Be("completed");
            (await GetFreshStatus(shouldNotChange.Id)).Should().Be("planning");
        }
    }
}

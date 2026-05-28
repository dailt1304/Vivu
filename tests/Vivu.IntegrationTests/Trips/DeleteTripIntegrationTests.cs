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
    public class DeleteTripIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _owner = null!;
        private User _otherUser = null!;
        private string _ownerToken  = string.Empty;
        private string _otherToken  = string.Empty;

        private static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNameCaseInsensitive = true };

        public DeleteTripIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _owner      = await SeedUserAsync("trip_owner@test.com", "Password123!");
            _otherUser  = await SeedUserAsync("trip_other@test.com", "Password123!");
            _ownerToken = await GetAccessTokenAsync("trip_owner@test.com", "Password123!");
            _otherToken = await GetAccessTokenAsync("trip_other@test.com", "Password123!");
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

        private async Task<Trip> SeedTripAsync(Guid userId)
        {
            var trip = Trip.Create(userId: userId, title: "Test Trip " + Guid.NewGuid().ToString("N")[..6]);
            _dbContext.Set<Trip>().Add(trip);
            await _dbContext.SaveChangesAsync();
            return trip;
        }

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

        private void SetAuth(string token)
            => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        private void ClearAuth()
            => _client.DefaultRequestHeaders.Authorization = null;

        private record ApiResponse<T>(bool Success, T? Data, int StatusCode = 200,
            string? Message = null, string? Code = null);

        // ═════════════════════════════════════════════════════════════════
        // Auth Guard
        // ═════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteTrip_WithoutToken_Returns401()
        {
            ClearAuth();
            var trip = await SeedTripAsync(_owner.Id);

            var response = await _client.DeleteAsync($"/api/trips/{trip.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ═════════════════════════════════════════════════════════════════
        // Failures
        // ═════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteTrip_TripNotFound_Returns404()
        {
            SetAuth(_ownerToken);
            var response = await _client.DeleteAsync($"/api/trips/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteTrip_NonOwnerDeletes_Returns403()
        {
            SetAuth(_otherToken);
            var trip = await SeedTripAsync(_owner.Id);

            var response = await _client.DeleteAsync($"/api/trips/{trip.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DeleteTrip_NonOwnerDeletes_TripStillExistsInDatabase()
        {
            SetAuth(_otherToken);
            var trip = await SeedTripAsync(_owner.Id);

            await _client.DeleteAsync($"/api/trips/{trip.Id}");

            var dbTrip = await _dbContext.Set<Trip>().FindAsync(trip.Id);
            dbTrip.Should().NotBeNull();
            dbTrip!.IsDeleted.Should().BeFalse();
        }

        // ═════════════════════════════════════════════════════════════════
        // Happy Path
        // ═════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteTrip_OwnerDeletes_Returns200()
        {
            SetAuth(_ownerToken);
            var trip = await SeedTripAsync(_owner.Id);

            var response = await _client.DeleteAsync($"/api/trips/{trip.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(JsonOpts);
            body!.Success.Should().BeTrue();
            body.Data.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteTrip_OwnerDeletes_SetsIsDeletedInDatabase()
        {
            SetAuth(_ownerToken);
            var trip = await SeedTripAsync(_owner.Id);

            await _client.DeleteAsync($"/api/trips/{trip.Id}");

            await using var scope = _factory.Services.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            var dbTrip = await ctx.Set<Trip>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == trip.Id);
            dbTrip!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteTrip_OwnerCanDeleteOwnTrip_MultipleTrips()
        {
            SetAuth(_ownerToken);
            var trip1 = await SeedTripAsync(_owner.Id);
            var trip2 = await SeedTripAsync(_owner.Id);

            var r1 = await _client.DeleteAsync($"/api/trips/{trip1.Id}");
            var r2 = await _client.DeleteAsync($"/api/trips/{trip2.Id}");

            r1.StatusCode.Should().Be(HttpStatusCode.OK);
            r2.StatusCode.Should().Be(HttpStatusCode.OK);

            await using var scope = _factory.Services.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            var t1 = await ctx.Set<Trip>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == trip1.Id);
            var t2 = await ctx.Set<Trip>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == trip2.Id);
            t1!.IsDeleted.Should().BeTrue();
            t2!.IsDeleted.Should().BeTrue();
        }
    }
}

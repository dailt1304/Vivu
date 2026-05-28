using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.Trips
{
    [Collection("Integration Tests")]
    public class GetTripByIdIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthTokenProcess _tokenProcess;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public GetTripByIdIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory        = factory;
            _client         = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            _scope          = _factory.Services.CreateScope();
            _dbContext       = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _passwordHasher  = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            _tokenProcess    = _scope.ServiceProvider.GetRequiredService<IAuthTokenProcess>();
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

        private async Task<(User user, string token)> SeedUserWithTokenAsync(string email = "gettrip@example.com")
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
            return (user, _tokenProcess.GenerateToken(user, roles));
        }

        private async Task<Trip> SeedTripWithOwnerAsync(Guid ownerId, string status = "planning")
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<VivuDbContext>();

            var trip = Trip.Create(
                userId: ownerId,
                title: "Detail Test Trip",
                description: "A trip for detail testing",
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(10));

            typeof(Trip).GetProperty("Status")!.SetValue(trip, status);
            ctx.Set<Trip>().Add(trip);

            ctx.Set<TripMember>().Add(TripMember.Create(trip.Id, ownerId, ownerId, "owner"));

            await ctx.SaveChangesAsync();
            return trip;
        }

        private async Task AddMemberToTripAsync(Guid tripId, Guid userId, Guid ownerId, string role = "member")
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            ctx.Set<TripMember>().Add(TripMember.Create(tripId, userId, ownerId, role));
            await ctx.SaveChangesAsync();
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
        public async Task GetTripById_WithNoToken_Returns401()
        {
            ClearAuth();

            var response = await _client.GetAsync($"/api/trips/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Trip Not Found

        [Fact]
        public async Task GetTripById_WithNonExistentTripId_Returns404()
        {
            var (_, token) = await SeedUserWithTokenAsync();
            SetAuth(token);

            var response = await _client.GetAsync($"/api/trips/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTripById_WithNonExistentTripId_ReturnsCorrectErrorCode()
        {
            var (_, token) = await SeedUserWithTokenAsync();
            SetAuth(token);

            var nonExistentId = Guid.NewGuid();
            var response = await _client.GetAsync($"/api/trips/{nonExistentId}");

            var content     = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.NotFoundById(nonExistentId).Code);
        }

        #endregion

        #region Access Denied

        [Fact]
        public async Task GetTripById_WhenUserIsNeitherOwnerNorMember_Returns403()
        {
            var (owner, _)         = await SeedUserWithTokenAsync("owner@gettrip.com");
            var (_, strangerToken) = await SeedUserWithTokenAsync("stranger@gettrip.com");

            var trip = await SeedTripWithOwnerAsync(owner.Id);
            SetAuth(strangerToken);

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetTripById_WhenUserIsNeitherOwnerNorMember_ReturnsCorrectErrorCode()
        {
            var (owner, _)         = await SeedUserWithTokenAsync("owner2@gettrip.com");
            var (_, strangerToken) = await SeedUserWithTokenAsync("stranger2@gettrip.com");

            var trip = await SeedTripWithOwnerAsync(owner.Id);
            SetAuth(strangerToken);

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            var content     = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.AccessDenied.Code);
        }

        #endregion

        #region Happy Path — Owner

        [Fact]
        public async Task GetTripById_WhenCalledByOwner_Returns200()
        {
            var (owner, token) = await SeedUserWithTokenAsync();
            var trip = await SeedTripWithOwnerAsync(owner.Id);
            SetAuth(token);

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetTripById_WhenCalledByOwner_ReturnsCorrectTripData()
        {
            var (owner, token) = await SeedUserWithTokenAsync();
            var trip = await SeedTripWithOwnerAsync(owner.Id);
            SetAuth(token);

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            var content     = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<DetailedTripDto>>(content, JsonOpts);

            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(trip.Id);
            apiResponse.Data.UserId.Should().Be(owner.Id);
            apiResponse.Data.Title.Should().Be("Detail Test Trip");
        }

        [Fact]
        public async Task GetTripById_WhenCalledByOwner_ReturnsMembers()
        {
            var (owner, token) = await SeedUserWithTokenAsync();
            var trip = await SeedTripWithOwnerAsync(owner.Id);
            SetAuth(token);

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            var content     = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<DetailedTripDto>>(content, JsonOpts);

            apiResponse!.Data!.Members.Should().NotBeEmpty();
            apiResponse.Data.MemberCount.Should().BeGreaterThanOrEqualTo(1);
        }

        #endregion

        #region Happy Path — Member

        [Fact]
        public async Task GetTripById_WhenCalledByMember_Returns200()
        {
            var (owner, _)        = await SeedUserWithTokenAsync("owner3@gettrip.com");
            var (member, memberToken) = await SeedUserWithTokenAsync("member@gettrip.com");

            var trip = await SeedTripWithOwnerAsync(owner.Id);
            await AddMemberToTripAsync(trip.Id, member.Id, owner.Id, "member");
            SetAuth(memberToken);

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetTripById_WhenCalledByMember_ReturnsTripDetails()
        {
            var (owner, _)            = await SeedUserWithTokenAsync("owner4@gettrip.com");
            var (member, memberToken) = await SeedUserWithTokenAsync("member2@gettrip.com");

            var trip = await SeedTripWithOwnerAsync(owner.Id);
            await AddMemberToTripAsync(trip.Id, member.Id, owner.Id, "editor");
            SetAuth(memberToken);

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            var content     = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<DetailedTripDto>>(content, JsonOpts);

            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.Id.Should().Be(trip.Id);
        }

        #endregion

        #region Response Shape

        [Fact]
        public async Task GetTripById_ReturnsDetailedTripDtoWithAllFields()
        {
            var (owner, token) = await SeedUserWithTokenAsync();
            var trip = await SeedTripWithOwnerAsync(owner.Id);
            SetAuth(token);

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            var content     = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<DetailedTripDto>>(content, JsonOpts);

            var data = apiResponse!.Data!;
            data.Id.Should().NotBeEmpty();
            data.Title.Should().NotBeNullOrEmpty();
            data.Status.Should().NotBeNullOrEmpty();
            data.Members.Should().NotBeNull();
            data.TripDays.Should().NotBeNull();
        }

        [Fact]
        public async Task GetTripById_ReturnsCorrectMemberCount()
        {
            var (owner, _)            = await SeedUserWithTokenAsync("owner5@gettrip.com");
            var (member1, _)          = await SeedUserWithTokenAsync("m1@gettrip.com");
            var (member2, ownerToken) = await SeedUserWithTokenAsync("m2@gettrip.com");

            var trip = await SeedTripWithOwnerAsync(owner.Id);
            await AddMemberToTripAsync(trip.Id, member1.Id, owner.Id, "member");
            await AddMemberToTripAsync(trip.Id, member2.Id, owner.Id, "member");
            SetAuth(ownerToken);

            // owner token won't work for this trip, seed token for owner
            var (_, ownerActualToken) = await SeedUserWithTokenAsync("owner5b@gettrip.com");
            // use owner's actual token
            var (ownerActual, realOwnerToken) = await SeedUserWithTokenAsync("owner5c@gettrip.com");
            var trip2 = await SeedTripWithOwnerAsync(ownerActual.Id);
            await AddMemberToTripAsync(trip2.Id, member1.Id, ownerActual.Id, "member");
            SetAuth(realOwnerToken);

            var response = await _client.GetAsync($"/api/trips/{trip2.Id}");

            var content     = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<DetailedTripDto>>(content, JsonOpts);

            // owner + 1 member
            apiResponse!.Data!.MemberCount.Should().Be(2);
        }

        #endregion
    }
}

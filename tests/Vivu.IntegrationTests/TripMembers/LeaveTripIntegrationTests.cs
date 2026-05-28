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
using TripMemberEntity = Vivu.Domain.Entities.TripMember;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.TripMembers
{
    [Collection("Integration Tests")]
    public class LeaveTripIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _testUser = null!;
        private string _accessToken = string.Empty;

        public LeaveTripIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedTestUserAsync("leavetrip@example.com", "Password123!");
            _accessToken = await GetAccessTokenAsync("leavetrip@example.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<TripMemberEntity>().ExecuteDeleteAsync();
            await _dbContext.Set<TripDay>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Success Scenarios

        [Fact]
        public async Task LeaveTrip_AsMember_ReturnsSuccess()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(jsonString, options);

            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().BeTrue();
        }

        [Fact]
        public async Task LeaveTrip_AsEditor_ReturnsSuccess()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "editor");

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task LeaveTrip_Success_RemovesMemberFromDatabase()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");

            var initialCount = await _dbContext.Set<TripMemberEntity>()
                .CountAsync(tm => tm.TripId == trip.Id && tm.UserId == _testUser.Id);

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var finalCount = await _dbContext.Set<TripMemberEntity>()
                .CountAsync(tm => tm.TripId == trip.Id && tm.UserId == _testUser.Id);

            initialCount.Should().Be(1);
            finalCount.Should().Be(0);
        }

        [Fact]
        public async Task LeaveTrip_Success_DoesNotAffectOtherMembers()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            var otherMember = await SeedUserAsync("other@example.com");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");
            await SeedTripMemberAsync(trip.Id, otherMember.Id, owner.Id, "editor");

            var initialTotalCount = await _dbContext.Set<TripMemberEntity>()
                .CountAsync(tm => tm.TripId == trip.Id);

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var finalTotalCount = await _dbContext.Set<TripMemberEntity>()
                .CountAsync(tm => tm.TripId == trip.Id);

            initialTotalCount.Should().Be(2);
            finalTotalCount.Should().Be(1);

            var otherMemberStillExists = await _dbContext.Set<TripMemberEntity>()
                .AnyAsync(tm => tm.TripId == trip.Id && tm.UserId == otherMember.Id);
            otherMemberStillExists.Should().BeTrue();
        }

        #endregion

        #region Authentication Failures

        [Fact]
        public async Task LeaveTrip_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            var client = _factory.CreateClient();

            // Act
            var response = await client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task LeaveTrip_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Validation Failures

        [Fact]
        public async Task LeaveTrip_TripNotFound_ReturnsNotFound()
        {
            // Arrange
            var nonExistentTripId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{nonExistentTripId}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task LeaveTrip_DeletedTrip_ReturnsNotFound()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Deleted Trip");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");
            
            trip.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            // Note: Deleted trip returns 404 (Trip.IsDelete error code doesn't match BadRequest patterns)
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task LeaveTrip_AsOwner_ReturnsBadRequest()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "My Trip");

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("TripMember.OwnerCannotLeave");
        }

        [Fact]
        public async Task LeaveTrip_NotAMember_ReturnsNotFound()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            // Don't add current user as a member

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task LeaveTrip_InvalidTripId_ReturnsNotFound()
        {
            // Act
            // Note: Invalid GUID format doesn't match route constraint {tripId:guid}, so returns 404
            var response = await _client.DeleteAsync("/api/tripmembers/invalid-guid/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task LeaveTrip_LastNonOwnerMember_ReturnsSuccess()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var memberCount = await _dbContext.Set<TripMemberEntity>()
                .CountAsync(tm => tm.TripId == trip.Id);
            memberCount.Should().Be(0);
        }

        [Fact]
        public async Task LeaveTrip_ConcurrentRequests_OnlyOneSucceeds()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");

            // Act - Send 3 concurrent requests
            var tasks = Enumerable.Range(0, 3)
                .Select(_ => _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave"))
                .ToArray();

            var responses = await Task.WhenAll(tasks);

            // Assert
            var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
            var notFoundCount = responses.Count(r => r.StatusCode == HttpStatusCode.NotFound);

            successCount.Should().Be(1);
            notFoundCount.Should().Be(2);

            // Verify member is removed
            var memberExists = await _dbContext.Set<TripMemberEntity>()
                .AnyAsync(tm => tm.TripId == trip.Id && tm.UserId == _testUser.Id);
            memberExists.Should().BeFalse();
        }

        [Fact]
        public async Task LeaveTrip_AfterLeaving_CannotAccessMembers()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");

            // Act - Leave the trip
            var leaveResponse = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");
            leaveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Try to access members after leaving
            var getMembersResponse = await _client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            getMembersResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region Database Verification Tests

        [Fact]
        public async Task LeaveTrip_Success_DecrementsMemberCount()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            var member1 = await SeedUserAsync("member1@example.com");
            var member2 = await SeedUserAsync("member2@example.com");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");
            await SeedTripMemberAsync(trip.Id, member1.Id, owner.Id, "viewer");
            await SeedTripMemberAsync(trip.Id, member2.Id, owner.Id, "editor");

            var initialCount = await _dbContext.Set<TripMemberEntity>()
                .CountAsync(tm => tm.TripId == trip.Id);

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var finalCount = await _dbContext.Set<TripMemberEntity>()
                .CountAsync(tm => tm.TripId == trip.Id);

            initialCount.Should().Be(3);
            finalCount.Should().Be(2);
        }

        [Fact]
        public async Task LeaveTrip_Success_TripStillExists()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");

            // Act
            var response = await _client.DeleteAsync($"/api/tripmembers/{trip.Id}/leave");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var tripStillExists = await _dbContext.Set<Trip>()
                .AnyAsync(t => t.Id == trip.Id && !t.IsDeleted);
            tripStillExists.Should().BeTrue();
        }

        #endregion

        #region Helper Methods

        private async Task<User> SeedTestUserAsync(string email, string password)
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
                RoleName = "User",
                RoleDescription = "Standard User"
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

        private async Task<User> SeedUserAsync(string email)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword("Password123!"),
                fullName: $"User {email}",
                avatarUrl: null
            );

            user.IsEmailVerified = true;

            var userRole = await _dbContext.Set<Role>()
                .FirstOrDefaultAsync(r => r.RoleName == "User");

            if (userRole != null)
            {
                user.AssignRole(userRole.Id);
            }

            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        private async Task<Trip> SeedTripAsync(Guid ownerId, string title)
        {
            var trip = Trip.Create(
                userId: ownerId,
                title: title,
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(7),
                endDate: DateTime.UtcNow.AddDays(14),
                isPublic: false,
                inviteCode: null
            );

            _dbContext.Set<Trip>().Add(trip);
            await _dbContext.SaveChangesAsync();

            return trip;
        }

        private async Task<TripMemberEntity> SeedTripMemberAsync(Guid tripId, Guid userId, Guid ownerId, string role)
        {
            var tripMember = TripMemberEntity.Create(tripId, userId, ownerId, role);

            _dbContext.Set<TripMemberEntity>().Add(tripMember);
            await _dbContext.SaveChangesAsync();

            return tripMember;
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
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

            return apiResponse!.Data.AccessToken;
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; } = default!;
            public string Message { get; set; } = string.Empty;
        }

        #endregion
    }
}

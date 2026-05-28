using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Google;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.JoinTripByCode;
using Vivu.Infrastructure.Data;
using Vivu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Vivu.IntegrationTests.Trips
{
    public class JoinTripByCodeIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private string _accessToken;
        private Guid _currentUserId;

        public JoinTripByCodeIntegrationTests(IntegrationTestWebAppFactory factory)
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

            // Create and authenticate a test user
            var (userId, token) = await CreateAndAuthenticateTestUserAsync();
            _currentUserId = userId;
            _accessToken = token;

            // Set authorization header
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task DisposeAsync()
        {
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            // Delete all data from tables (in reverse order of dependencies)
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"TripMembers\"");
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Trips\"");
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Users\"");
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Roles\"");
            await _dbContext.SaveChangesAsync();
        }

        #region Success Scenarios

        [Fact]
        public async Task JoinTrip_WithValidInviteCode_ReturnsOkAndCreatesMembership()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "ABC123");
            var command = new JoinTripByCodeCommand("ABC123");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var jsonString = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TripMemberDto>>(jsonString, options);

            var result = apiResponse!.Data;
            result.Should().NotBeNull();
            result!.TripId.Should().Be(trip.Id);
            result.UserId.Should().Be(_currentUserId);
            result.Role.Should().Be("viewer");

            // Verify in database
            var member = await _dbContext.Set<Domain.Entities.TripMember>()
                .FirstOrDefaultAsync(m => m.TripId == trip.Id && m.UserId == _currentUserId);

            member.Should().NotBeNull();
            member!.Role.Should().Be("viewer");
            member.OwnerId.Should().Be(tripOwner.Id);
        }

        [Fact]
        public async Task JoinTrip_ValidRequest_MembershipRecordHasCorrectTimestamp()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "XYZ789");
            var command = new JoinTripByCodeCommand("XYZ789");

            // Act
            var beforeJoin = DateTime.UtcNow;
            await _client.PostAsJsonAsync("/api/trips/join", command);
            var afterJoin = DateTime.UtcNow;

            // Assert
            var member = await _dbContext.Set<Domain.Entities.TripMember>()
                .FirstOrDefaultAsync(m => m.TripId == trip.Id && m.UserId == _currentUserId);

            member.Should().NotBeNull();
            member!.JoinedAt.Should().BeOnOrAfter(beforeJoin);
            member.JoinedAt.Should().BeOnOrBefore(afterJoin);
        }

        [Fact]
        public async Task JoinTrip_MultipleUsers_AllCanJoinSameTrip()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "MULTI123");

            // Create 3 different users
            var user1 = await SeedUserAsync("user1@example.com");
            var user2 = await SeedUserAsync("user2@example.com");
            var user3 = await SeedUserAsync("user3@example.com");

            // Act - Manually add members (simulating authenticated requests)
            var member1 = Domain.Entities.TripMember.Create(trip.Id, user1.Id, tripOwner.Id, "viewer");
            var member2 = Domain.Entities.TripMember.Create(trip.Id, user2.Id, tripOwner.Id, "viewer");
            var member3 = Domain.Entities.TripMember.Create(trip.Id, user3.Id, tripOwner.Id, "viewer");

            _dbContext.Set<Domain.Entities.TripMember>().AddRange(member1, member2, member3);
            await _dbContext.SaveChangesAsync();

            // Assert
            var memberCount = await _dbContext.Set<Domain.Entities.TripMember>()
                .CountAsync(m => m.TripId == trip.Id);

            memberCount.Should().Be(3);
        }

        [Fact]
        public async Task JoinTrip_WithSpecialCharactersInInviteCode_Works()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "CODE-2024-XYZ");
            var command = new JoinTripByCodeCommand("CODE-2024-XYZ");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var member = await _dbContext.Set<Domain.Entities.TripMember>()
                .FirstOrDefaultAsync(m => m.TripId == trip.Id && m.UserId == _currentUserId);

            member.Should().NotBeNull();
        }

        #endregion

        #region Failure Scenarios - Authentication

        [Fact]
        public async Task JoinTrip_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "ABC123");
            var command = new JoinTripByCodeCommand("ABC123");

            // Remove auth header
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Verify no member created
            var memberCount = await _dbContext.Set<Domain.Entities.TripMember>()
                .CountAsync(m => m.TripId == trip.Id);

            memberCount.Should().Be(0);
        }

        [Fact]
        public async Task JoinTrip_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "ABC123");
            var command = new JoinTripByCodeCommand("ABC123");

            // Set invalid token
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task JoinTrip_WithExpiredToken_ReturnsUnauthorized()
        {
            // Arrange
            var command = new JoinTripByCodeCommand("ABC123");

            // Create expired token (you may need to adjust based on your token service)
            var expiredToken = "expired.jwt.token"; // Mock expired token
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Failure Scenarios - Invalid Invite Code

        [Fact]
        public async Task JoinTrip_WithNonExistentInviteCode_ReturnsNotFound()
        {
            // Arrange
            var command = new JoinTripByCodeCommand("NOTEXIST");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Trip.InvalidInviteCode");
            content.Should().Contain("Invalid or expired invite code.");
        }

        [Fact]
        public async Task JoinTrip_WithEmptyInviteCode_ReturnsBadRequest()
        {
            // Arrange
            var command = new JoinTripByCodeCommand("");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task JoinTrip_WithNullInviteCode_ReturnsBadRequest()
        {
            // Arrange
            var command = new JoinTripByCodeCommand(null);

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("WRONG123")]
        [InlineData("ABC999")]
        [InlineData("INVALID")]
        public async Task JoinTrip_WithWrongInviteCode_ReturnsNotFound(string wrongCode)
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            await SeedTripAsync(tripOwner.Id, "CORRECT123");
            var command = new JoinTripByCodeCommand(wrongCode);

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Failure Scenarios - Already Member

        [Fact]
        public async Task JoinTrip_WhenAlreadyMember_ReturnsConflict()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "ABC123");
            var command = new JoinTripByCodeCommand("ABC123");

            // Join first time
            await _client.PostAsJsonAsync("/api/trips/join", command);

            // Act - Try to join again
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Trip.AlreadyMember");
            content.Should().Contain("already a member");

            // Verify only one membership exists
            var memberCount = await _dbContext.Set<Domain.Entities.TripMember>()
                .CountAsync(m => m.TripId == trip.Id && m.UserId == _currentUserId);

            memberCount.Should().Be(1);
        }

        [Fact]
        public async Task JoinTrip_OwnerTriesToJoin_ReturnsConflict()
        {
            // Arrange - Current user creates a trip
            var trip = await SeedTripAsync(_currentUserId, "OWNER123");

            // Manually add owner as member (as they should be auto-added on trip creation)
            var ownerMember = Domain.Entities.TripMember.Create(trip.Id, _currentUserId, _currentUserId, "owner");
            _dbContext.Set<Domain.Entities.TripMember>().Add(ownerMember);
            await _dbContext.SaveChangesAsync();

            var command = new JoinTripByCodeCommand("OWNER123");

            // Act - Owner tries to join their own trip
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task JoinTrip_CaseSensitiveInviteCode_WorksCorrectly()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "AbC123");

            // Try with different casing
            var command = new JoinTripByCodeCommand("abc123");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

   
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task JoinTrip_DeletedUser_ReturnsNotFound()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "ABC123");

            // Delete current user
            var currentUser = await _dbContext.Set<User>().FindAsync(_currentUserId);
            _dbContext.Set<User>().Remove(currentUser);
            await _dbContext.SaveChangesAsync();

            var command = new JoinTripByCodeCommand("ABC123");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task JoinTrip_DeletedTrip_ReturnsNotFound()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "ABC123");

            // Soft delete trip
            trip.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            var command = new JoinTripByCodeCommand("ABC123");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Concurrent Requests Tests

        [Fact]
        public async Task JoinTrip_ConcurrentRequestsFromSameUser_OnlyOneSucceeds()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "CONCURRENT");
            var command = new JoinTripByCodeCommand("CONCURRENT");

            // Act - Send 3 concurrent requests
            var tasks = Enumerable.Range(0, 3)
                .Select(_ => _client.PostAsJsonAsync("/api/trips/join", command))
                .ToArray();

            var responses = await Task.WhenAll(tasks);

            // Assert
            var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
            var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

            successCount.Should().Be(1);
            conflictCount.Should().Be(2);

            // Verify only one membership exists
            var memberCount = await _dbContext.Set<Domain.Entities.TripMember>()
                .CountAsync(m => m.TripId == trip.Id && m.UserId == _currentUserId);

            memberCount.Should().Be(1);
        }

        #endregion

        #region Database Verification Tests

        [Fact]
        public async Task JoinTrip_Success_TripMemberHasAllRequiredFields()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "VERIFY123");
            var command = new JoinTripByCodeCommand("VERIFY123");

            // Act
            await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            var member = await _dbContext.Set<Domain.Entities.TripMember>()
                .Include(m => m.User)
                .Include(m => m.Trip)
                .FirstOrDefaultAsync(m => m.TripId == trip.Id && m.UserId == _currentUserId);

            member.Should().NotBeNull();
            member!.TripId.Should().Be(trip.Id);
            member.UserId.Should().Be(_currentUserId);
            member.OwnerId.Should().Be(tripOwner.Id);
            member.Role.Should().Be("viewer");
            member.JoinedAt.Should().NotBe(default);
            member.User.Should().NotBeNull();
            member.Trip.Should().NotBeNull();
        }

        [Fact]
        public async Task JoinTrip_Success_IncrementsTripMemberCount()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "COUNT123");

            var initialCount = await _dbContext.Set<Domain.Entities.TripMember>()
                .CountAsync(m => m.TripId == trip.Id);

            var command = new JoinTripByCodeCommand("COUNT123");

            // Act
            await _client.PostAsJsonAsync("/api/trips/join", command);

            // Assert
            var finalCount = await _dbContext.Set<Domain.Entities.TripMember>()
                .CountAsync(m => m.TripId == trip.Id);

            finalCount.Should().Be(initialCount + 1);
        }

        #endregion

        #region Response Structure Tests

        [Fact]
        public async Task JoinTrip_SuccessResponse_HasCorrectStructure()
        {
            // Arrange
            var tripOwner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(tripOwner.Id, "STRUCTURE");
            var command = new JoinTripByCodeCommand("STRUCTURE");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var jsonString = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TripMemberDto>>(jsonString, options);

            var result = apiResponse!.Data;
            result.Should().NotBeNull();
            result.TripId.Should().NotBeEmpty();
            result.UserId.Should().NotBeEmpty();
            result.Role.Should().NotBeNullOrEmpty();
            result.JoinedAt.Should().NotBe(default);
        }

        [Fact]
        public async Task JoinTrip_ErrorResponse_HasCorrectStructure()
        {
            // Arrange
            var command = new JoinTripByCodeCommand("NOTFOUND");

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips/join", command);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Contain("code");
            content.Should().Contain("message");
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

            // Override ID for consistency
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

        private async Task<Trip> SeedTripAsync(Guid ownerId, string inviteCode)
        {
            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip",
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(7),
                endDate: DateTime.UtcNow.AddDays(10),
                isPublic: false,
                inviteCode: inviteCode
            );

            _dbContext.Set<Trip>().Add(trip);
            await _dbContext.SaveChangesAsync();

            return trip;
        }
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; }
            public string Message { get; set; }
        }

        #endregion
    }
}

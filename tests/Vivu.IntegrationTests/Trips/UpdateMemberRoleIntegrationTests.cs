using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.Trips.Commands.UpdateMemberRole;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Xunit;
using TripMemberEntity = Vivu.Domain.Entities.TripMember;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.Trips
{
    [Collection("Integration Tests")]
    public class UpdateMemberRoleIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _ownerUser = null!;
        private User _memberUser = null!;
        private string _ownerAccessToken = string.Empty;
        private string _memberAccessToken = string.Empty;

        public UpdateMemberRoleIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _ownerUser = await SeedTestUserAsync("owner@example.com", "Password123!", "Trip Owner");
            _memberUser = await SeedTestUserAsync("member@example.com", "Password123!", "Trip Member");
            _ownerAccessToken = await GetAccessTokenAsync("owner@example.com", "Password123!");
            _memberAccessToken = await GetAccessTokenAsync("member@example.com", "Password123!");
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
            await _dbContext.Set<UserRoleEntity>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Happy Path Tests

        [Fact]
        public async Task UpdateMemberRole_FromViewerToEditor_ReturnsSuccess()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");
            await SeedTripMemberAsync(trip.Id, _memberUser.Id, _ownerUser.Id, "viewer");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<TestApiResponse<TripMemberDto>>(jsonString, options);

            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            apiResponse.Data.Role.Should().Be("editor");
            apiResponse.Data.UserId.Should().Be(_memberUser.Id);
            apiResponse.Data.TripId.Should().Be(trip.Id);
        }

        [Fact]
        public async Task UpdateMemberRole_FromEditorToViewer_ReturnsSuccess()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");
            await SeedTripMemberAsync(trip.Id, _memberUser.Id, _ownerUser.Id, "editor");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Viewer
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<TestApiResponse<TripMemberDto>>();
            apiResponse!.Data.Role.Should().Be("viewer");
        }

        [Fact]
        public async Task UpdateMemberRole_Success_UpdatesDatabaseCorrectly()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");
            await SeedTripMemberAsync(trip.Id, _memberUser.Id, _ownerUser.Id, "viewer");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Clear change tracker to ensure fresh data from database
            _dbContext.ChangeTracker.Clear();

            var memberInDb = await _dbContext.Set<TripMemberEntity>()
                .FirstOrDefaultAsync(tm => tm.TripId == trip.Id && tm.UserId == _memberUser.Id);

            memberInDb.Should().NotBeNull();
            memberInDb!.Role.Should().Be("editor");
        }

        [Fact]
        public async Task UpdateMemberRole_ToSameRole_ReturnsSuccess()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");
            await SeedTripMemberAsync(trip.Id, _memberUser.Id, _ownerUser.Id, "editor");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var apiResponse = await response.Content.ReadFromJsonAsync<TestApiResponse<TripMemberDto>>();
            apiResponse!.Data.Role.Should().Be("editor");
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task UpdateMemberRole_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _memberUser.Id, _ownerUser.Id, "viewer");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            var client = _factory.CreateClient();

            // Act
            var response = await client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateMemberRole_AsNonOwner_ReturnsForbid()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");
            await SeedTripMemberAsync(trip.Id, _memberUser.Id, _ownerUser.Id, "viewer");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _memberAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Verify role was not changed
            var memberInDb = await _dbContext.Set<TripMemberEntity>()
                .FirstOrDefaultAsync(tm => tm.TripId == trip.Id && tm.UserId == _memberUser.Id);
            memberInDb!.Role.Should().Be("viewer");
        }

        [Fact]
        public async Task UpdateMemberRole_ToOwnerRole_ReturnsBadRequest()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");
            await SeedTripMemberAsync(trip.Id, _memberUser.Id, _ownerUser.Id, "viewer");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Owner
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<TestApiResponse<TripMemberDto>>(jsonString, options);

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeFalse();
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task UpdateMemberRole_WithNonExistentTrip_ReturnsNotFound()
        {
            // Arrange
            var nonExistentTripId = Guid.NewGuid();
            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{nonExistentTripId}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateMemberRole_WithNonExistentMember_ReturnsNotFound()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");
            var nonExistentUserId = Guid.NewGuid();

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{nonExistentUserId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<TestApiResponse<TripMemberDto>>(jsonString, options);

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task UpdateMemberRole_WithEmptyTripId_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{Guid.Empty}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateMemberRole_WithEmptyMemberUserId_ReturnsUnprocessableEntity()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{Guid.Empty}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Multiple Members Tests

        [Fact]
        public async Task UpdateMemberRole_WithMultipleMembers_OnlyUpdatesSpecifiedMember()
        {
            // Arrange
            var otherMember = await SeedTestUserAsync("other@example.com", "Password123!", "Other Member");
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");
            await SeedTripMemberAsync(trip.Id, _memberUser.Id, _ownerUser.Id, "viewer");
            await SeedTripMemberAsync(trip.Id, otherMember.Id, _ownerUser.Id, "viewer");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Clear change tracker to ensure fresh data from database
            _dbContext.ChangeTracker.Clear();

            var updatedMember = await _dbContext.Set<TripMemberEntity>()
                .FirstOrDefaultAsync(tm => tm.TripId == trip.Id && tm.UserId == _memberUser.Id);
            updatedMember!.Role.Should().Be("editor");

            var otherMemberInDb = await _dbContext.Set<TripMemberEntity>()
                .FirstOrDefaultAsync(tm => tm.TripId == trip.Id && tm.UserId == otherMember.Id);
            otherMemberInDb!.Role.Should().Be("viewer");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task UpdateMemberRole_OwnerUpdatingSelfToEditor_ReturnsSuccess()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");

            var command = new UpdateMemberRoleCommand
            {
                NewRole = TripRole.Editor
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_ownerUser.Id}", command);

            // Assert - Owner can update their own role
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Clear change tracker to ensure fresh data from database
            _dbContext.ChangeTracker.Clear();

            var memberInDb = await _dbContext.Set<TripMemberEntity>()
                .FirstOrDefaultAsync(tm => tm.TripId == trip.Id && tm.UserId == _ownerUser.Id);
            memberInDb!.Role.Should().Be("editor");
        }

        [Fact]
        public async Task UpdateMemberRole_RapidRoleChanges_HandlesCorrectly()
        {
            // Arrange
            var trip = await SeedTripAsync(_ownerUser.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _ownerUser.Id, _ownerUser.Id, "owner");
            await SeedTripMemberAsync(trip.Id, _memberUser.Id, _ownerUser.Id, "viewer");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ownerAccessToken);

            // Act - Change to Editor
            var command1 = new UpdateMemberRoleCommand { NewRole = TripRole.Editor };
            var response1 = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command1);

            // Act - Change back to Viewer
            var command2 = new UpdateMemberRoleCommand { NewRole = TripRole.Viewer };
            var response2 = await _client.PatchAsJsonAsync($"/api/trips/{trip.Id}/members/{_memberUser.Id}", command2);

            // Assert
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);

            var memberInDb = await _dbContext.Set<TripMemberEntity>()
                .FirstOrDefaultAsync(tm => tm.TripId == trip.Id && tm.UserId == _memberUser.Id);
            memberInDb!.Role.Should().Be("viewer");
        }

        #endregion

        #region Helper Methods

        private async Task<User> SeedTestUserAsync(string email, string password, string fullName)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword(password),
                fullName: fullName,
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

            user.UserRoles = new List<UserRoleEntity>
            {
                new UserRoleEntity
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

        private async Task<Trip> SeedTripAsync(Guid userId, string title)
        {
            var trip = Trip.Create(
                userId: userId,
                title: title
            );

            await _dbContext.Set<Trip>().AddAsync(trip);
            await _dbContext.SaveChangesAsync();
            return trip;
        }

        private async Task<TripMemberEntity> SeedTripMemberAsync(Guid tripId, Guid userId, Guid ownerId, string role)
        {
            var tripMember = TripMemberEntity.Create(
                tripId: tripId,
                userId: userId,
                ownerId: ownerId,
                role: role
            );

            await _dbContext.Set<TripMemberEntity>().AddAsync(tripMember);
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
            var apiResponse = await response.Content.ReadFromJsonAsync<TestApiResponse<LoginResponse>>();

            return apiResponse!.Data.AccessToken;
        }

        #endregion
    }

    public class TestApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; } = default!;
        public string Message { get; set; } = string.Empty;
    }
}

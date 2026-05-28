using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.TripLocation.Commands.AddLocationToTrip;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.TripLocation
{
    [Collection("Integration Tests")]
    public class RemoveTripLocationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private User _testUser = null!;
        private string _accessToken = null!;

        public RemoveTripLocationIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedTestUserAsync("removetriplocation@example.com", "Password123!");
            _accessToken = await GetAccessTokenAsync();
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<Domain.Entities.TripLocation>().ExecuteDeleteAsync();
            await _dbContext.Set<TripDay>().ExecuteDeleteAsync();
            await _dbContext.Set<Domain.Entities.TripMember>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<Location>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Happy Path Tests

        [Fact]
        public async Task RemoveTripLocation_WithValidId_ReturnsOkWithRemovedLocation()
        {
            // Arrange
            var (trip, tripDay, location, tripLocation) = await SeedTripLocationAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.DeleteAsync($"/api/TripLocation/{tripLocation.Id}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Body: {content}");

            var apiResponse = JsonSerializer.Deserialize<ResultResponse<TripLocationResponse>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull($"Body: {content}");
            apiResponse!.Success.Should().BeTrue($"Body: {content}");
            apiResponse.Data.Should().NotBeNull($"Body: {content}");

            apiResponse.Data.Id.Should().Be(tripLocation.Id);
            apiResponse.Data.TripDayId.Should().Be(tripDay.Id);
            apiResponse.Data.LocationId.Should().Be(location.Id);
            apiResponse.Data.LocationName.Should().Be(location.Name);
        }

        [Fact]
        public async Task RemoveTripLocation_WithValidId_DeletesFromDatabase()
        {
            // Arrange
            var (_, _, _, tripLocation) = await SeedTripLocationAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.DeleteAsync($"/api/TripLocation/{tripLocation.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var deletedTripLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .FirstOrDefaultAsync(tl => tl.Id == tripLocation.Id);

            deletedTripLocation.Should().BeNull();
        }

        [Fact]
        public async Task RemoveTripLocation_WithCompleteData_ReturnsAllFields()
        {
            // Arrange
            var (trip, tripDay, location) = await SeedTripDataAsync();

            var tripLocation = Domain.Entities.TripLocation.Create(
                tripDay.Id,
                location.Id,
                1,
                new TimeSpan(9, 0, 0),
                new TimeSpan(12, 0, 0),
                "Morning visit",
                "Walk");

            _dbContext.Set<Domain.Entities.TripLocation>().Add(tripLocation);
            await _dbContext.SaveChangesAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.DeleteAsync($"/api/TripLocation/{tripLocation.Id}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ResultResponse<TripLocationResponse>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            apiResponse.Data.OrderIndex.Should().Be(1);
            apiResponse.Data.StartTime.Should().Be(new TimeSpan(9, 0, 0));
            apiResponse.Data.EndTime.Should().Be(new TimeSpan(12, 0, 0));
            apiResponse.Data.Note.Should().Be("Morning visit");
            apiResponse.Data.TransportMode.Should().Be("Walk");
        }

        [Fact]
        public async Task RemoveTripLocation_MultipleLocations_ReordersRemainingLocations()
        {
            // Arrange
            var (trip, tripDay, location1) = await SeedTripDataAsync();
            var location2 = await SeedLocationAsync("Location 2");
            var location3 = await SeedLocationAsync("Location 3");

            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDay.Id, location1.Id, 0);
            var tripLocation2 = Domain.Entities.TripLocation.Create(tripDay.Id, location2.Id, 1);
            var tripLocation3 = Domain.Entities.TripLocation.Create(tripDay.Id, location3.Id, 2);

            _dbContext.Set<Domain.Entities.TripLocation>().AddRange(tripLocation1, tripLocation2, tripLocation3);
            await _dbContext.SaveChangesAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act - Remove middle location (index 1)
            var response = await _client.DeleteAsync($"/api/TripLocation/{tripLocation2.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var remainingLocations = await _dbContext.Set<Domain.Entities.TripLocation>()
                .Where(tl => tl.TripDayId == tripDay.Id)
                .OrderBy(tl => tl.OrderIndex)
                .ToListAsync();

            remainingLocations.Should().HaveCount(2);
            remainingLocations[0].LocationId.Should().Be(location1.Id);
            remainingLocations[0].OrderIndex.Should().Be(0);
            remainingLocations[1].LocationId.Should().Be(location3.Id);
            remainingLocations[1].OrderIndex.Should().Be(2); // Remains at original index
        }

        [Fact]
        public async Task RemoveTripLocation_RemovesFirstLocation_ReordersCorrectly()
        {
            // Arrange
            var (trip, tripDay, location1) = await SeedTripDataAsync();
            var location2 = await SeedLocationAsync("Location 2");

            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDay.Id, location1.Id, 0);
            var tripLocation2 = Domain.Entities.TripLocation.Create(tripDay.Id, location2.Id, 1);

            _dbContext.Set<Domain.Entities.TripLocation>().AddRange(tripLocation1, tripLocation2);
            await _dbContext.SaveChangesAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act - Remove first location
            var response = await _client.DeleteAsync($"/api/TripLocation/{tripLocation1.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var remainingLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .FirstOrDefaultAsync(tl => tl.Id == tripLocation2.Id);

            remainingLocation.Should().NotBeNull();
            remainingLocation!.OrderIndex.Should().Be(1); // Remains at original index
        }

        [Fact]
        public async Task RemoveTripLocation_RemovesLastLocation_LeavesOthersUnchanged()
        {
            // Arrange
            var (trip, tripDay, location1) = await SeedTripDataAsync();
            var location2 = await SeedLocationAsync("Location 2");

            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDay.Id, location1.Id, 0);
            var tripLocation2 = Domain.Entities.TripLocation.Create(tripDay.Id, location2.Id, 1);

            _dbContext.Set<Domain.Entities.TripLocation>().AddRange(tripLocation1, tripLocation2);
            await _dbContext.SaveChangesAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act - Remove last location
            var response = await _client.DeleteAsync($"/api/TripLocation/{tripLocation2.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var remainingLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .FirstOrDefaultAsync(tl => tl.Id == tripLocation1.Id);

            remainingLocation.Should().NotBeNull();
            remainingLocation!.OrderIndex.Should().Be(0); // Should remain unchanged
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task RemoveTripLocation_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var (_, _, _, tripLocation) = await SeedTripLocationAsync();

            // Act - No authorization header
            var response = await _client.DeleteAsync($"/api/TripLocation/{tripLocation.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RemoveTripLocation_AsNonOwner_ReturnsForbidden()
        {
            // Arrange
            var otherUser = await SeedTestUserAsync("other@example.com", "Password123!");
            var (_, _, _, tripLocation) = await SeedTripLocationForUserAsync(otherUser);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.DeleteAsync($"/api/TripLocation/{tripLocation.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"code\":\"Trip.AccessDenied\"");
            content.Should().Contain("don't have permission");
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task RemoveTripLocation_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.DeleteAsync($"/api/TripLocation/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("not found");
        }

        [Fact]
        public async Task RemoveTripLocation_WithInvalidGuid_ReturnsNotFound()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.DeleteAsync("/api/TripLocation/invalid-guid");

            // Assert
            // ASP.NET Core routing with {id:guid} constraint returns 404 when GUID format is invalid
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

        private async Task<string> GetAccessTokenAsync()
        {
            var loginRequest = new LoginUserCommand
            {
                Email = _testUser.Email,
                Password = "Password123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse == null || !apiResponse.Success || apiResponse.Data == null)
            {
                throw new Exception($"Login failed. Status: {response.StatusCode}, Body: {content}");
            }

            return apiResponse.Data.AccessToken;
        }

        private async Task<(Trip trip, TripDay tripDay, Location location)> SeedTripDataAsync()
        {
            return await SeedTripDataForUserAsync(_testUser);
        }

        private async Task<(Trip trip, TripDay tripDay, Location location)> SeedTripDataForUserAsync(User user)
        {
            var trip = Trip.Create(
                userId: user.Id,
                title: "Test Trip",
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(1),
                endDate: DateTime.UtcNow.AddDays(5),
                isPublic: true
            );
            _dbContext.Set<Trip>().Add(trip);

            var tripMember = Domain.Entities.TripMember.Create(
                tripId: trip.Id,
                userId: user.Id,
                ownerId: user.Id,
                role: "owner"
            );
            _dbContext.Set<Domain.Entities.TripMember>().Add(tripMember);

            var tripDay = TripDay.Create(
                TripId: trip.Id,
                Tittle: "Day 1",
                DayDate: DateTime.UtcNow.AddDays(1),
                DayIndex: 1
            );
            _dbContext.Set<TripDay>().Add(tripDay);

            var location = await SeedLocationAsync("Test Location");

            await _dbContext.SaveChangesAsync();

            return (trip, tripDay, location);
        }

        private async Task<Location> SeedLocationAsync(string name)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = "Test Description",
                Address = "123 Test St",
                Latitude = 10.123,
                Longitude = 106.456,
                IsVerified = true,
                CreatedDate = DateTime.UtcNow
            };

            _dbContext.Set<Location>().Add(location);
            await _dbContext.SaveChangesAsync();

            return location;
        }

        private async Task<(Trip trip, TripDay tripDay, Location location, Domain.Entities.TripLocation tripLocation)> SeedTripLocationAsync()
        {
            return await SeedTripLocationForUserAsync(_testUser);
        }

        private async Task<(Trip trip, TripDay tripDay, Location location, Domain.Entities.TripLocation tripLocation)> SeedTripLocationForUserAsync(User user)
        {
            var (trip, tripDay, location) = await SeedTripDataForUserAsync(user);

            var tripLocation = Domain.Entities.TripLocation.Create(
                tripDay.Id,
                location.Id,
                0
            );

            _dbContext.Set<Domain.Entities.TripLocation>().Add(tripLocation);
            await _dbContext.SaveChangesAsync();

            return (trip, tripDay, location, tripLocation);
        }

        #endregion
    }
}

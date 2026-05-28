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
using Vivu.Application.UseCases.TripLocation.Commands.ReorderTripLocations;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.TripLocation
{
    [Collection("Integration Tests")]
    public class ReorderTripLocationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private User _testUser = null!;
        private string _accessToken = null!;

        public ReorderTripLocationIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedTestUserAsync("reordertriplocation@example.com", "Password123!");
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
        public async Task ReorderTripLocations_ValidReorder_ReturnsOkWithReorderedLocations()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(3);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>
                {
                    tripLocations[2].Id, // Move 3rd to 1st
                    tripLocations[0].Id, // Move 1st to 2nd
                    tripLocations[1].Id  // Move 2nd to 3rd
                }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Body: {content}");

            var apiResponse = JsonSerializer.Deserialize<ResultResponse<List<TripLocationResponse>>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull($"Body: {content}");
            apiResponse!.Success.Should().BeTrue($"Body: {content}");
            apiResponse.Data.Should().NotBeNull($"Body: {content}");
            apiResponse.Data.Should().HaveCount(3);

            // Verify response order
            apiResponse.Data[0].Id.Should().Be(tripLocations[2].Id);
            apiResponse.Data[0].OrderIndex.Should().Be(1);
            apiResponse.Data[1].Id.Should().Be(tripLocations[0].Id);
            apiResponse.Data[1].OrderIndex.Should().Be(2);
            apiResponse.Data[2].Id.Should().Be(tripLocations[1].Id);
            apiResponse.Data[2].OrderIndex.Should().Be(3);
        }

        [Fact]
        public async Task ReorderTripLocations_ValidReorder_UpdatesDatabaseCorrectly()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(3);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>
                {
                    tripLocations[2].Id,
                    tripLocations[0].Id,
                    tripLocations[1].Id
                }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify database order - clear tracked entities and query fresh data
            _dbContext.ChangeTracker.Clear();
            var reorderedLocations = await _dbContext.Set<Domain.Entities.TripLocation>()
                .AsNoTracking()
                .Where(tl => tl.TripDayId == tripDay.Id)
                .OrderBy(tl => tl.OrderIndex)
                .ToListAsync();

            reorderedLocations.Should().HaveCount(3);
            reorderedLocations[0].Id.Should().Be(tripLocations[2].Id);
            reorderedLocations[0].OrderIndex.Should().Be(1);
            reorderedLocations[1].Id.Should().Be(tripLocations[0].Id);
            reorderedLocations[1].OrderIndex.Should().Be(2);
            reorderedLocations[2].Id.Should().Be(tripLocations[1].Id);
            reorderedLocations[2].OrderIndex.Should().Be(3);
        }

        [Fact]
        public async Task ReorderTripLocations_ReverseOrder_UpdatesCorrectly()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(4);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>
                {
                    tripLocations[3].Id,
                    tripLocations[2].Id,
                    tripLocations[1].Id,
                    tripLocations[0].Id
                }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var reorderedLocations = await _dbContext.Set<Domain.Entities.TripLocation>()
                .Where(tl => tl.TripDayId == tripDay.Id)
                .OrderBy(tl => tl.OrderIndex)
                .ToListAsync();

            reorderedLocations[0].Id.Should().Be(tripLocations[3].Id);
            reorderedLocations[1].Id.Should().Be(tripLocations[2].Id);
            reorderedLocations[2].Id.Should().Be(tripLocations[1].Id);
            reorderedLocations[3].Id.Should().Be(tripLocations[0].Id);
        }

        [Fact]
        public async Task ReorderTripLocations_SameOrder_ReturnsSuccessfully()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(2);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>
                {
                    tripLocations[0].Id,
                    tripLocations[1].Id
                }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ResultResponse<List<TripLocationResponse>>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task ReorderTripLocations_SingleLocation_ReturnsSuccessfully()
        {
            // Arrange
            var (trip, tripDay, location, tripLocation) = await SeedTripLocationAsync();

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid> { tripLocation.Id }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = JsonSerializer.Deserialize<ResultResponse<List<TripLocationResponse>>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(1);
            apiResponse.Data[0].OrderIndex.Should().Be(1);
        }

        [Fact]
        public async Task ReorderTripLocations_SwapTwoLocations_UpdatesCorrectly()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(2);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>
                {
                    tripLocations[1].Id,
                    tripLocations[0].Id
                }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Clear tracked entities and query fresh data
            _dbContext.ChangeTracker.Clear();
            var reorderedLocations = await _dbContext.Set<Domain.Entities.TripLocation>()
                .AsNoTracking()
                .Where(tl => tl.TripDayId == tripDay.Id)
                .OrderBy(tl => tl.OrderIndex)
                .ToListAsync();

            reorderedLocations[0].Id.Should().Be(tripLocations[1].Id);
            reorderedLocations[0].OrderIndex.Should().Be(1);
            reorderedLocations[1].Id.Should().Be(tripLocations[0].Id);
            reorderedLocations[1].OrderIndex.Should().Be(2);
        }

        #endregion Happy Path Tests

        #region Authorization Tests

        [Fact]
        public async Task ReorderTripLocations_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(2);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = tripLocations.Select(tl => tl.Id).ToList()
            };

            // Act - No authorization header
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReorderTripLocations_AsNonOwner_ReturnsForbidden()
        {
            // Arrange
            var otherUser = await SeedTestUserAsync("other@example.com", "Password123!");
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsForUserAsync(otherUser, 2);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = tripLocations.Select(tl => tl.Id).ToList()
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"code\":\"Trip.AccessDenied\"");
        }

        #endregion Authorization Tests

        #region Validation Tests

        [Fact]
        public async Task ReorderTripLocations_WithNonExistentTripDay_ReturnsNotFound()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("TripDay");
            content.Should().Contain("not found");
        }

        [Fact]
        public async Task ReorderTripLocations_WithEmptyTripDayId_ReturnsBadRequest()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.Empty,
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("TripDayId");
        }

        [Fact]
        public async Task ReorderTripLocations_WithEmptyLocationList_ReturnsBadRequest()
        {
            // Arrange
            var (trip, tripDay, location, tripLocation) = await SeedTripLocationAsync();

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>()
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("OrderedTripLocationIds");
        }

        [Fact]
        public async Task ReorderTripLocations_WithInvalidLocationIds_ReturnsBadRequest()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(2);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>
                {
                    tripLocations[0].Id,
                    Guid.NewGuid() // Invalid ID
                }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("InvalidIds");
        }

        [Fact]
        public async Task ReorderTripLocations_WithMissingLocations_ReturnsBadRequest()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(3);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>
                {
                    tripLocations[0].Id,
                    tripLocations[1].Id
                    // Missing tripLocations[2]
                }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("InvalidCount");
        }

        [Fact]
        public async Task ReorderTripLocations_WithDuplicateIds_ReturnsBadRequest()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(2);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>
                {
                    tripLocations[0].Id,
                    tripLocations[0].Id // Duplicate
                }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("duplicate");
        }

        [Fact]
        public async Task ReorderTripLocations_WithEmptyGuidInList_ReturnsBadRequest()
        {
            // Arrange
            var (trip, tripDay, locations, tripLocations) = await SeedMultipleTripLocationsAsync(2);

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDay.Id,
                OrderedTripLocationIds = new List<Guid>
                {
                    tripLocations[0].Id,
                    Guid.Empty
                }
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/TripLocation/reorder", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("TripLocationId");
        }

        #endregion Validation Tests

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

        private async Task<(Trip trip, TripDay tripDay, List<Location> locations, List<Domain.Entities.TripLocation> tripLocations)> SeedMultipleTripLocationsAsync(int count)
        {
            return await SeedMultipleTripLocationsForUserAsync(_testUser, count);
        }

        private async Task<(Trip trip, TripDay tripDay, List<Location> locations, List<Domain.Entities.TripLocation> tripLocations)> SeedMultipleTripLocationsForUserAsync(User user, int count)
        {
            var (trip, tripDay, firstLocation) = await SeedTripDataForUserAsync(user);

            var locations = new List<Location> { firstLocation };
            var tripLocations = new List<Domain.Entities.TripLocation>();

            // Create first trip location
            var firstTripLocation = Domain.Entities.TripLocation.Create(
                tripDay.Id,
                firstLocation.Id,
                0
            );
            tripLocations.Add(firstTripLocation);

            // Create additional locations and trip locations
            for (int i = 1; i < count; i++)
            {
                var location = await SeedLocationAsync($"Location {i + 1}");
                locations.Add(location);

                var tripLocation = Domain.Entities.TripLocation.Create(
                    tripDay.Id,
                    location.Id,
                    i
                );
                tripLocations.Add(tripLocation);
            }

            _dbContext.Set<Domain.Entities.TripLocation>().AddRange(tripLocations);
            await _dbContext.SaveChangesAsync();

            return (trip, tripDay, locations, tripLocations);
        }

        #endregion Helper Methods
    }
}
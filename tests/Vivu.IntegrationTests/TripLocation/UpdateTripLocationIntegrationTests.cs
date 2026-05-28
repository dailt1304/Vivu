using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.TripLocation.Commands.UpdateTripLocation;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.TripLocation
{
    [Collection("Integration Tests")]
    public class UpdateTripLocationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private User _testUser = null!;
        private string _accessToken = null!;

        public UpdateTripLocationIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedTestUserAsync("updatetriplocation@example.com", "Password123!");
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
        public async Task UpdateTripLocation_WithValidData_ReturnsOkWithUpdatedLocation()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location2.Id,
                OrderIndex = 2,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                Note = "Updated afternoon visit",
                TransportMode = "Metro"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Body: {content}");

            var apiResponse = JsonSerializer.Deserialize<ResultResponse<TripLocationResponse>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull($"Body: {content}");
            apiResponse!.Data.Should().NotBeNull($"Body: {content}");
            apiResponse.Success.Should().BeTrue($"Body: {content}");
            apiResponse.Data.Should().NotBeNull($"Body: {content}");

            apiResponse.Data.Id.Should().Be(tripLocation.Id);
            apiResponse.Data.TripDayId.Should().Be(tripDay.Id);
            apiResponse.Data.LocationId.Should().Be(location2.Id);
            apiResponse.Data.LocationName.Should().Be(location2.Name);
            apiResponse.Data.OrderIndex.Should().Be(2);
            apiResponse.Data.StartTime.Should().Be(new TimeSpan(14, 0, 0));
            apiResponse.Data.EndTime.Should().Be(new TimeSpan(17, 0, 0));
            apiResponse.Data.Note.Should().Be("Updated afternoon visit");
            apiResponse.Data.TransportMode.Should().Be("Metro");
        }

        [Fact]
        public async Task UpdateTripLocation_WithValidData_UpdatesInDatabase()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location2.Id,
                OrderIndex = 3,
                Note = "Changed location"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedTripLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .Include(tl => tl.Location)
                .AsNoTracking()
                .FirstOrDefaultAsync(tl => tl.Id == tripLocation.Id);

            updatedTripLocation.Should().NotBeNull();
            updatedTripLocation!.LocationId.Should().Be(location2.Id);
            updatedTripLocation.OrderIndex.Should().Be(3);
            updatedTripLocation.Note.Should().Be("Changed location");
            updatedTripLocation.Location.Name.Should().Be(location2.Name);
        }

        [Fact]
        public async Task UpdateTripLocation_WithMinimalData_UpdatesSuccessfully()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location1.Id,
                OrderIndex = 5
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ResultResponse<TripLocationResponse>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.OrderIndex.Should().Be(5);
            
            // When times are not provided, they are set to null (Update replaces all fields)
            _dbContext.ChangeTracker.Clear();
            var updatedTripLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(tl => tl.Id == tripLocation.Id);
            
            updatedTripLocation!.StartTime.Should().BeNull();
            updatedTripLocation.EndTime.Should().BeNull();
            updatedTripLocation.Note.Should().BeNull();
            updatedTripLocation.TransportMode.Should().BeNull();
        }

        [Fact]
        public async Task UpdateTripLocation_ChangingOrderIndex_UpdatesCorrectly()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var initialOrderIndex = tripLocation.OrderIndex;

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location1.Id,
                OrderIndex = 10
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedTripLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(tl => tl.Id == tripLocation.Id);

            updatedTripLocation!.OrderIndex.Should().Be(10);
            updatedTripLocation.OrderIndex.Should().NotBe(initialOrderIndex);
        }

        [Fact]
        public async Task UpdateTripLocation_UpdateTimesAndTransportMode_UpdatesAllFields()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location1.Id,
                OrderIndex = 1,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(11, 45, 0),
                Note = "Early morning visit",
                TransportMode = "Bicycle"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedTripLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(tl => tl.Id == tripLocation.Id);

            updatedTripLocation!.StartTime.Should().Be(new TimeSpan(8, 30, 0));
            updatedTripLocation.EndTime.Should().Be(new TimeSpan(11, 45, 0));
            updatedTripLocation.Note.Should().Be("Early morning visit");
            updatedTripLocation.TransportMode.Should().Be("Bicycle");
        }

        [Fact]
        public async Task UpdateTripLocation_MoveToDifferentTripDay_UpdatesSuccessfully()
        {
            // Arrange
            var (trip, oldTripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();
            
            var newTripDay = TripDay.Create(
                TripId: trip.Id,
                Tittle: "Day 2",
                DayDate: DateTime.UtcNow.AddDays(2),
                DayIndex: 2
            );
            _dbContext.Set<TripDay>().Add(newTripDay);
            await _dbContext.SaveChangesAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = newTripDay.Id,
                LocationId = location1.Id,
                OrderIndex = 1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedTripLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(tl => tl.Id == tripLocation.Id);

            updatedTripLocation!.TripDayId.Should().Be(newTripDay.Id);
            updatedTripLocation.TripDayId.Should().NotBe(oldTripDay.Id);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task UpdateTripLocation_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location1.Id,
                OrderIndex = 1
            };

            // Act - No authorization header
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateTripLocation_AsNonOwner_ReturnsForbidden()
        {
            // Arrange
            var otherUser = await SeedTestUserAsync("other@example.com", "Password123!");
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataForUserAsync(otherUser);

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location1.Id,
                OrderIndex = 2
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Trip.AccessDenied");
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task UpdateTripLocation_WithNonExistentTripLocation_ReturnsNotFound()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location1.Id,
                OrderIndex = 1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{Guid.NewGuid()}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("not found");
        }

        [Fact]
        public async Task UpdateTripLocation_WithNonExistentTripDay_ReturnsNotFound()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = Guid.NewGuid(), // Non-existent
                LocationId = location1.Id,
                OrderIndex = 1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("not found");
        }

        [Fact]
        public async Task UpdateTripLocation_WithNonExistentLocation_ReturnsNotFound()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = Guid.NewGuid(), // Non-existent
                OrderIndex = 1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("not found");
        }

        [Fact]
        public async Task UpdateTripLocation_WithInvalidOrderIndex_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location1.Id,
                OrderIndex = -5 // Invalid
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateTripLocation_WithStartTimeAfterEndTime_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location1.Id,
                OrderIndex = 1,
                StartTime = new TimeSpan(15, 0, 0),
                EndTime = new TimeSpan(10, 0, 0) // End before start
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Time Conflict Tests

        [Fact]
        public async Task UpdateTripLocation_WithTimeConflict_ReturnsBadRequest()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            // Create another trip location with specific time slot
            var anotherTripLocation = Domain.Entities.TripLocation.Create(
                tripDay.Id,
                location2.Id,
                2,
                new TimeSpan(13, 0, 0),
                new TimeSpan(16, 0, 0)
            );
            _dbContext.Set<Domain.Entities.TripLocation>().Add(anotherTripLocation);
            await _dbContext.SaveChangesAsync();

            // Try to update first location to overlap with second
            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location2.Id, // Same location
                OrderIndex = 1,
                StartTime = new TimeSpan(14, 0, 0), // Overlaps with 13:00-16:00
                EndTime = new TimeSpan(17, 0, 0)
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("TripLocation.TimeConflict");
            content.Should().Contain("already scheduled");
        }

        [Fact]
        public async Task UpdateTripLocation_WithNoTimeConflict_Succeeds()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            // Create another trip location
            var anotherTripLocation = Domain.Entities.TripLocation.Create(
                tripDay.Id,
                location2.Id,
                2,
                new TimeSpan(13, 0, 0),
                new TimeSpan(16, 0, 0)
            );
            _dbContext.Set<Domain.Entities.TripLocation>().Add(anotherTripLocation);
            await _dbContext.SaveChangesAsync();

            // Update with non-overlapping time
            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location2.Id,
                OrderIndex = 1,
                StartTime = new TimeSpan(17, 0, 0), // After the other location
                EndTime = new TimeSpan(19, 0, 0)
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateTripLocation_UpdateSameLocationWithoutChangingTimes_Succeeds()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            // Update existing trip location (should exclude itself from conflict check)
            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripLocation.TripDayId,
                LocationId = tripLocation.LocationId,
                OrderIndex = 5,
                StartTime = tripLocation.StartTime,
                EndTime = tripLocation.EndTime,
                Note = "Just changing order index"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateTripLocation_ExplicitlySetTimesToNull_Succeeds()
        {
            // Arrange
            var (trip, tripDay, location1, location2, tripLocation) = await SeedTripLocationDataAsync();

            // Verify the trip location initially has times
            tripLocation.StartTime.Should().NotBeNull();
            tripLocation.EndTime.Should().NotBeNull();

            // Update to explicitly set times to null
            var command = new UpdateTripLocationCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location1.Id,
                OrderIndex = 1,
                StartTime = null,
                EndTime = null,
                Note = "Times removed"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripLocation/{tripLocation.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedTripLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(tl => tl.Id == tripLocation.Id);

            updatedTripLocation.Should().NotBeNull();
            updatedTripLocation!.StartTime.Should().BeNull();
            updatedTripLocation.EndTime.Should().BeNull();
            updatedTripLocation.Note.Should().Be("Times removed");
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

        private async Task<(Trip trip, TripDay tripDay, Location location1, Location location2, Domain.Entities.TripLocation tripLocation)> 
            SeedTripLocationDataAsync()
        {
            return await SeedTripLocationDataForUserAsync(_testUser);
        }

        private async Task<(Trip trip, TripDay tripDay, Location location1, Location location2, Domain.Entities.TripLocation tripLocation)> 
            SeedTripLocationDataForUserAsync(User user)
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

            var location1 = new Location
            {
                Id = Guid.NewGuid(),
                Name = "Original Location",
                Description = "Original Description",
                Address = "123 Original St",
                Latitude = 10.123,
                Longitude = 106.456,
                IsVerified = true,
                CreatedDate = DateTime.UtcNow
            };
            _dbContext.Set<Location>().Add(location1);

            var location2 = new Location
            {
                Id = Guid.NewGuid(),
                Name = "New Location",
                Description = "New Description",
                Address = "456 New St",
                Latitude = 10.789,
                Longitude = 106.123,
                IsVerified = true,
                CreatedDate = DateTime.UtcNow
            };
            _dbContext.Set<Location>().Add(location2);

            var tripLocation = Domain.Entities.TripLocation.Create(
                tripDayId: tripDay.Id,
                locationId: location1.Id,
                orderIndex: 1,
                startTime: new TimeSpan(9, 0, 0),
                endTime: new TimeSpan(12, 0, 0),
                note: "Original note",
                transportMode: "Walk"
            );
            _dbContext.Set<Domain.Entities.TripLocation>().Add(tripLocation);

            await _dbContext.SaveChangesAsync();

            return (trip, tripDay, location1, location2, tripLocation);
        }

        #endregion
    }
}

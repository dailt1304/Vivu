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
using Vivu.Application.UseCases.TripLocation.Commands.AddLocationToTrip;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.TripLocation
{
    [Collection("Integration Tests")]
    public class AddLocationToTripIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private User _testUser = null!;
        private string _accessToken = null!;

        public AddLocationToTripIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedTestUserAsync("triplocation@example.com", "Password123!");
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
        public async Task AddLocationToTrip_WithValidData_ReturnsCreatedWithLocation()
        {
            var (_, tripDay, location) = await SeedTripDataAsync();

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                Note = "Morning visit",
                TransportMode = "Walk"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _client.PostAsJsonAsync("/api/TripLocation", command);
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.Created, $"Body: {content}");

            var apiResponse = JsonSerializer.Deserialize<ResultResponse<TripLocationResponse>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull($"Body: {content}");
            apiResponse!.Success.Should().BeTrue($"Body: {content}");
            apiResponse.Data.Should().NotBeNull($"Body: {content}");

            apiResponse.Data.TripDayId.Should().Be(tripDay.Id);
            apiResponse.Data.LocationId.Should().Be(location.Id);
            apiResponse.Data.LocationName.Should().Be(location.Name);
            apiResponse.Data.OrderIndex.Should().Be(1);
            apiResponse.Data.StartTime.Should().Be(new TimeSpan(9, 0, 0));
            apiResponse.Data.EndTime.Should().Be(new TimeSpan(12, 0, 0));
            apiResponse.Data.Note.Should().Be("Morning visit");
            apiResponse.Data.TransportMode.Should().Be("Walk");
        }

        [Fact]
        public async Task AddLocationToTrip_WithValidData_SavesToDatabase()
        {
            // Arrange
            var (trip, tripDay, location) = await SeedTripDataAsync();
            
            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripLocation", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var savedTripLocation = await _dbContext.Set<Domain.Entities.TripLocation>()
                .Include(tl => tl.Location)
                .FirstOrDefaultAsync(tl => tl.TripDayId == tripDay.Id && tl.LocationId == location.Id);

            savedTripLocation.Should().NotBeNull();
            savedTripLocation!.OrderIndex.Should().Be(1);
            savedTripLocation.Location.Name.Should().Be(location.Name);
        }

        [Fact]
        public async Task AddLocationToTrip_WithMinimalData_Succeeds()
        {
            var (_, tripDay, location) = await SeedTripDataAsync();

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _client.PostAsJsonAsync("/api/TripLocation", command);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ResultResponse<TripLocationResponse>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();

            // ✅ ép in BODY khi fail
            if (apiResponse?.Success != true)
                throw new Exception($"STATUS: {(int)response.StatusCode} {response.StatusCode}\nBODY: {content}");

            apiResponse.Data.Should().NotBeNull();

            apiResponse.Data.StartTime.Should().BeNull();
            apiResponse.Data.EndTime.Should().BeNull();
            apiResponse.Data.Note.Should().BeNull();
            apiResponse.Data.TransportMode.Should().BeNull();
        }


        [Fact]
        public async Task AddLocationToTrip_MultipleLocations_AllSavedCorrectly()
        {
            // Arrange
            var (trip, tripDay, location1) = await SeedTripDataAsync();
            var location2 = await SeedLocationAsync("Location 2");
            var location3 = await SeedLocationAsync("Location 3");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var command1 = new AddLocationToTripCommand { TripDayId = tripDay.Id, LocationId = location1.Id, OrderIndex = 1 };
            var command2 = new AddLocationToTripCommand { TripDayId = tripDay.Id, LocationId = location2.Id, OrderIndex = 2 };
            var command3 = new AddLocationToTripCommand { TripDayId = tripDay.Id, LocationId = location3.Id, OrderIndex = 3 };

            // Act
            await _client.PostAsJsonAsync("/api/TripLocation", command1);
            await _client.PostAsJsonAsync("/api/TripLocation", command2);
            await _client.PostAsJsonAsync("/api/TripLocation", command3);

            // Assert
            var tripLocations = await _dbContext.Set<Domain.Entities.TripLocation>()
                .Where(tl => tl.TripDayId == tripDay.Id)
                .OrderBy(tl => tl.OrderIndex)
                .ToListAsync();

            tripLocations.Should().HaveCount(3);
            tripLocations[0].LocationId.Should().Be(location1.Id);
            tripLocations[1].LocationId.Should().Be(location2.Id);
            tripLocations[2].LocationId.Should().Be(location3.Id);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task AddLocationToTrip_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var (trip, tripDay, location) = await SeedTripDataAsync();
            
            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 1
            };

            // Act - No authorization header
            var response = await _client.PostAsJsonAsync("/api/TripLocation", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AddLocationToTrip_AsNonOwner_ReturnsForbidden()
        {
            var otherUser = await SeedTestUserAsync("other@example.com", "Password123!");
            var (_, tripDay, location) = await SeedTripDataForUserAsync(otherUser);

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _client.PostAsJsonAsync("/api/TripLocation", command);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"code\":\"Trip.AccessDenied\"");
            content.Should().Contain("don't have permission");
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task AddLocationToTrip_WithNonExistentTripDay_ReturnsNotFound()
        {
            // Arrange
            var location = await SeedLocationAsync("Test Location");
            
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(), // Non-existent
                LocationId = location.Id,
                OrderIndex = 1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripLocation", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("not found");
        }

        [Fact]
        public async Task AddLocationToTrip_WithNonExistentLocation_ReturnsNotFound()
        {
            // Arrange
            var (trip, tripDay, _) = await SeedTripDataAsync();
            
            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = Guid.NewGuid(), // Non-existent
                OrderIndex = 1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripLocation", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("not found");
        }

        [Fact]
        public async Task AddLocationToTrip_WithInvalidOrderIndex_ReturnsUnprocessableEntity()
        {
            var (_, tripDay, location) = await SeedTripDataAsync();

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = -1
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _client.PostAsJsonAsync("/api/TripLocation", command);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Time Conflict Tests

        [Fact]
        public async Task AddLocationToTrip_WithTimeConflict_ReturnsBadRequest()
        {
            var (_, tripDay, location) = await SeedTripDataAsync();

            var firstCommand = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(12, 0, 0)
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            await _client.PostAsJsonAsync("/api/TripLocation", firstCommand);

            var conflictingCommand = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 2,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(13, 0, 0)
            };

            var response = await _client.PostAsJsonAsync("/api/TripLocation", conflictingCommand);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("TripLocation.TimeConflict");
            content.Should().Contain("already scheduled");
        }

        [Fact]
        public async Task AddLocationToTrip_WithNoTimeConflict_Succeeds()
        {
            // Arrange
            var (trip, tripDay, location) = await SeedTripDataAsync();
            
            var firstCommand = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(12, 0, 0)
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            await _client.PostAsJsonAsync("/api/TripLocation", firstCommand);

            // Non-overlapping time slot 13:00 - 15:00
            var nonConflictingCommand = new AddLocationToTripCommand
            {
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 2,
                StartTime = new TimeSpan(13, 0, 0),
                EndTime = new TimeSpan(15, 0, 0)
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripLocation", nonConflictingCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
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

        #endregion
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    // Response model for Login endpoint (uses "success" and "data")
    public class ApiResponse<T>
    {
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public T Data { get; set; } = default!;
        
        [System.Text.Json.Serialization.JsonPropertyName("error")]
        public ErrorDetail? Error { get; set; }
    }

    // Response model for AddLocationToTrip endpoint (uses "isSuccess" and "value")
    public class ResultResponse<T>
    {
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public T Data { get; set; } = default!;
        
        [System.Text.Json.Serialization.JsonPropertyName("error")]
        public ErrorDetail? Error { get; set; }
    }

    public class ErrorDetail
    {
        [System.Text.Json.Serialization.JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}

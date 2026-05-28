using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.Locations
{
    [Collection("Integration Tests")]
    public class DeleteLocationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _adminUser = null!;
        private User _normalUser = null!;
        private string _adminToken = string.Empty;
        private string _normalUserToken = string.Empty;

        public DeleteLocationIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _adminUser = await SeedAdminUserAsync("admin_delete@example.com", "Password123!");
            _normalUser = await SeedNormalUserAsync("user_delete@example.com", "Password123!");
            _adminToken = await GetAccessTokenAsync("admin_delete@example.com", "Password123!");
            _normalUserToken = await GetAccessTokenAsync("user_delete@example.com", "Password123!");
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
            await _dbContext.Set<LocationReport>().ExecuteDeleteAsync();
            await _dbContext.Set<LocationDetail>().ExecuteDeleteAsync();
            await _dbContext.Set<Location>().ExecuteDeleteAsync();
            await _dbContext.Set<LocationCategory>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRoleEntity>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Helper Methods

        private async Task<User> SeedAdminUserAsync(string email, string password)
        {
            var moderatorRole = new Role { Id = Guid.NewGuid(), RoleName = "MODERATOR", RoleDescription = "Moderator Role" };
            await _dbContext.Set<Role>().AddAsync(moderatorRole);

            var hashedPassword = _passwordHasher.HashPassword(password);
            var user = User.Create(email, hashedPassword, "Moderator User");
            await _dbContext.Users.AddAsync(user);

            var userRole = new UserRoleEntity { UserId = user.Id, RoleId = moderatorRole.Id };
            await _dbContext.Set<UserRoleEntity>().AddAsync(userRole);

            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<User> SeedNormalUserAsync(string email, string password)
        {
            var hashedPassword = _passwordHasher.HashPassword(password);
            var user = User.Create(email, hashedPassword, "Normal User");
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var loginRequest = new LoginUserCommand
            {
                Email = email,
                Password = password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var loginResponse = await response.Content.ReadFromJsonAsync<DeleteLocationApiResponse<DeleteLocationLoginResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return loginResponse!.Data!.AccessToken;
        }

        private async Task<Location> SeedTestLocationAsync(
            string name,
            string? description = null,
            string? address = null,
            bool withDetails = false,
            bool isDeleted = false)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description ?? "Test Description",
                Address = address ?? "123 Test Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = true,
                IsDeleted = isDeleted
            };

            await _dbContext.Set<Location>().AddAsync(location);

            if (withDetails)
            {
                var locationDetail = new LocationDetail
                {
                    LocationId = location.Id,
                    OpeningHours = "9:00-22:00",
                    Phone = "0123456789",
                    Website = "https://test.com"
                };
                await _dbContext.Set<LocationDetail>().AddAsync(locationDetail);
            }

            await _dbContext.SaveChangesAsync();
            return location;
        }

        /// <summary>
        /// Seeds a location and an active trip that references it, to trigger UsedInActiveTrips error.
        /// </summary>
        private async Task<(Location location, Trip trip)> SeedLocationWithActiveTripAsync(string locationName)
        {
            var location = await SeedTestLocationAsync(locationName);

            // Create an active trip (status != "completed" and not deleted)
            var trip = Trip.Create(
                userId: _adminUser.Id,
                title: "Active Trip",
                description: "A trip using the location",
                startDate: DateTime.UtcNow.AddDays(1),
                endDate: DateTime.UtcNow.AddDays(5),
                isPublic: true
            );
            await _dbContext.Set<Trip>().AddAsync(trip);

            var tripDay = TripDay.Create(
                TripId: trip.Id,
                Tittle: "Day 1",
                DayDate: DateTime.UtcNow.AddDays(1),
                DayIndex: 1
            );
            await _dbContext.Set<TripDay>().AddAsync(tripDay);
            await _dbContext.SaveChangesAsync();

            // Create a TripLocation linking the trip day to the location
            var tripLocation = new Domain.Entities.TripLocation
            {
                Id = Guid.NewGuid(),
                TripDayId = tripDay.Id,
                LocationId = location.Id,
                OrderIndex = 1
            };
            await _dbContext.Set<Domain.Entities.TripLocation>().AddAsync(tripLocation);
            await _dbContext.SaveChangesAsync();

            return (location, trip);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task DeleteLocation_ValidRequest_ReturnsOk()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Location To Delete");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{location.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<DeleteLocationApiResponse<bool>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteLocation_ValidRequest_MarksLocationAsDeletedInDatabase()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Location To Delete From DB");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{location.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var deletedLocation = await _dbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            deletedLocation.Should().NotBeNull();
            deletedLocation!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteLocation_LocationWithDetails_DeletesSuccessfully()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Location With Details", withDetails: true);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{location.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var deletedLocation = await _dbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            deletedLocation!.IsDeleted.Should().BeTrue();
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task DeleteLocation_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{location.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeleteLocation_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{location.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeleteLocation_WithNormalUserToken_ReturnsForbidden()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _normalUserToken);

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{location.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region Not Found Tests

        [Fact]
        public async Task DeleteLocation_NonExistentLocation_ReturnsNotFound()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
            var nonExistentLocationId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{nonExistentLocationId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Already Deleted Tests

        [Fact]
        public async Task DeleteLocation_AlreadyDeletedLocation_ReturnsConflict()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Already Deleted Location", isDeleted: true);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{location.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("AlreadyDeleted");
        }

        #endregion

        #region Used In Active Trips Tests

        [Fact]
        public async Task DeleteLocation_UsedInActiveTrip_ReturnsBadRequest()
        {
            // Arrange
            var (location, _) = await SeedLocationWithActiveTripAsync("Location In Active Trip");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{location.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("UsedInActiveTrips");
        }

        [Fact]
        public async Task DeleteLocation_LocationNotInActiveTrip_DeletesSuccessfully()
        {
            // Arrange – location with no trips at all
            var location = await SeedTestLocationAsync("Location Without Trips");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{location.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var deletedLocation = await _dbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            deletedLocation!.IsDeleted.Should().BeTrue();
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task DeleteLocation_EmptyGuid_ReturnsNotFoundOrMethodNotAllowed()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            // Act
            var response = await _client.DeleteAsync($"/api/locations/{Guid.Empty}");

            // Assert
            // Empty GUID routes to the endpoint but the handler returns NotFound for non-existent location
            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity);
        }

        #endregion
    }

    #region Helper Classes

    public class DeleteLocationApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteLocationLoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    #endregion
}

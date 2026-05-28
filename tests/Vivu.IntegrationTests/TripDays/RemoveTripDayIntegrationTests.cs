using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Infrastructure.Data;
using Xunit;
using TripLocationEntity = Vivu.Domain.Entities.TripLocation;

namespace Vivu.IntegrationTests.TripDays
{
    [Collection("Integration Tests")]
    public class RemoveTripDayIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public RemoveTripDayIntegrationTests(IntegrationTestWebAppFactory factory)
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
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"TripLocations\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"TripDays\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"TripMembers\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"Trips\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"Locations\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"UserProfiles\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"Users\"");
        }

        #region Happy Path Tests

        [Fact]
        public async Task RemoveTripDay_ValidRequest_RemovesTripDaySuccessfully()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("remove_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            SetAuth(token);

            // Act
            var response = await _client.DeleteAsync($"/api/TripDay/{tripDay.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TripDayResponse>>(content, JsonOpts);

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(tripDay.Id);
            apiResponse.Data.Title.Should().Be("Day 1");

            // Verify in database - TripDay should be deleted
            var deletedTripDay = await _dbContext.TripDays
                .AsNoTracking()
                .FirstOrDefaultAsync(td => td.Id == tripDay.Id);
            deletedTripDay.Should().BeNull();
        }

        [Fact]
        public async Task RemoveTripDay_WithMultipleDays_ReordersRemainingDays()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("reorder_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            var tripDay3 = await SeedTripDayAsync(trip.Id, 3, DateTime.UtcNow.AddDays(7), "Day 3");
            SetAuth(token);

            // Act - Remove the middle day (Day 2)
            var response = await _client.DeleteAsync($"/api/TripDay/{tripDay2.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify remaining days are reordered
            var remainingDays = await _dbContext.TripDays
                .AsNoTracking()
                .Where(td => td.TripId == trip.Id)
                .OrderBy(td => td.DayIndex)
                .ToListAsync();

            remainingDays.Should().HaveCount(2);
            remainingDays[0].Id.Should().Be(tripDay1.Id);
            remainingDays[0].DayIndex.Should().Be(1);
            remainingDays[1].Id.Should().Be(tripDay3.Id);
            remainingDays[1].DayIndex.Should().Be(2); // Should be reordered from 3 to 2
        }

        [Fact]
        public async Task RemoveTripDay_WithTripLocations_RemovesTripDayAndLocations()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("remove_with_locations@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var location1 = await SeedLocationAsync("Location 1");
            var location2 = await SeedLocationAsync("Location 2");
            await SeedTripLocationAsync(tripDay.Id, location1.Id, 1);
            await SeedTripLocationAsync(tripDay.Id, location2.Id, 2);
            SetAuth(token);

            // Act
            var response = await _client.DeleteAsync($"/api/TripDay/{tripDay.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify TripDay is deleted
            var deletedTripDay = await _dbContext.TripDays
                .AsNoTracking()
                .FirstOrDefaultAsync(td => td.Id == tripDay.Id);
            deletedTripDay.Should().BeNull();

            // Verify TripLocations are also deleted
            var deletedLocations = await _dbContext.TripLocations
                .AsNoTracking()
                .Where(tl => tl.TripDayId == tripDay.Id)
                .ToListAsync();
            deletedLocations.Should().BeEmpty();
        }

        [Fact]
        public async Task RemoveTripDay_FirstDay_ReordersRemainingDays()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("remove_first@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            SetAuth(token);

            // Act - Remove first day
            var response = await _client.DeleteAsync($"/api/TripDay/{tripDay1.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify Day 2 is now Day 1
            var remainingDay = await _dbContext.TripDays
                .AsNoTracking()
                .FirstOrDefaultAsync(td => td.Id == tripDay2.Id);
            remainingDay.Should().NotBeNull();
            remainingDay!.DayIndex.Should().Be(1);
        }

        [Fact]
        public async Task RemoveTripDay_LastDay_SuccessfullyRemoves()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("remove_last@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            SetAuth(token);

            // Act - Remove last day
            var response = await _client.DeleteAsync($"/api/TripDay/{tripDay2.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify Day 1 remains unchanged
            var remainingDay = await _dbContext.TripDays
                .AsNoTracking()
                .FirstOrDefaultAsync(td => td.Id == tripDay1.Id);
            remainingDay.Should().NotBeNull();
            remainingDay!.DayIndex.Should().Be(1);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task RemoveTripDay_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var tripDayId = Guid.NewGuid();

            // Act - No authorization header
            var response = await _client.DeleteAsync($"/api/TripDay/{tripDayId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RemoveTripDay_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            var tripDayId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/TripDay/{tripDayId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task RemoveTripDay_UserNotOwner_ReturnsForbidden()
        {
            // Arrange
            var (owner, _) = await SeedUserWithTokenAsync("owner@example.com");
            var (otherUser, otherToken) = await SeedUserWithTokenAsync("other@example.com");
            var trip = await SeedTripAsync(owner.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");

            SetAuth(otherToken);

            // Act
            var response = await _client.DeleteAsync($"/api/TripDay/{tripDay.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.AccessDenied.Code);
        }

        [Fact]
        public async Task RemoveTripDay_TripDayNotFound_ReturnsNotFound()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("notfound_user@example.com");
            SetAuth(token);

            var nonExistentTripDayId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/TripDay/{nonExistentTripDayId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.NotFoundByTripDay(nonExistentTripDayId).Code);
        }

        #endregion

        #region Helper Methods

        private async Task<(User user, string token)> SeedUserWithTokenAsync(string email)
        {
            var user = User.Create(
                email: email,
                passwordHash: _passwordHasher.HashPassword("Password123!"),
                fullName: "Test User"
            );
            user.IsEmailVerified = true;

            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, RoleName = "USER", RoleDescription = "Standard User" };
            user.UserRoles = new List<UserRole> { new() { UserId = user.Id, RoleId = roleId, Role = role } };

            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            var token = GenerateJwtToken(user.Id.ToString(), user.Email);
            return (user, token);
        }

        private async Task<Trip> SeedTripAsync(Guid userId, DateTime? startDate, DateTime? endDate)
        {
            var trip = Trip.Create(
                userId: userId,
                title: "Test Trip",
                description: "Test Description",
                startDate: startDate,
                endDate: endDate,
                isPublic: false
            );

            _dbContext.Trips.Add(trip);

            var tripMember = TripMember.Create(
                tripId: trip.Id,
                userId: userId,
                ownerId: userId,
                role: "owner"
            );

            _dbContext.TripMembers.Add(tripMember);
            await _dbContext.SaveChangesAsync();

            return trip;
        }

        private async Task<TripDay> SeedTripDayAsync(Guid tripId, int dayIndex, DateTime dayDate, string? title)
        {
            var tripDay = TripDay.Create(tripId, title, dayDate, dayIndex);
            _dbContext.TripDays.Add(tripDay);
            await _dbContext.SaveChangesAsync();

            return tripDay;
        }

        private async Task<Location> SeedLocationAsync(string locationName)
        {
            var location = Location.Create(
                name: locationName,
                description: "Test Description",
                address: "Test Address",
                latitude: 35.6762,
                longitude: 139.6503,
                cityId: null,
                categoryId: null,
                isVerified: true
            );

            _dbContext.Locations.Add(location);
            await _dbContext.SaveChangesAsync();

            return location;
        }

        private async Task<TripLocationEntity> SeedTripLocationAsync(Guid tripDayId, Guid locationId, int orderIndex)
        {
            var tripLocation = TripLocationEntity.Create(
                tripDayId: tripDayId,
                locationId: locationId,
                orderIndex: orderIndex
            );

            _dbContext.TripLocations.Add(tripLocation);
            await _dbContext.SaveChangesAsync();

            return tripLocation;
        }

        private string GenerateJwtToken(string userId, string email)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("DayLaMotCaiKeyBiMatDaiDeTest123456789"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("userId", userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "VivuTest",
                audience: "VivuUser",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void SetAuth(string token)
            => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        private void ClearAuth()
            => _client.DefaultRequestHeaders.Authorization = null;

        private record ApiResponse<T>(bool Success, T? Data, int StatusCode = 200,
            string? Message = null, string? Code = null);

        private record TripDayResponse
        {
            public Guid Id { get; set; }
            public Guid TripId { get; set; }
            public string? Title { get; set; }
            public DateTime DayDate { get; set; }
            public int DateIndex { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        #endregion
    }
}

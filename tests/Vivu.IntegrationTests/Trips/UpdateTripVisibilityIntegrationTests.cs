using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.UseCases.Trips.Commands.UpdateTripVisibility;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Vivu.IntegrationTests.Trips
{
    [Collection("Integration Tests")]
    public class UpdateTripVisibilityIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;

        public UpdateTripVisibilityIntegrationTests(IntegrationTestWebAppFactory factory)
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
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            // Use raw SQL to avoid EF Core change tracker concurrency issues
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"TripDays\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"TripMembers\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"Trips\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"UserProfiles\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"Users\"");
        }
        
        private void SetAuthorizationHeader(string token)
        {
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
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

        #region Helper Methods

        private async Task<(User user, string token)> SeedTestUserWithTokenAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VivuDbContext>();

            var user = User.Create(
                email: email,
                passwordHash: BCrypt.Net.BCrypt.HashPassword("Password123!"),
                fullName: "Test User"
            );

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var token = GenerateJwtToken(user.Id.ToString(), user.Email);
            return (user, token);
        }

        private async Task<Trip> SeedTripAsync(Guid userId, string title, bool isPublic = false, bool isComplete = true)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VivuDbContext>();

            var trip = Trip.Create(
                userId: userId,
                title: title,
                description: isComplete ? "Complete Description" : null,
                startDate: isComplete ? DateTime.UtcNow.AddDays(5) : (DateTime?)null,
                endDate: isComplete ? DateTime.UtcNow.AddDays(10) : (DateTime?)null,
                isPublic: isPublic
            );

            dbContext.Trips.Add(trip);

            var tripMember = new Domain.Entities.TripMember
            {
                TripId = trip.Id,
                UserId = userId,
                OwnerId = userId,
                Role = "organizer",
                JoinedAt = DateTime.UtcNow
            };

            dbContext.TripMembers.Add(tripMember);
            await dbContext.SaveChangesAsync();

            return trip;
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task UpdateTripVisibility_MakePublic_ReturnsOkWithPublicTrip()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("makepublic@example.com");
            var trip = await SeedTripAsync(user.Id, "Private Trip", isPublic: false, isComplete: true);
            SetAuthorizationHeader(token);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            
            var result = apiResponse.Data;
            result.Should().NotBeNull();
            result!.IsPublic.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateTripVisibility_MakePrivate_ReturnsOkWithPrivateTrip()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("makeprivate@example.com");
            var trip = await SeedTripAsync(user.Id, "Public Trip", isPublic: true, isComplete: true);
            SetAuthorizationHeader(token);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = false
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            
            var result = apiResponse.Data;
            result.Should().NotBeNull();
            result!.IsPublic.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateTripVisibility_ToggleMultipleTimes_WorksCorrectly()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("toggle@example.com");
            var trip = await SeedTripAsync(user.Id, "Toggle Trip", isPublic: false, isComplete: true);
            SetAuthorizationHeader(token);

            // Act & Assert - Make Public
            var makePublicCommand = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = true
            };
            var response1 = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", makePublicCommand);
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse1 = await response1.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse1!.Data!.IsPublic.Should().BeTrue();

            // Act & Assert - Make Private
            var makePrivateCommand = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = false
            };
            var response2 = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", makePrivateCommand);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse2 = await response2.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse2!.Data!.IsPublic.Should().BeFalse();

            // Act & Assert - Make Public Again
            var response3 = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", makePublicCommand);
            response3.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse3 = await response3.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse3!.Data!.IsPublic.Should().BeTrue();
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task UpdateTripVisibility_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var command = new UpdateTripVisibilityCommand
            {
                TripId = Guid.NewGuid(),
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{command.TripId}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateTripVisibility_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            SetAuthorizationHeader("invalid.token.here");
            var command = new UpdateTripVisibilityCommand
            {
                TripId = Guid.NewGuid(),
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{command.TripId}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task UpdateTripVisibility_NotOwner_ReturnsForbidden()
        {
            // Arrange
            var (owner, _) = await SeedTestUserWithTokenAsync("owner@example.com");
            var (otherUser, otherToken) = await SeedTestUserWithTokenAsync("other@example.com");
            var trip = await SeedTripAsync(owner.Id, "Owner's Trip", isPublic: false, isComplete: true);
            SetAuthorizationHeader(otherToken);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region Not Found Tests

        [Fact]
        public async Task UpdateTripVisibility_NonExistentTrip_ReturnsNotFound()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("notfound@example.com");
            SetAuthorizationHeader(token);
            var nonExistentTripId = Guid.NewGuid();

            var command = new UpdateTripVisibilityCommand
            {
                TripId = nonExistentTripId,
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{nonExistentTripId}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task UpdateTripVisibility_EmptyTripId_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("emptyid@example.com");
            SetAuthorizationHeader(token);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = Guid.Empty,
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{command.TripId}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateTripVisibility_IncompleteTripMadePublic_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("incomplete@example.com");
            var trip = await SeedTripAsync(user.Id, "Incomplete Trip", isPublic: false, isComplete: false);
            SetAuthorizationHeader(token);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", command);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateTripVisibility_IncompleteTripMadePrivate_Succeeds()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("incompleteprivate@example.com");
            var trip = await SeedTripAsync(user.Id, "Incomplete Trip", isPublic: false, isComplete: false);
            SetAuthorizationHeader(token);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = false
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.IsPublic.Should().BeFalse();
        }

        #endregion

        #region Deleted Trip Tests

        [Fact]
        public async Task UpdateTripVisibility_DeletedTrip_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("deleted@example.com");
            
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            
            var trip = Trip.Create(
                userId: user.Id,
                title: "Trip to be deleted",
                description: "Description",
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(10),
                isPublic: false
            );
            
            dbContext.Trips.Add(trip);
            
            var tripMember = new Domain.Entities.TripMember
            {
                TripId = trip.Id,
                UserId = user.Id,
                OwnerId = user.Id,
                Role = "organizer",
                JoinedAt = DateTime.UtcNow
            };
            dbContext.TripMembers.Add(tripMember);
            
            await dbContext.SaveChangesAsync();
            
            // Delete the trip
            trip.Delete();
            await dbContext.SaveChangesAsync();
            
            SetAuthorizationHeader(token);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", command);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task UpdateTripVisibility_AlreadyPublic_MakePublicAgain_Succeeds()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("alreadypublic@example.com");
            var trip = await SeedTripAsync(user.Id, "Already Public Trip", isPublic: true, isComplete: true);
            SetAuthorizationHeader(token);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse!.Data!.IsPublic.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateTripVisibility_AlreadyPrivate_MakePrivateAgain_Succeeds()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("alreadyprivate@example.com");
            var trip = await SeedTripAsync(user.Id, "Already Private Trip", isPublic: false, isComplete: true);
            SetAuthorizationHeader(token);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = false
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse!.Data!.IsPublic.Should().BeFalse();
        }

        #endregion

        #region Database Verification Tests

        [Fact]
        public async Task UpdateTripVisibility_DatabasePersistsChanges_CorrectlyUpdated()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("dbpersist@example.com");
            var trip = await SeedTripAsync(user.Id, "Persist Trip", isPublic: false, isComplete: true);
            SetAuthorizationHeader(token);

            var command = new UpdateTripVisibilityCommand
            {
                TripId = trip.Id,
                IsPublic = true
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Trips/{trip.Id}/visibility", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify in database
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            var updatedTrip = await dbContext.Trips.FindAsync(trip.Id);
            
            updatedTrip.Should().NotBeNull();
            updatedTrip!.IsPublic.Should().BeTrue();
        }

        #endregion
    }
}

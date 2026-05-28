using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.UseCases.Trips.Commands.UpdateTrip;
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
    public class UpdateTripIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;

        public UpdateTripIntegrationTests(IntegrationTestWebAppFactory factory)
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

        private async Task<Trip> SeedTripAsync(Guid userId, string title, string status = "planning")
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VivuDbContext>();

            var trip = Trip.Create(
                userId: userId,
                title: title,
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(10),
                isPublic: false
            );

            typeof(Trip).GetProperty("Status")!.SetValue(trip, status);

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

        private async Task<TripDay> SeedTripDayAsync(Guid tripId, int dayIndex, DateTime dayDate)
        {
            var tripDay = TripDay.Create(tripId, $"Day {dayIndex}", dayDate, dayIndex);
            _dbContext.TripDays.Add(tripDay);
            await _dbContext.SaveChangesAsync();

            return tripDay;
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task UpdateTrip_ValidRequest_ReturnsOkWithUpdatedTrip()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("updatetrip@example.com");
            var trip = await SeedTripAsync(user.Id, "Original Title");
            SetAuthorizationHeader(token);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = "Updated Title",
                Description = "Updated Description",
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(12),
                Status = "ongoing"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            
            var result = apiResponse.Data;
            result.Should().NotBeNull();
            result!.Title.Should().Be("Updated Title");
            result.Description.Should().Be("Updated Description");
            result.Status.Should().Be("ongoing");
        }

        [Fact]
        public async Task UpdateTrip_PartialUpdate_OnlyUpdatesProvidedFields()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("partialupdate@example.com");
            var trip = await SeedTripAsync(user.Id, "Original Title", "planning");
            SetAuthorizationHeader(token);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = "Only Title Changed",
                Status = "planning" // Keep original status
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            
            var result = apiResponse.Data;
            result.Should().NotBeNull();
            result!.Title.Should().Be("Only Title Changed");
            result.Status.Should().Be("planning");
        }

        [Theory]
        [InlineData("planning")]
        [InlineData("ongoing")]
        [InlineData("completed")]
        public async Task UpdateTrip_ChangeStatus_UpdatesSuccessfully(string newStatus)
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync($"changestatus_{newStatus}@example.com");
            var trip = await SeedTripAsync(user.Id, "Test Trip", "planning");
            SetAuthorizationHeader(token);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = trip.Title,
                Status = newStatus
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponse>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            
            var result = apiResponse.Data;
            result!.Status.Should().Be(newStatus);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task UpdateTrip_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var updateCommand = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test",
                Status = "planning"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{updateCommand.TripId}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateTrip_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            SetAuthorizationHeader("invalid.token.here");
            var updateCommand = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test",
                Status = "planning"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{updateCommand.TripId}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task UpdateTrip_NotOwner_ReturnsForbidden()
        {
            // Arrange
            var (owner, _) = await SeedTestUserWithTokenAsync("owner@example.com");
            var (otherUser, otherToken) = await SeedTestUserWithTokenAsync("other@example.com");
            var trip = await SeedTripAsync(owner.Id, "Owner's Trip");
            SetAuthorizationHeader(otherToken);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = "Trying to update",
                Status = "planning"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region Not Found Tests

        [Fact]
        public async Task UpdateTrip_NonExistentTrip_ReturnsNotFound()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("notfound@example.com");
            SetAuthorizationHeader(token);
            var nonExistentTripId = Guid.NewGuid();

            var updateCommand = new UpdateTripCommand
            {
                TripId = nonExistentTripId,
                Title = "Test",
                Status = "planning"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{nonExistentTripId}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task UpdateTrip_EmptyTitle_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("emptytitle@example.com");
            var trip = await SeedTripAsync(user.Id, "Original Title");
            SetAuthorizationHeader(token);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = string.Empty,
                Status = "planning"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateTrip_TitleTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("longtitle@example.com");
            var trip = await SeedTripAsync(user.Id, "Original Title");
            SetAuthorizationHeader(token);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = new string('a', 201),
                Status = "planning"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateTrip_InvalidStatus_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("invalidstatus@example.com");
            var trip = await SeedTripAsync(user.Id, "Test Trip");
            SetAuthorizationHeader(token);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = "Test",
                Status = "invalid_status"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateTrip_InvalidDateRange_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("invaliddate@example.com");
            var trip = await SeedTripAsync(user.Id, "Test Trip");
            SetAuthorizationHeader(token);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = "Test",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(5), // End before start
                Status = "planning"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region TripDay Sync Tests

        [Fact]
        public async Task UpdateTrip_ExtendDates_CreatesAdditionalTripDays()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("extendtrip@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var oldEndDate = DateTime.UtcNow.AddDays(7).Date; // 3 days
            var newEndDate = DateTime.UtcNow.AddDays(10).Date; // 6 days

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VivuDbContext>();

            var trip = Trip.Create(
                userId: user.Id,
                title: "Test Trip",
                description: "Desc",
                startDate: startDate,
                endDate: oldEndDate,
                isPublic: false
            );
            _dbContext.Trips.Add(trip);
            
            var tripMember = new Domain.Entities.TripMember
            {
                TripId = trip.Id,
                UserId = user.Id,
                OwnerId = user.Id,
                Role = "organizer",
                JoinedAt = DateTime.UtcNow
            };
            _dbContext.TripMembers.Add(tripMember);

            // Create initial 3 TripDays
            for (int i = 1; i <= 3; i++)
            {
                var tripDay = TripDay.Create(trip.Id, $"Day {i}", startDate.AddDays(i - 1), i);
                _dbContext.TripDays.Add(tripDay);
            }

            await _dbContext.SaveChangesAsync();

            SetAuthorizationHeader(token);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = "Extended Trip",
                StartDate = startDate,
                EndDate = newEndDate,
                Status = "planning"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify TripDays count increased
            var tripDays = dbContext.TripDays.Where(td => td.TripId == trip.Id).ToList();
            tripDays.Should().HaveCount(6); // Should now have 6 days
        }

        [Fact]
        public async Task UpdateTrip_ShortenDates_RemovesExcessTripDays()
        {
            // Arrange
            var (user, token) = await SeedTestUserWithTokenAsync("shortentrip@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var oldEndDate = DateTime.UtcNow.AddDays(10).Date; // 6 days
            var newEndDate = DateTime.UtcNow.AddDays(7).Date; // 3 days

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VivuDbContext>();

            var trip = Trip.Create(
                userId: user.Id,
                title: "Test Trip",
                description: "Desc",
                startDate: startDate,
                endDate: oldEndDate,
                isPublic: false
            );
            _dbContext.Trips.Add(trip);
            
            var tripMember = new Domain.Entities.TripMember
            {
                TripId = trip.Id,
                UserId = user.Id,
                OwnerId = user.Id,
                Role = "organizer",
                JoinedAt = DateTime.UtcNow
            };
            _dbContext.TripMembers.Add(tripMember);

            // Create 6 TripDays
            for (int i = 1; i <= 6; i++)
            {
                var tripDay = TripDay.Create(trip.Id, $"Day {i}", startDate.AddDays(i - 1), i);
                _dbContext.TripDays.Add(tripDay);
            }

            await _dbContext.SaveChangesAsync();

            SetAuthorizationHeader(token);

            var updateCommand = new UpdateTripCommand
            {
                TripId = trip.Id,
                Title = "Shortened Trip",
                StartDate = startDate,
                EndDate = newEndDate,
                Status = "planning"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Trips/{trip.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify TripDays count decreased
            var tripDays = dbContext.TripDays.Where(td => td.TripId == trip.Id).ToList();
            tripDays.Should().HaveCount(3); // Should now have 3 days
        }

        #endregion
    }

    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("data")]
        public T? Data { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class TripResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }
        
        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("isPublic")]
        public bool IsPublic { get; set; }
    }
}

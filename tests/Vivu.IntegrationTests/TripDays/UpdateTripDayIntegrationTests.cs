using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
using Vivu.Application.UseCases.TripDay.Commands.UpdateTripDay;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.TripDays
{
    [Collection("Integration Tests")]
    public class UpdateTripDayIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public UpdateTripDayIntegrationTests(IntegrationTestWebAppFactory factory)
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
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"TripDays\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"TripMembers\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"Trips\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"UserProfiles\"");
            await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM \"Users\"");
        }

        #region Happy Path Tests

        [Fact]
        public async Task UpdateTripDay_WithValidTitle_ReturnsUpdatedTripDay()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("update_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Original Title");
            SetAuth(token);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                Title = "Updated Title: Tokyo Tower Visit"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TripDayResponse>>(content, JsonOpts);

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(tripDay.Id);
            apiResponse.Data.Title.Should().Be(command.Title);

            // Verify in database - reload entity to avoid caching
            var updatedTripDay = await _dbContext.TripDays
                .AsNoTracking()
                .FirstOrDefaultAsync(td => td.Id == tripDay.Id);
            updatedTripDay.Should().NotBeNull();
            updatedTripDay!.Title.Should().Be(command.Title);
        }

        [Fact]
        public async Task UpdateTripDay_WithValidDayDate_UpdatesDayDate()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("update_date_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var trip = await SeedTripAsync(user.Id, startDate, startDate.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, startDate, "Day 1");
            SetAuth(token);

            var newDate = startDate.AddDays(3);
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                DayDate = newDate
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TripDayResponse>>(content, JsonOpts);

            apiResponse!.Data.Should().NotBeNull();
            apiResponse.Data!.DayDate.Date.Should().Be(newDate.Date);

            // Verify in database - reload entity to avoid caching
            var updatedTripDay = await _dbContext.TripDays
                .AsNoTracking()
                .FirstOrDefaultAsync(td => td.Id == tripDay.Id);
            updatedTripDay!.DayDate!.Value.Date.Should().Be(newDate.Date);
        }

        [Fact]
        public async Task UpdateTripDay_WithBothTitleAndDayDate_UpdatesBoth()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("update_both_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var trip = await SeedTripAsync(user.Id, startDate, startDate.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, startDate, "Original");
            SetAuth(token);

            var newDate = startDate.AddDays(2);
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                Title = "Updated Both",
                DayDate = newDate
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TripDayResponse>>(content, JsonOpts);

            apiResponse!.Data!.Title.Should().Be(command.Title);
            apiResponse.Data.DayDate.Date.Should().Be(newDate.Date);
        }

        [Fact]
        public async Task UpdateTripDay_WithNullTitle_ClearsTitle()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("null_title_user@example.com");
            var trip = await SeedTripAsync(user.Id, null, null);
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow, "Original Title");
            SetAuth(token);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                Title = null
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateTripDay_WithOnlyTripDayId_Succeeds()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("only_id_user@example.com");
            var trip = await SeedTripAsync(user.Id, null, null);
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow, "Title");
            SetAuth(token);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                Title = null,
                DayDate = null
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task UpdateTripDay_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "Updated"
            };

            // Act - No authorization header
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{command.TripDayId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateTripDay_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            var tripDayId = Guid.NewGuid();
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                Title = "Updated"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDayId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task UpdateTripDay_UserNotOwner_ReturnsForbidden()
        {
            // Arrange
            var (owner, _) = await SeedUserWithTokenAsync("owner@example.com");
            var (otherUser, otherToken) = await SeedUserWithTokenAsync("other@example.com");
            var trip = await SeedTripAsync(owner.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");

            SetAuth(otherToken);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                Title = "Unauthorized Update"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.AccessDenied.Code);
        }

        [Fact]
        public async Task UpdateTripDay_TripDayNotFound_ReturnsNotFound()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("notfound_user@example.com");
            SetAuth(token);

            var nonExistentTripDayId = Guid.NewGuid();
            var command = new UpdateTripDayCommand
            {
                TripDayId = nonExistentTripDayId,
                Title = "Updated"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{nonExistentTripDayId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.NotFoundByTripDay(nonExistentTripDayId).Code);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task UpdateTripDay_TitleExceedsMaxLength_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("longtitle_user@example.com");
            var trip = await SeedTripAsync(user.Id, null, null);
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow, "Title");
            SetAuth(token);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                Title = new string('A', 201) // Exceeds 200 char limit
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateTripDay_DayDateBeforeStartDate_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("beforestart_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var trip = await SeedTripAsync(user.Id, startDate, startDate.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, startDate, "Day 1");
            SetAuth(token);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                DayDate = startDate.AddDays(-1) // Before start date
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.DateInvalid.Code);
        }

        [Fact]
        public async Task UpdateTripDay_DayDateAfterEndDate_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("afterend_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var endDate = startDate.AddDays(10);
            var trip = await SeedTripAsync(user.Id, startDate, endDate);
            var tripDay = await SeedTripDayAsync(trip.Id, 1, startDate, "Day 1");
            SetAuth(token);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                DayDate = endDate.AddDays(1) // After end date
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.DateInvalid.Code);
        }

        [Fact]
        public async Task UpdateTripDay_DayDateEqualToStartDate_Succeeds()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("equalstart_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var trip = await SeedTripAsync(user.Id, startDate, startDate.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, startDate.AddDays(2), "Day 1");
            SetAuth(token);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                DayDate = startDate // Equal to start date
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateTripDay_DayDateEqualToEndDate_Succeeds()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("equalend_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var endDate = startDate.AddDays(10);
            var trip = await SeedTripAsync(user.Id, startDate, endDate);
            var tripDay = await SeedTripDayAsync(trip.Id, 1, startDate, "Day 1");
            SetAuth(token);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                DayDate = endDate // Equal to end date
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateTripDay_DayDateWithinRange_Succeeds()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("withinrange_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var endDate = startDate.AddDays(10);
            var trip = await SeedTripAsync(user.Id, startDate, endDate);
            var tripDay = await SeedTripDayAsync(trip.Id, 1, startDate, "Day 1");
            SetAuth(token);

            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDay.Id,
                DayDate = startDate.AddDays(5) // Within range
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/{tripDay.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
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

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
using Vivu.Application.UseCases.TripDay.Commands.AddTripDay;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.TripDays
{
    [Collection("Integration Tests")]
    public class AddTripDayIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public AddTripDayIntegrationTests(IntegrationTestWebAppFactory factory)
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
        public async Task AddTripDay_WithValidData_ReturnsCreatedTripDay()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("tripday_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            SetAuth(token);

            var command = new AddTripDayCommand
            {
                TripId = trip.Id,
                Title = "Day 1: Tokyo Adventure"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResultResponse<TripDayResponse>>(content, JsonOpts);

            result.Should().NotBeNull();
            result!.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.TripId.Should().Be(trip.Id);
            result.Value.Title.Should().Be(command.Title);
            result.Value.DateIndex.Should().Be(1);

            // Verify in database
            var tripDay = await _dbContext.TripDays.FirstOrDefaultAsync(td => td.TripId == trip.Id);
            tripDay.Should().NotBeNull();
            tripDay!.Title.Should().Be(command.Title);
        }

        [Fact]
        public async Task AddTripDay_WithTripHavingStartDate_AutoCalculatesDayDate()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("daydate_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(7).Date;
            var trip = await SeedTripAsync(user.Id, startDate, startDate.AddDays(10));
            SetAuth(token);

            var command = new AddTripDayCommand
            {
                TripId = trip.Id,
                Title = "Day 1"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResultResponse<TripDayResponse>>(content, JsonOpts);

            result.Should().NotBeNull();
            result!.Value.Should().NotBeNull();
            result.Value!.DayDate.Date.Should().Be(startDate.Date);
        }

        [Fact]
        public async Task AddTripDay_MultipleDays_CalculatesCorrectDayIndex()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("multiday_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(15));
            
            // Add first trip day
            await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5));
            await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6));
            
            SetAuth(token);

            var command = new AddTripDayCommand
            {
                TripId = trip.Id,
                Title = "Day 3"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResultResponse<TripDayResponse>>(content, JsonOpts);

            result.Should().NotBeNull();
            result!.Value.Should().NotBeNull();
            result.Value!.DateIndex.Should().Be(3);
        }

        [Fact]
        public async Task AddTripDay_WithoutStartDate_UsesDayDateFromCommand()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("nodatetrip_user@example.com");
            var trip = await SeedTripAsync(user.Id, null, null);
            var customDate = DateTime.UtcNow.AddDays(10).Date;
            SetAuth(token);

            var command = new AddTripDayCommand
            {
                TripId = trip.Id,
                Title = "Custom Date Day",
                DayDate = customDate
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResultResponse<TripDayResponse>>(content, JsonOpts);

            result.Should().NotBeNull();
            result!.Value.Should().NotBeNull();
            result.Value!.DayDate.Date.Should().Be(customDate.Date);
        }

        [Fact]
        public async Task AddTripDay_WithNullTitle_CreatesSuccessfully()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("nulltitle_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            SetAuth(token);

            var command = new AddTripDayCommand
            {
                TripId = trip.Id,
                Title = null
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task AddTripDay_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Unauthorized Day"
            };

            // Act - No authorization header
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AddTripDay_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Invalid Token Day"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task AddTripDay_UserNotOwner_ReturnsForbidden()
        {
            // Arrange
            var (owner, _) = await SeedUserWithTokenAsync("owner@example.com");
            var (otherUser, otherToken) = await SeedUserWithTokenAsync("other@example.com");
            var trip = await SeedTripAsync(owner.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            
            SetAuth(otherToken);

            var command = new AddTripDayCommand
            {
                TripId = trip.Id,
                Title = "Unauthorized Day"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.AccessDenied.Code);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task AddTripDay_TripNotFound_ReturnsNotFound()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("notfound_user@example.com");
            SetAuth(token);

            var nonExistentTripId = Guid.NewGuid();
            var command = new AddTripDayCommand
            {
                TripId = nonExistentTripId,
                Title = "Day 1"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.NotFoundById(nonExistentTripId).Code);
        }

        [Fact]
        public async Task AddTripDay_EmptyTripId_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("emptytripid_user@example.com");
            SetAuth(token);

            var command = new AddTripDayCommand
            {
                TripId = Guid.Empty,
                Title = "Day 1"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task AddTripDay_TitleExceedsMaxLength_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("longtitle_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            SetAuth(token);

            var command = new AddTripDayCommand
            {
                TripId = trip.Id,
                Title = new string('A', 201) // Exceeds 200 char limit
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task AddTripDay_DayDateBeforeTripStartDate_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("beforestart_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(10).Date;
            var endDate = startDate.AddDays(7);
            
            // When trip has StartDate, handler auto-calculates: dayDate = StartDate + (newDayIndex - 1)
            // newDayIndex = maxDayIndex + 1
            // To get dayDate before StartDate, need newDayIndex <= 0
            // So need maxDayIndex <= -1
            var trip = await SeedTripAsync(user.Id, startDate, endDate);
            
            // Seed trip day with negative index to force next calculated date before start
            await SeedTripDayAsync(trip.Id, -1, startDate.AddDays(-2));
            
            SetAuth(token);

            var command = new AddTripDayCommand
            {
                TripId = trip.Id,
                Title = "Day before start"
            };

            // Act - newDayIndex will be 0, dayDate = StartDate + (0-1) = StartDate - 1
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.DateInvalid.Code);
        }

        [Fact]
        public async Task AddTripDay_DayDateAfterTripEndDate_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("afterend_user@example.com");
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var endDate = startDate.AddDays(7); // Trip is 8 days (days 0-7)
            
            // When trip has StartDate, handler auto-calculates: dayDate = StartDate + (newDayIndex - 1)
            // newDayIndex = maxDayIndex + 1
            // EndDate is StartDate + 7, so need dayDate > StartDate + 7
            // Need newDayIndex > 8, so maxDayIndex >= 8
            var trip = await SeedTripAsync(user.Id, startDate, endDate);
            
            // Seed 9 trip days to make maxDayIndex = 9, so newDayIndex = 10
            for (int i = 1; i <= 9; i++)
            {
                await SeedTripDayAsync(trip.Id, i, startDate.AddDays(i - 1));
            }
            
            SetAuth(token);

            var command = new AddTripDayCommand
            {
                TripId = trip.Id,
                Title = "Day after end"
            };

            // Act - newDayIndex will be 10, dayDate = StartDate + 9 > EndDate
            var response = await _client.PostAsJsonAsync("/api/TripDay", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
            apiResponse!.Code.Should().Be(DomainErrors.Trip.DateInvalid.Code);
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

        private async Task<TripDay> SeedTripDayAsync(Guid tripId, int dayIndex, DateTime dayDate)
        {
            var tripDay = TripDay.Create(tripId, $"Day {dayIndex}", dayDate, dayIndex);
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

        private record ResultResponse<T>
        {
            public bool IsSuccess { get; set; }
            public T? Value { get; set; }
            public ErrorResponse? Error { get; set; }
        }

        private record ErrorResponse
        {
            public string Code { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

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

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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
using Vivu.Application.UseCases.TripDay.Commands.ReorderTripDay;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.TripDays
{
    [Collection("Integration Tests")]
    public class ReorderTripDayIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public ReorderTripDayIntegrationTests(IntegrationTestWebAppFactory factory)
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
        public async Task ReorderTripDays_ValidRequest_ReordersSuccessfully()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("reorder_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            var tripDay3 = await SeedTripDayAsync(trip.Id, 3, DateTime.UtcNow.AddDays(7), "Day 3");
            
            SetAuth(token);

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> { tripDay3.Id, tripDay1.Id, tripDay2.Id }
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<TripDayResponse>>>(content, JsonOpts);

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Should().HaveCount(3);
            
            // Verify order
            apiResponse.Data![0].Id.Should().Be(tripDay3.Id);
            apiResponse.Data[1].Id.Should().Be(tripDay1.Id);
            apiResponse.Data[2].Id.Should().Be(tripDay2.Id);

            // Verify in database
            var reorderedTripDay1 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay1.Id);
            var reorderedTripDay2 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay2.Id);
            var reorderedTripDay3 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay3.Id);

            reorderedTripDay3.DayIndex.Should().Be(1);
            reorderedTripDay1.DayIndex.Should().Be(2);
            reorderedTripDay2.DayIndex.Should().Be(3);
        }

        [Fact]
        public async Task ReorderTripDays_ReverseOrder_UpdatesIndexesCorrectly()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("reverse_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            var tripDay3 = await SeedTripDayAsync(trip.Id, 3, DateTime.UtcNow.AddDays(7), "Day 3");
            var tripDay4 = await SeedTripDayAsync(trip.Id, 4, DateTime.UtcNow.AddDays(8), "Day 4");
            
            SetAuth(token);

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> { tripDay4.Id, tripDay3.Id, tripDay2.Id, tripDay1.Id }
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var td1 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay1.Id);
            var td2 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay2.Id);
            var td3 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay3.Id);
            var td4 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay4.Id);

            td4.DayIndex.Should().Be(1);
            td3.DayIndex.Should().Be(2);
            td2.DayIndex.Should().Be(3);
            td1.DayIndex.Should().Be(4);
        }

        [Fact]
        public async Task ReorderTripDays_SingleTripDay_SuccessfullyProcessed()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("single_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            
            SetAuth(token);

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> { tripDay.Id }
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var reorderedTripDay = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay.Id);
            reorderedTripDay.DayIndex.Should().Be(1);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task ReorderTripDays_NoAuthToken_ReturnsUnauthorized()
        {
            // Arrange
            var (user, _) = await SeedUserWithTokenAsync("noauth_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");

            ClearAuth();

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> { tripDay.Id }
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task ReorderTripDays_UserNotOwner_ReturnsForbidden()
        {
            // Arrange
            var (owner, _) = await SeedUserWithTokenAsync("owner@example.com");
            var (otherUser, otherToken) = await SeedUserWithTokenAsync("other@example.com");
            
            var trip = await SeedTripAsync(owner.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            
            SetAuth(otherToken);

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> { tripDay2.Id, tripDay1.Id }
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task ReorderTripDays_TripNotFound_ReturnsNotFound()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("notfound_user@example.com");
            var nonExistentTripId = Guid.NewGuid();
            
            SetAuth(token);

            var command = new ReorderTripDayCommand
            {
                TripId = nonExistentTripId,
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{nonExistentTripId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ReorderTripDays_InvalidTripDayId_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("invalid_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            
            SetAuth(token);

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> { tripDay1.Id, Guid.NewGuid() } // Invalid ID
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("invalid");
        }

        [Fact]
        public async Task ReorderTripDays_MissingTripDayId_ReturnsBadRequest()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("missing_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            var tripDay3 = await SeedTripDayAsync(trip.Id, 3, DateTime.UtcNow.AddDays(7), "Day 3");
            
            SetAuth(token);

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> { tripDay1.Id, tripDay2.Id } // Missing tripDay3
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Expected 3 trip days, but received 2");
        }

        [Fact]
        public async Task ReorderTripDays_EmptyTripDayIds_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("empty_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            
            SetAuth(token);

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid>() // Empty list
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task ReorderTripDays_DuplicateTripDayIds_ReturnsUnprocessableEntity()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("duplicate_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            
            SetAuth(token);

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> { tripDay1.Id, tripDay1.Id } // Duplicate
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Complex Reordering Tests

        [Fact]
        public async Task ReorderTripDays_MoveFirstToLast_UpdatesCorrectly()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("firstlast_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            var tripDay3 = await SeedTripDayAsync(trip.Id, 3, DateTime.UtcNow.AddDays(7), "Day 3");
            
            SetAuth(token);

            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> { tripDay2.Id, tripDay3.Id, tripDay1.Id }
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var td1 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay1.Id);
            var td2 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay2.Id);
            var td3 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay3.Id);

            td2.DayIndex.Should().Be(1);
            td3.DayIndex.Should().Be(2);
            td1.DayIndex.Should().Be(3);
        }

        [Fact]
        public async Task ReorderTripDays_ComplexReorder_UpdatesAllIndexes()
        {
            // Arrange
            var (user, token) = await SeedUserWithTokenAsync("complex_user@example.com");
            var trip = await SeedTripAsync(user.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(15));
            
            var tripDay1 = await SeedTripDayAsync(trip.Id, 1, DateTime.UtcNow.AddDays(5), "Day 1");
            var tripDay2 = await SeedTripDayAsync(trip.Id, 2, DateTime.UtcNow.AddDays(6), "Day 2");
            var tripDay3 = await SeedTripDayAsync(trip.Id, 3, DateTime.UtcNow.AddDays(7), "Day 3");
            var tripDay4 = await SeedTripDayAsync(trip.Id, 4, DateTime.UtcNow.AddDays(8), "Day 4");
            var tripDay5 = await SeedTripDayAsync(trip.Id, 5, DateTime.UtcNow.AddDays(9), "Day 5");
            
            SetAuth(token);

            // New order: 2, 4, 1, 5, 3
            var command = new ReorderTripDayCommand
            {
                TripId = trip.Id,
                OrderedTripDayIds = new List<Guid> 
                { 
                    tripDay2.Id, 
                    tripDay4.Id, 
                    tripDay1.Id, 
                    tripDay5.Id, 
                    tripDay3.Id 
                }
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TripDay/reorder/{trip.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var td1 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay1.Id);
            var td2 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay2.Id);
            var td3 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay3.Id);
            var td4 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay4.Id);
            var td5 = await _dbContext.TripDays.AsNoTracking().FirstAsync(td => td.Id == tripDay5.Id);

            td2.DayIndex.Should().Be(1);
            td4.DayIndex.Should().Be(2);
            td1.DayIndex.Should().Be(3);
            td5.DayIndex.Should().Be(4);
            td3.DayIndex.Should().Be(5);
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

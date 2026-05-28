using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using TripMemberEntity = Vivu.Domain.Entities.TripMember;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.TripMembers
{
    [Collection("Integration Tests")]
    public class GetTripMembersIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _testUser = null!;
        private string _accessToken = string.Empty;

        public GetTripMembersIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedTestUserAsync("tripmember@example.com", "Password123!");
            _accessToken = await GetAccessTokenAsync("tripmember@example.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<TripMemberEntity>().ExecuteDeleteAsync();
            await _dbContext.Set<TripDay>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Success Scenarios

        [Fact]
        public async Task GetTripMembers_AsOwner_ReturnsMembers()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Test Trip");
            var member1 = await SeedUserAsync("member1@example.com");
            var member2 = await SeedUserAsync("member2@example.com");
            await SeedTripMemberAsync(trip.Id, member1.Id, _testUser.Id, "viewer");
            await SeedTripMemberAsync(trip.Id, member2.Id, _testUser.Id, "editor");

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<TripMemberDto>>>(jsonString, options);

            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            apiResponse.Data.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetTripMembers_AsMember_ReturnsMembers()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Test Trip");
            await SeedTripMemberAsync(trip.Id, _testUser.Id, owner.Id, "viewer");
            var member = await SeedUserAsync("member@example.com");
            await SeedTripMemberAsync(trip.Id, member.Id, owner.Id, "editor");

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<TripMemberDto>>>(jsonString, options);

            apiResponse!.Data.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetTripMembers_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Test Trip");
            for (int i = 1; i <= 15; i++)
            {
                var member = await SeedUserAsync($"member{i}@example.com");
                await SeedTripMemberAsync(trip.Id, member.Id, _testUser.Id, "viewer");
            }

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<TripMemberDto>>>(jsonString, options);

            apiResponse!.Data.Items.Should().HaveCount(10);
            apiResponse.Data.TotalCount.Should().Be(15);
            apiResponse.Data.PageNumber.Should().Be(1);
            apiResponse.Data.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetTripMembers_SecondPage_ReturnsRemainingMembers()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Test Trip");
            for (int i = 1; i <= 15; i++)
            {
                var member = await SeedUserAsync($"member{i}@example.com");
                await SeedTripMemberAsync(trip.Id, member.Id, _testUser.Id, "viewer");
            }

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}?pageNumber=2&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<TripMemberDto>>>(jsonString, options);

            apiResponse!.Data.Items.Should().HaveCount(5);
            apiResponse.Data.PageNumber.Should().Be(2);
        }

        [Fact]
        public async Task GetTripMembers_EmptyTrip_ReturnsEmptyList()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Empty Trip");

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<TripMemberDto>>>(jsonString, options);

            apiResponse!.Data.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        #endregion

        #region Authentication Failures

        [Fact]
        public async Task GetTripMembers_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Test Trip");
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetTripMembers_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Test Trip");
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authorization Failures

        [Fact]
        public async Task GetTripMembers_AsNonMember_ReturnsForbidden()
        {
            // Arrange
            var owner = await SeedUserAsync("owner@example.com");
            var trip = await SeedTripAsync(owner.Id, "Private Trip");

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region Validation Failures

        [Fact]
        public async Task GetTripMembers_TripNotFound_ReturnsNotFound()
        {
            // Arrange
            var nonExistentTripId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{nonExistentTripId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTripMembers_DeletedTrip_ReturnsBadRequest()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Deleted Trip");
            trip.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTripMembers_InvalidTripId_ReturnsBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/tripmembers/invalid-guid");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetTripMembers_WithDifferentRoles_ReturnsAllMembers()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Test Trip");
            var viewer = await SeedUserAsync("viewer@example.com");
            var editor = await SeedUserAsync("editor@example.com");
            await SeedTripMemberAsync(trip.Id, viewer.Id, _testUser.Id, "viewer");
            await SeedTripMemberAsync(trip.Id, editor.Id, _testUser.Id, "editor");

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<TripMemberDto>>>(jsonString, options);

            apiResponse!.Data.Items.Should().Contain(m => m.Role == "viewer");
            apiResponse.Data.Items.Should().Contain(m => m.Role == "editor");
        }

        [Fact]
        public async Task GetTripMembers_CustomPageSize_ReturnsCorrectCount()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Test Trip");
            for (int i = 1; i <= 10; i++)
            {
                var member = await SeedUserAsync($"member{i}@example.com");
                await SeedTripMemberAsync(trip.Id, member.Id, _testUser.Id, "viewer");
            }

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}?pageSize=5");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<TripMemberDto>>>(jsonString, options);

            apiResponse!.Data.Items.Should().HaveCount(5);
            apiResponse.Data.PageSize.Should().Be(5);
        }

        #endregion

        #region Database Verification

        [Fact]
        public async Task GetTripMembers_ReturnsCorrectMemberDetails()
        {
            // Arrange
            var trip = await SeedTripAsync(_testUser.Id, "Test Trip");
            var member = await SeedUserAsync("member@example.com");
            await SeedTripMemberAsync(trip.Id, member.Id, _testUser.Id, "viewer");

            // Act
            var response = await _client.GetAsync($"/api/tripmembers/{trip.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<TripMemberDto>>>(jsonString, options);

            var returnedMember = apiResponse!.Data.Items.FirstOrDefault(m => m.UserId == member.Id);
            returnedMember.Should().NotBeNull();
            returnedMember!.Email.Should().Be(member.Email);
            returnedMember.Role.Should().Be("viewer");
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

        private async Task<User> SeedUserAsync(string email)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword("Password123!"),
                fullName: $"User {email}",
                avatarUrl: null
            );

            user.IsEmailVerified = true;

            var userRole = await _dbContext.Set<Role>()
                .FirstOrDefaultAsync(r => r.RoleName == "User");

            if (userRole != null)
            {
                user.AssignRole(userRole.Id);
            }

            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        private async Task<Trip> SeedTripAsync(Guid ownerId, string title)
        {
            var trip = Trip.Create(
                userId: ownerId,
                title: title,
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(7),
                endDate: DateTime.UtcNow.AddDays(14),
                isPublic: false,
                inviteCode: null
            );

            _dbContext.Set<Trip>().Add(trip);
            await _dbContext.SaveChangesAsync();

            return trip;
        }

        private async Task<TripMemberEntity> SeedTripMemberAsync(Guid tripId, Guid userId, Guid ownerId, string role)
        {
            var tripMember = TripMemberEntity.Create(tripId, userId, ownerId, role);

            _dbContext.Set<TripMemberEntity>().Add(tripMember);
            await _dbContext.SaveChangesAsync();

            return tripMember;
        }

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var loginRequest = new LoginUserCommand
            {
                Email = email,
                Password = password,
                IpAddress = "127.0.0.1",
                DeviceType = "Test",
                DeviceName = "Test Device"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

            return apiResponse!.Data.AccessToken;
        }

        public class TestPaginatedList<T>
        {
            [JsonPropertyName("items")]
            public List<T> Items { get; set; } = new();

            [JsonPropertyName("pageNumber")]
            public int PageNumber { get; set; }

            [JsonPropertyName("pageSize")]
            public int PageSize { get; set; }

            [JsonPropertyName("totalCount")]
            public int TotalCount { get; set; }

            [JsonPropertyName("totalPages")]
            public int TotalPages { get; set; }

            [JsonPropertyName("hasPreviousPage")]
            public bool HasPreviousPage { get; set; }

            [JsonPropertyName("hasNextPage")]
            public bool HasNextPage { get; set; }
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; } = default!;
            public string Message { get; set; } = string.Empty;
        }

        #endregion
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.Trips.Commands.CreateTrip;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Vivu.IntegrationTests.Auth;
using Xunit;

namespace Vivu.IntegrationTests.Trips
{
    [Collection("Integration Tests")]
    public class CreateTripIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _testUser = null!;
        private string _accessToken = string.Empty;

        public CreateTripIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedTestUserAsync("tripuser@example.com", "Password123!");
            _accessToken = await GetAccessTokenAsync("tripuser@example.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<Domain.Entities.TripMember>().ExecuteDeleteAsync();
            await _dbContext.Set<TripDay>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Happy Path Integration Tests

        [Fact]
        public async Task CreateTrip_WithValidData_ReturnsCreatedTrip()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "My Paris Trip",
                Description = "A wonderful trip to Paris",
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow.AddDays(37),
                TripSize = 4,
                IsPublic = false,
                GenerateInviteCode = false
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var jsonString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TripDto>>(jsonString, options);

            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            apiResponse.Data.Title.Should().Be(command.Title);
            apiResponse.Data.Description.Should().Be(command.Description);
            apiResponse.Data.TripSize.Should().Be(command.TripSize);
            apiResponse.Data.IsPublic.Should().Be(command.IsPublic);
            apiResponse.Data.Status.Should().Be("planning");
            apiResponse.Data.UserId.Should().Be(_testUser.Id);
        }

        [Fact]
        public async Task CreateTrip_WithValidData_CreatesTripInDatabase()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Database Test Trip",
                Description = "Testing database persistence",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(15),
                TripSize = 2,
                IsPublic = true,
                GenerateInviteCode = false
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>();

            var tripInDb = await _dbContext.Set<Trip>()
                .FirstOrDefaultAsync(t => t.Id == apiResponse!.Data.Id);

            tripInDb.Should().NotBeNull();
            tripInDb!.Title.Should().Be(command.Title);
            tripInDb.Description.Should().Be(command.Description);
            tripInDb.TripSize.Should().Be(command.TripSize);
            tripInDb.IsPublic.Should().Be(command.IsPublic);
            tripInDb.UserId.Should().Be(_testUser.Id);
            tripInDb.Status.Should().Be("planning");
            tripInDb.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task CreateTrip_WithValidData_CreatesTripMemberForOwner()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "TripMember Test Trip",
                Description = "Testing TripMember creation"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var tripMember = await _dbContext.Set<Domain.Entities.TripMember>()
                .FirstOrDefaultAsync(tm => tm.TripId == apiResponse!.Data.Id);

            tripMember.Should().NotBeNull();
            tripMember!.UserId.Should().Be(_testUser.Id);
            tripMember.OwnerId.Should().Be(_testUser.Id);
            tripMember.Role.Should().Be("owner");
            tripMember.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task CreateTrip_WithGenerateInviteCode_GeneratesInviteCode()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Trip With Invite Code",
                GenerateInviteCode = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse!.Data.InviteCode.Should().NotBeNullOrEmpty();

            var tripInDb = await _dbContext.Set<Trip>()
                .FirstOrDefaultAsync(t => t.Id == apiResponse.Data.Id);
            tripInDb!.InviteCode.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateTrip_WithoutGenerateInviteCode_NoInviteCode()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Trip Without Invite Code",
                GenerateInviteCode = false
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse!.Data.InviteCode.Should().BeNull();
        }

        [Fact]
        public async Task CreateTrip_MinimalData_ReturnsSuccess()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Minimal Trip"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Failure Tests - Unauthorized

        [Fact]
        public async Task CreateTrip_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            var command = new CreateTripCommand
            {
                Title = "Unauthorized Trip"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateTrip_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            var command = new CreateTripCommand
            {
                Title = "Invalid Token Trip"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateTrip_WithExpiredToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            // This is a manually crafted expired token (you may need to adjust based on your JWT settings)
            var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZXhwIjoxMDAwMDAwMDAwfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

            var command = new CreateTripCommand
            {
                Title = "Expired Token Trip"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Failure Tests - Validation

        [Fact]
        public async Task CreateTrip_EmptyTitle_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = string.Empty
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task CreateTrip_TitleTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = new string('A', 201) // Exceeds 200 character limit
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task CreateTrip_DescriptionTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                Description = new string('A', 2001) // Exceeds 2000 character limit
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task CreateTrip_EndDateBeforeStartDate_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Invalid Dates Trip",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(5)
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task CreateTrip_NegativeTripSize_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Invalid Trip Size",
                TripSize = -1
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task CreateTrip_ZeroTripSize_ReturnsUnprocessableEntity()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Zero Trip Size",
                TripSize = 0
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Database State Verification Tests

        [Fact]
        public async Task CreateTrip_MultipleTrips_AllPersistedCorrectly()
        {
            // Arrange
            var commands = new[]
            {
                new CreateTripCommand { Title = "Trip 1", IsPublic = true },
                new CreateTripCommand { Title = "Trip 2", IsPublic = false },
                new CreateTripCommand { Title = "Trip 3", GenerateInviteCode = true }
            };

            // Act
            foreach (var command in commands)
            {
                await _client.PostAsJsonAsync("/api/trips", command);
            }

            // Assert
            var tripsInDb = await _dbContext.Set<Trip>()
                .Where(t => t.UserId == _testUser.Id)
                .ToListAsync();

            tripsInDb.Should().HaveCount(3);
            tripsInDb.Select(t => t.Title).Should().Contain(new[] { "Trip 1", "Trip 2", "Trip 3" });
        }

        [Fact]
        public async Task CreateTrip_EachTripHasTripMember()
        {
            // Arrange & Act
            for (int i = 1; i <= 3; i++)
            {
                var command = new CreateTripCommand { Title = $"Trip {i}" };
                await _client.PostAsJsonAsync("/api/trips", command);
            }

            // Assert
            var tripMembers = await _dbContext.Set<Domain.Entities.TripMember>()
                .Where(tm => tm.UserId == _testUser.Id)
                .ToListAsync();

            tripMembers.Should().HaveCount(3);
            tripMembers.Should().AllSatisfy(tm =>
            {
                tm.Role.Should().Be("owner");
                tm.OwnerId.Should().Be(_testUser.Id);
            });
        }

        #endregion

        #region Concurrent Request Tests

        [Fact]
        public async Task CreateTrip_ConcurrentRequests_AllSucceed()
        {
            // Arrange
            var tasks = Enumerable.Range(1, 5)
                .Select(i => _client.PostAsJsonAsync("/api/trips", new CreateTripCommand
                {
                    Title = $"Concurrent Trip {i}"
                }))
                .ToList();

            // Act
            var responses = await Task.WhenAll(tasks);

            // Assert
            responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

            var tripsInDb = await _dbContext.Set<Trip>()
                .Where(t => t.UserId == _testUser.Id && t.Title.StartsWith("Concurrent Trip"))
                .CountAsync();

            tripsInDb.Should().Be(5);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task CreateTrip_WithSpecialCharactersInTitle_Succeeds()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Trip với tiếng Việt 🌍 & special chars !@#$"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse!.Data.Title.Should().Be(command.Title);
        }

        [Fact]
        public async Task CreateTrip_PublicTrip_SetsIsPublicTrue()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Public Trip",
                IsPublic = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/trips", command);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse!.Data.IsPublic.Should().BeTrue();

            var tripInDb = await _dbContext.Set<Trip>()
                .FirstOrDefaultAsync(t => t.Id == apiResponse.Data.Id);
            tripInDb!.IsPublic.Should().BeTrue();
        }

        #endregion

        #region Helper Methods

        private async Task<User> SeedTestUserAsync(string email, string password)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword(password),
                fullName: "Trip Test User",
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

        #endregion
    }
}

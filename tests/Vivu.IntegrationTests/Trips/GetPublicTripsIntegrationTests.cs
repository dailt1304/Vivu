using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Vivu.IntegrationTests.Common;
using Xunit;

namespace Vivu.IntegrationTests.Trips
{
    [Collection("Integration Tests")]
    public class GetPublicTripsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthTokenProcess _tokenProcess;

        private User _testUser = null!;
        private string _testToken = string.Empty;

        public GetPublicTripsIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            _tokenProcess = _scope.ServiceProvider.GetRequiredService<IAuthTokenProcess>();
        }

        public async Task InitializeAsync()
        {
            await CleanupDatabaseAsync();
            _testUser = await SeedUserAsync("publictrip@example.com");
            var roles = _testUser.UserRoles.Select(ur => ur.Role.RoleName).ToArray();
            _testToken = _tokenProcess.GenerateToken(_testUser, roles);
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<TripMember>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Helper Methods

        private async Task<User> SeedUserAsync(string email, string roleName = "USER")
        {
            var user = User.Create(
                email: email,
                passwordHash: _passwordHasher.HashPassword("Password123!"),
                fullName: "Test User"
            );
            user.IsEmailVerified = true;
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            var role = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = roleName,
                RoleDescription = "Standard User"
            };
            user.UserRoles = new List<UserRole>
            {
                new() { UserId = user.Id, RoleId = role.Id, Role = role }
            };

            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<Trip> SeedTripAsync(
            Guid userId,
            string title,
            bool isPublic = true,
            DateTime? startDate = null,
            DateTime? endDate = null,
            Guid? cityId = null)
        {
            var trip = Trip.Create(
                userId: userId,
                title: title,
                description: $"Description for {title}",
                startDate: startDate ?? DateTime.UtcNow.AddDays(-5),
                endDate: endDate ?? DateTime.UtcNow.AddDays(2),
                isPublic: isPublic
            );

            if (cityId.HasValue)
                typeof(Trip).GetProperty("CityId")!.SetValue(trip, cityId.Value);

            _dbContext.Set<Trip>().Add(trip);

            var member = TripMember.Create(
                tripId: trip.Id,
                userId: userId,
                ownerId: userId,
                role: "owner"
            );
            _dbContext.Set<TripMember>().Add(member);

            await _dbContext.SaveChangesAsync();
            return trip;
        }

        private async Task<List<Trip>> SeedPublicTripsAsync(int count, Guid? userId = null)
        {
            var trips = new List<Trip>();
            var uid = userId ?? _testUser.Id;
            for (int i = 0; i < count; i++)
            {
                var trip = await SeedTripAsync(uid, $"Public Trip {i + 1}", isPublic: true);
                trips.Add(trip);
            }
            return trips;
        }

        private async Task<PublicTripsApiResponse> GetPublicTripsAsync(string queryString = "")
        {
            var response = await _client.GetAsync($"/api/Trips/public{queryString}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<PublicTripsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!;
        }

        #endregion

        #region Anonymous Access Tests

        [Fact]
        public async Task GetPublicTrips_WithoutToken_ReturnsOk()
        {
            // Arrange – no Authorization header
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/Trips/public");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetPublicTrips_WithToken_ReturnsOk()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _testToken);

            // Act
            var response = await _client.GetAsync("/api/Trips/public");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task GetPublicTrips_WithNoTrips_ReturnsEmptyList()
        {
            // Act
            var apiResponse = await GetPublicTripsAsync();

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetPublicTrips_WithPublicTrips_ReturnsCorrectCount()
        {
            // Arrange
            await SeedPublicTripsAsync(3);

            // Act
            var apiResponse = await GetPublicTripsAsync();

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().HaveCount(3);
            apiResponse.Data.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetPublicTrips_WithPrivateTrips_ExcludesThem()
        {
            // Arrange
            await SeedPublicTripsAsync(2);                                            // 2 public
            await SeedTripAsync(_testUser.Id, "Private Trip", isPublic: false);      // 1 private

            // Act
            var apiResponse = await GetPublicTripsAsync();

            // Assert
            apiResponse.Data!.TotalCount.Should().Be(2);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task GetPublicTrips_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            await SeedPublicTripsAsync(5);

            // Act
            var apiResponse = await GetPublicTripsAsync("?pageNumber=1&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.PageNumber.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(2);
            apiResponse.Data.TotalCount.Should().Be(5);
            apiResponse.Data.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task GetPublicTrips_SecondPage_ReturnsCorrectItems()
        {
            // Arrange
            await SeedPublicTripsAsync(5);

            // Act
            var apiResponse = await GetPublicTripsAsync("?pageNumber=2&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.PageNumber.Should().Be(2);
            apiResponse.Data.HasPreviousPage.Should().BeTrue();
            apiResponse.Data.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetPublicTrips_LastPage_HasNoNextPage()
        {
            // Arrange
            await SeedPublicTripsAsync(5);

            // Act
            var apiResponse = await GetPublicTripsAsync("?pageNumber=3&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(1);
            apiResponse.Data.HasNextPage.Should().BeFalse();
            apiResponse.Data.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetPublicTrips_DefaultPagination_UsesPageSize10()
        {
            // Arrange
            await SeedPublicTripsAsync(3);

            // Act
            var apiResponse = await GetPublicTripsAsync();

            // Assert
            apiResponse.Data!.PageNumber.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(10);
        }

        #endregion

        #region SortBy Tests

        [Theory]
        [InlineData("newest")]
        [InlineData("popular")]
        [InlineData("trending")]
        public async Task GetPublicTrips_WithValidSortBy_ReturnsOk(string sortBy)
        {
            // Arrange
            await SeedPublicTripsAsync(2);

            // Act
            var response = await _client.GetAsync($"/api/Trips/public?sortBy={sortBy}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetPublicTrips_WithInvalidSortBy_ReturnsUnprocessableEntity()
        {
            // Act
            var response = await _client.GetAsync("/api/Trips/public?sortBy=invalid");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Duration Filter Tests

        [Fact]
        public async Task GetPublicTrips_WithPositiveDuration_ReturnsOk()
        {
            // Arrange
            await SeedPublicTripsAsync(2);

            // Act
            var response = await _client.GetAsync("/api/Trips/public?duration=7");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetPublicTrips_WithZeroDuration_ReturnsUnprocessableEntity()
        {
            // Act
            var response = await _client.GetAsync("/api/Trips/public?duration=0");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task GetPublicTrips_WithNegativeDuration_ReturnsUnprocessableEntity()
        {
            // Act
            var response = await _client.GetAsync("/api/Trips/public?duration=-1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region City Filter Tests

        [Fact]
        public async Task GetPublicTrips_WithInvalidCityId_ReturnsNotFound()
        {
            // Arrange – city does not exist in DB
            var nonExistentCityId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/Trips/public?city={nonExistentCityId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetPublicTrips_WithPageNumberZero_ClampsToOne()
        {
            // Note: PaginationRequest clamps PageNumber < 1 to 1 in the setter
            // Act
            var response = await _client.GetAsync("/api/Trips/public?pageNumber=0");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<PublicTripsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            apiResponse!.Data!.PageNumber.Should().Be(1);
        }

        [Fact]
        public async Task GetPublicTrips_WithPageSizeTooLarge_ClampsToMaximum()
        {
            // Note: PaginationRequest clamps PageSize > 100 to 100 in the setter
            // Act
            var response = await _client.GetAsync("/api/Trips/public?pageSize=101");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<PublicTripsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            apiResponse!.Data!.PageSize.Should().Be(100);
        }

        #endregion

        #region Response Structure Tests

        [Fact]
        public async Task GetPublicTrips_ResponseHasCorrectStructure()
        {
            // Arrange
            await SeedPublicTripsAsync(2);

            // Act
            var response = await _client.GetAsync("/api/Trips/public");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<PublicTripsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().NotBeNull();
        }

        #endregion
    }

    #region Response Helper Classes

    public class PublicTripsApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public PublicTripsPaginatedResponse? Data { get; set; }
    }

    #endregion
}

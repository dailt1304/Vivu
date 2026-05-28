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
    public class SearchPublicTripsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthTokenProcess _tokenProcess;

        private User _testUser = null!;
        private string _testToken = string.Empty;

        public SearchPublicTripsIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedUserAsync("searchtest@example.com");
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
            string description,
            bool isPublic = true,
            DateTime? startDate = null,
            DateTime? endDate = null,
            Guid? cityId = null)
        {
            var trip = Trip.Create(
                userId: userId,
                title: title,
                description: description,
                startDate: startDate ?? DateTime.UtcNow.AddDays(1),
                endDate: endDate ?? DateTime.UtcNow.AddDays(7),
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

        private async Task<SearchPublicTripsApiResponse> SearchPublicTripsAsync(string queryString)
        {
            var response = await _client.GetAsync($"/api/Trips/public/search{queryString}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<SearchPublicTripsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!;
        }

        #endregion

        #region Anonymous Access Tests

        [Fact]
        public async Task SearchPublicTrips_WithoutToken_ReturnsOk()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Trip", "Beautiful beach vacation", isPublic: true);
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/Trips/public/search?q=beach");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SearchPublicTrips_WithToken_ReturnsOk()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Trip", "Beautiful beach vacation", isPublic: true);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _testToken);

            // Act
            var response = await _client.GetAsync("/api/Trips/public/search?q=beach");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task SearchPublicTrips_WithMatchingTerm_ReturnsResults()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Vacation", "Relaxing beach vacation", isPublic: true);
            await SeedTripAsync(_testUser.Id, "Mountain Hiking", "Adventure mountain trip", isPublic: true);

            // Act
            var apiResponse = await SearchPublicTripsAsync("?q=beach");

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().NotBeEmpty();
            apiResponse.Data.Items.Should().Contain(t => t.Title.Contains("Beach", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SearchPublicTrips_WithNoMatches_ReturnsEmptyList()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Trip", "Beach vacation", isPublic: true);

            // Act
            var apiResponse = await SearchPublicTripsAsync("?q=mountain");

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task SearchPublicTrips_ExcludesPrivateTrips()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Public Beach Trip", "Public beach vacation", isPublic: true);
            await SeedTripAsync(_testUser.Id, "Private Beach Trip", "Private beach vacation", isPublic: false);

            // Act
            var apiResponse = await SearchPublicTripsAsync("?q=beach");

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().OnlyContain(t => t.Title.Contains("Public"));
        }

        #endregion

        #region Search Term Tests

        [Theory]
        [InlineData("beach")]
        [InlineData("BEACH")]
        [InlineData("Beach")]
        public async Task SearchPublicTrips_CaseInsensitiveSearch_ReturnsResults(string searchTerm)
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Vacation", "Tropical beach", isPublic: true);

            // Act
            var response = await _client.GetAsync($"/api/Trips/public/search?q={searchTerm}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<SearchPublicTripsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            apiResponse!.Data!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SearchPublicTrips_WithMultiWordTerm_ReturnsResults()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Vacation", "Beautiful beach vacation trip", isPublic: true);

            // Act
            var apiResponse = await SearchPublicTripsAsync("?q=beach vacation");

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SearchPublicTrips_SearchInDescription_ReturnsResults()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Summer Trip", "Relaxing beach vacation", isPublic: true);

            // Act
            var apiResponse = await SearchPublicTripsAsync("?q=beach");

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().NotBeEmpty();
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task SearchPublicTrips_WithoutSearchTerm_ReturnsBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/Trips/public/search");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchPublicTrips_WithEmptySearchTerm_ReturnsBadRequest()
        {
            // Act - Empty query string parameter results in BadRequest, not Unprocessable
            var response = await _client.GetAsync("/api/Trips/public/search?q=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchPublicTrips_WithTooShortSearchTerm_ReturnsUnprocessableEntity()
        {
            // Act
            var response = await _client.GetAsync("/api/Trips/public/search?q=a");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task SearchPublicTrips_WithPageNumberZero_ClampsToOne()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Trip", "Beach vacation", isPublic: true);

            // Act
            var response = await _client.GetAsync("/api/Trips/public/search?q=beach&pageNumber=0");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<SearchPublicTripsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            apiResponse!.Data!.PageNumber.Should().Be(1);
        }

        [Fact]
        public async Task SearchPublicTrips_WithPageSizeTooLarge_ClampsToMaximum()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Trip", "Beach vacation", isPublic: true);

            // Act
            var response = await _client.GetAsync("/api/Trips/public/search?q=beach&pageSize=101");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<SearchPublicTripsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            apiResponse!.Data!.PageSize.Should().Be(100);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task SearchPublicTrips_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                await SeedTripAsync(_testUser.Id, $"Beach Trip {i}", "Beach vacation", isPublic: true);
            }

            // Act
            var apiResponse = await SearchPublicTripsAsync("?q=beach&pageNumber=1&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.PageNumber.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(2);
            apiResponse.Data.TotalCount.Should().BeGreaterThanOrEqualTo(5);
        }

        [Fact]
        public async Task SearchPublicTrips_SecondPage_ReturnsCorrectItems()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                await SeedTripAsync(_testUser.Id, $"Beach Trip {i}", "Beach vacation", isPublic: true);
            }

            // Act
            var apiResponse = await SearchPublicTripsAsync("?q=beach&pageNumber=2&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.PageNumber.Should().Be(2);
            apiResponse.Data.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public async Task SearchPublicTrips_DefaultPagination_UsesDefaultValues()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Trip", "Beach vacation", isPublic: true);

            // Act
            var apiResponse = await SearchPublicTripsAsync("?q=beach");

            // Assert
            apiResponse.Data!.PageNumber.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(10);
        }

        #endregion

        #region Filter Tests

        [Fact]
        public async Task SearchPublicTrips_WithCityFilter_RequiresValidCity()
        {
            // Arrange
            var invalidCityId = Guid.NewGuid();
            await SeedTripAsync(_testUser.Id, "Beach Trip", "Beach in city", isPublic: true);

            // Act - Using a cityId that doesn't exist in the Cities table
            var response = await _client.GetAsync($"/api/Trips/public/search?q=beach&city={invalidCityId}");

            // Assert - City validation fails if city doesn't exist
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SearchPublicTrips_WithDurationFilter_AppliesFilter()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Trip", "Beach vacation", isPublic: true,
                startDate: DateTime.UtcNow, endDate: DateTime.UtcNow.AddDays(7));

            // Act
            var response = await _client.GetAsync("/api/Trips/public/search?q=beach&duration=7");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Response Structure Tests

        [Fact]
        public async Task SearchPublicTrips_ResponseHasCorrectStructure()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Trip", "Beach vacation", isPublic: true);

            // Act
            var response = await _client.GetAsync("/api/Trips/public/search?q=beach");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<SearchPublicTripsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().NotBeNull();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task SearchPublicTrips_WithSpecialCharacters_HandlesGracefully()
        {
            // Arrange
            await SeedTripAsync(_testUser.Id, "Beach Trip!", "Beach vacation", isPublic: true);

            // Act
            var response = await _client.GetAsync("/api/Trips/public/search?q=beach!");

            // Assert
            // Should either return results or handle gracefully
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task SearchPublicTrips_WithLongSearchTerm_ReturnsResults()
        {
            // Arrange
            var longDescription = "This is a very long description with many words about beach vacation";
            await SeedTripAsync(_testUser.Id, "Trip", longDescription, isPublic: true);

            // Act
            var response = await _client.GetAsync("/api/Trips/public/search?q=beach vacation");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion
    }

    #region Response Helper Classes

    public class SearchPublicTripsApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public PublicTripsPaginatedResponse? Data { get; set; }
    }

    #endregion
}

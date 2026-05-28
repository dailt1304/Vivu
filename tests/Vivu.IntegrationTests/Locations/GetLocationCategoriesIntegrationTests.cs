using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.LocationCategories;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.Locations
{
    [Collection("Integration Tests")]
    public class GetLocationCategoriesIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _user = null!;
        private string _userToken = string.Empty;

        public GetLocationCategoriesIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _user = await SeedUserAsync("user_categories@example.com", "Password123!");
            _userToken = await GetAccessTokenAsync("user_categories@example.com", "Password123!");
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<LocationReport>().ExecuteDeleteAsync();
            await _dbContext.Set<LocationDetail>().ExecuteDeleteAsync();
            await _dbContext.Set<Location>().ExecuteDeleteAsync();
            await _dbContext.Set<LocationCategory>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRoleEntity>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Helper Methods

        private async Task<User> SeedUserAsync(string email, string password)
        {
            var hashedPassword = _passwordHasher.HashPassword(password);
            var user = User.Create(email, hashedPassword, "Test User");
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var loginRequest = new LoginUserCommand
            {
                Email = email,
                Password = password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var loginResponse = await response.Content.ReadFromJsonAsync<GetCategoriesApiResponse<GetCategoriesLoginResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return loginResponse!.Data!.AccessToken;
        }

        private async Task<LocationCategory> SeedCategoryAsync(string name, string? iconUrl = null)
        {
            var category = new LocationCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                IconUrl = iconUrl ?? "https://example.com/icon.png"
            };

            await _dbContext.Set<LocationCategory>().AddAsync(category);
            await _dbContext.SaveChangesAsync();
            return category;
        }

        private async Task<Location> SeedLocationWithCategoryAsync(string locationName, Guid categoryId)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                Name = locationName,
                Description = "Test Description",
                Address = "123 Test Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = true,
                CategoryId = categoryId
            };

            await _dbContext.Set<Location>().AddAsync(location);
            await _dbContext.SaveChangesAsync();
            return location;
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task GetLocationCategories_WithToken_ReturnsOkWithCategories()
        {
            // Arrange
            await SeedCategoryAsync("Food & Drink");
            await SeedCategoryAsync("Nature");
            await SeedCategoryAsync("Entertainment");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

            // Act
            var response = await _client.GetAsync("/api/location-categories");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<GetCategoriesApiResponse<GetCategoriesPaginatedResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().HaveCount(3);
            apiResponse.Data.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetLocationCategories_WithNoCategories_ReturnsEmptyList()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

            // Act
            var response = await _client.GetAsync("/api/location-categories");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<GetCategoriesApiResponse<GetCategoriesPaginatedResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Data!.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetLocationCategories_ReturnsCorrectCategoryData()
        {
            // Arrange
            var category = await SeedCategoryAsync("Museums", "https://example.com/museum.png");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

            // Act
            var response = await _client.GetAsync("/api/location-categories");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<GetCategoriesApiResponse<GetCategoriesPaginatedResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var item = apiResponse!.Data!.Items.Should().ContainSingle().Subject;
            item.Id.Should().Be(category.Id);
            item.Name.Should().Be("Museums");
            item.IconUrl.Should().Be("https://example.com/museum.png");
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task GetLocationCategories_WithDefaultPagination_ReturnsFirstPage()
        {
            // Arrange
            for (int i = 1; i <= 15; i++)
                await SeedCategoryAsync($"Category {i:D2}");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

            // Act
            var response = await _client.GetAsync("/api/location-categories?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<GetCategoriesApiResponse<GetCategoriesPaginatedResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Data!.Items.Should().HaveCount(10);
            apiResponse.Data.TotalCount.Should().Be(15);
            apiResponse.Data.PageNumber.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(10);
            apiResponse.Data.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetLocationCategories_WithPageTwo_ReturnsSecondPage()
        {
            // Arrange
            for (int i = 1; i <= 15; i++)
                await SeedCategoryAsync($"Category {i:D2}");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

            // Act
            var response = await _client.GetAsync("/api/location-categories?pageNumber=2&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<GetCategoriesApiResponse<GetCategoriesPaginatedResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Data!.Items.Should().HaveCount(5);
            apiResponse.Data.TotalCount.Should().Be(15);
            apiResponse.Data.PageNumber.Should().Be(2);
        }

        [Fact]
        public async Task GetLocationCategories_WithCustomPageSize_ReturnsCorrectCount()
        {
            // Arrange
            for (int i = 1; i <= 10; i++)
                await SeedCategoryAsync($"Category {i:D2}");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

            // Act
            var response = await _client.GetAsync("/api/location-categories?pageNumber=1&pageSize=5");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<GetCategoriesApiResponse<GetCategoriesPaginatedResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Data!.Items.Should().HaveCount(5);
            apiResponse.Data.TotalCount.Should().Be(10);
            apiResponse.Data.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetLocationCategories_PageBeyondTotal_ReturnsEmptyItems()
        {
            // Arrange
            await SeedCategoryAsync("Only Category");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

            // Act
            var response = await _client.GetAsync("/api/location-categories?pageNumber=99&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<GetCategoriesApiResponse<GetCategoriesPaginatedResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Data!.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(1);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task GetLocationCategories_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/location-categories");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetLocationCategories_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

            // Act
            var response = await _client.GetAsync("/api/location-categories");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Database Verification Tests

        [Fact]
        public async Task GetLocationCategories_VerifiesDataPersistedInDatabase()
        {
            // Arrange
            await SeedCategoryAsync("Restaurants");
            await SeedCategoryAsync("Parks");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

            // Verify data is in the database
            var dbCount = await _dbContext.Set<LocationCategory>().CountAsync();
            dbCount.Should().Be(2);

            // Act
            var response = await _client.GetAsync("/api/location-categories");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<GetCategoriesApiResponse<GetCategoriesPaginatedResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Data!.TotalCount.Should().Be(dbCount);
        }

        #endregion
    }

    #region Helper Classes

    public class GetCategoriesApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class GetCategoriesLoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class GetCategoriesPaginatedResponse
    {
        [JsonPropertyName("items")]
        public List<LocationCategoryDto> Items { get; set; } = new();

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
    }

    #endregion
}

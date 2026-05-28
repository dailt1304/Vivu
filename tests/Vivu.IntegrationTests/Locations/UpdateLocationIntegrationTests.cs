using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.Locations.Commands.UpdateLocation;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.Locations
{
    [Collection("Integration Tests")]
    public class UpdateLocationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _adminUser = null!;
        private User _normalUser = null!;
        private string _adminToken = string.Empty;
        private string _normalUserToken = string.Empty;

        public UpdateLocationIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _adminUser = await SeedAdminUserAsync("admin@example.com", "Password123!");
            _normalUser = await SeedNormalUserAsync("user@example.com", "Password123!");
            _adminToken = await GetAccessTokenAsync("admin@example.com", "Password123!");
            _normalUserToken = await GetAccessTokenAsync("user@example.com", "Password123!");
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

        private async Task<User> SeedAdminUserAsync(string email, string password)
        {
            var moderatorRole = new Role { Id = Guid.NewGuid(), RoleName = "MODERATOR", RoleDescription = "Moderator Role" };
            await _dbContext.Set<Role>().AddAsync(moderatorRole);

            var hashedPassword = _passwordHasher.HashPassword(password);
            var user = User.Create(email, hashedPassword, "Moderator User");
            await _dbContext.Users.AddAsync(user);

            var userRole = new UserRoleEntity { UserId = user.Id, RoleId = moderatorRole.Id };
            await _dbContext.Set<UserRoleEntity>().AddAsync(userRole);

            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<User> SeedNormalUserAsync(string email, string password)
        {
            var hashedPassword = _passwordHasher.HashPassword(password);
            var user = User.Create(email, hashedPassword, "Normal User");
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

            var loginResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return loginResponse!.Data!.AccessToken;
        }

        private async Task<Location> SeedTestLocationAsync(
            string name,
            string? description = null,
            string? address = null,
            double? latitude = null,
            double? longitude = null,
            bool withDetails = true)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description ?? "Test Description",
                Address = address ?? "123 Test Street",
                Latitude = latitude ?? 10.762622,
                Longitude = longitude ?? 106.660172,
                IsVerified = true
            };

            await _dbContext.Set<Location>().AddAsync(location);

            if (withDetails)
            {
                var locationDetail = new LocationDetail
                {
                    LocationId = location.Id,
                    OpeningHours = "9:00-22:00",
                    Phone = "0123456789",
                    Website = "https://test.com",
                    Tags = "[\"test\",\"location\"]",
                    Images = "[\"image1.jpg\",\"image2.jpg\"]"
                };

                await _dbContext.Set<LocationDetail>().AddAsync(locationDetail);
            }

            await _dbContext.SaveChangesAsync();
            return location;
        }

        private async Task<LocationCategory> SeedTestCategoryAsync(string name)
        {
            var category = new LocationCategory
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            await _dbContext.Set<LocationCategory>().AddAsync(category);
            await _dbContext.SaveChangesAsync();
            return category;
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task UpdateLocation_ValidRequest_ReturnsOkWithUpdatedLocation()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Original Name");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Name = "Updated Name",
                Description = "Updated Description",
                Address = "Updated Address",
                Latitude = 21.028511,
                Longitude = 105.804817
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();

            var result = apiResponse.Data;
            result.Should().NotBeNull();
            result!.Name.Should().Be("Updated Name");
            result.Description.Should().Be("Updated Description");
            result.Address.Should().Be("Updated Address");
            result.Latitude.Should().Be(21.028511);
            result.Longitude.Should().Be(105.804817);
        }

        [Fact]
        public async Task UpdateLocation_PartialUpdate_OnlyUpdatesProvidedFields()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Original Name", "Original Description");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Name = "Only Name Changed"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var result = apiResponse!.Data;
            result.Should().NotBeNull();
            result!.Name.Should().Be("Only Name Changed");
            result.Description.Should().Be("Original Description"); // Should remain unchanged
        }

        [Fact]
        public async Task UpdateLocation_UpdateLocationDetails_UpdatesSuccessfully()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                OpeningHours = "8:00-23:00",
                Phone = "9876543210",
                Website = "https://updated.com",
                Tags = "[\"updated\",\"tags\"]"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify in database
            _dbContext.ChangeTracker.Clear();
            var updatedLocation = await _dbContext.Set<Location>()
                .Include(l => l.LocationDetail)
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            updatedLocation.Should().NotBeNull();
            updatedLocation!.LocationDetail.Should().NotBeNull();
            updatedLocation.LocationDetail!.OpeningHours.Should().Be("8:00-23:00");
            updatedLocation.LocationDetail.Phone.Should().Be("9876543210");
            updatedLocation.LocationDetail.Website.Should().Be("https://updated.com");
            updatedLocation.LocationDetail.Tags.Should().Be("[\"updated\",\"tags\"]");
        }

        [Fact]
        public async Task UpdateLocation_UpdateCoordinates_UpdatesSuccessfully()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Latitude = 16.047079,
                Longitude = 108.206230
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var result = apiResponse!.Data;
            result!.Latitude.Should().Be(16.047079);
            result.Longitude.Should().Be(108.206230);
        }

        [Fact]
        public async Task UpdateLocation_UpdateCategory_UpdatesSuccessfully()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            var newCategory = await SeedTestCategoryAsync("New Category");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                CategoryId = newCategory.Id
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedLocation = await _dbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            updatedLocation!.CategoryId.Should().Be(newCategory.Id);
        }

        [Fact]
        public async Task UpdateLocation_UpdateIsVerified_UpdatesSuccessfully()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            location.IsVerified = false;
            await _dbContext.SaveChangesAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                IsVerified = true
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _dbContext.ChangeTracker.Clear();
            var updatedLocation = await _dbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            updatedLocation!.IsVerified.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateLocation_AllFields_UpdatesSuccessfully()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Original Location");
            var category = await SeedTestCategoryAsync("Test Category");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Name = "Completely Updated",
                Description = "New Description",
                Address = "456 New Street",
                Latitude = 21.028511,
                Longitude = 105.804817,
                CategoryId = category.Id,
                OpeningHours = "7:00-24:00",
                Phone = "1111111111",
                Website = "https://new-website.com",
                Tags = "[\"new\",\"tags\",\"updated\"]",
                Images = "[\"new1.jpg\",\"new2.jpg\"]",
                IsVerified = false
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var result = apiResponse!.Data;
            result.Should().NotBeNull();
            result!.Name.Should().Be("Completely Updated");
            result.Description.Should().Be("New Description");
            result.Address.Should().Be("456 New Street");
            result.Latitude.Should().Be(21.028511);
            result.Longitude.Should().Be(105.804817);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task UpdateLocation_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = null;

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Name = "Test"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateLocation_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Name = "Test"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Not Found Tests

        [Fact]
        public async Task UpdateLocation_NonExistentLocation_ReturnsNotFound()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
            var nonExistentLocationId = Guid.NewGuid();

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = nonExistentLocationId,
                Name = "Test"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{nonExistentLocationId}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task UpdateLocation_EmptyLocationId_ReturnsUnprocessableEntity()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = Guid.Empty,
                Name = "Test"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{Guid.Empty}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateLocation_NameTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Name = new string('a', 201)
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateLocation_DescriptionTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Description = new string('a', 2001)
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateLocation_AddressTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Address = new string('a', 501)
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateLocation_PhoneTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Phone = new string('1', 21)
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task UpdateLocation_WebsiteTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Website = new string('a', 501)
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task UpdateLocation_WithoutLocationDetails_UpdatesSuccessfully()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location", withDetails: false);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id,
                Name = "Updated Name",
                OpeningHours = "8:00-22:00" // Should not throw even without LocationDetail
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Data!.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task UpdateLocation_OnlyLocationId_ReturnsOk()
        {
            // Arrange
            var location = await SeedTestLocationAsync("Test Location");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

            var updateCommand = new UpdateLocationCommand
            {
                LocationId = location.Id
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/locations/{location.Id}", updateCommand);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify location remains unchanged
            _dbContext.ChangeTracker.Clear();
            var updatedLocation = await _dbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            updatedLocation!.Name.Should().Be("Test Location");
        }

        #endregion
    }

    #region Helper Classes

    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    #endregion
}

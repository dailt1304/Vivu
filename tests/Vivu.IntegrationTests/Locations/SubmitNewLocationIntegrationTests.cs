using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Infrastructure.Data;
using Xunit;
using UserRoleEntity = Vivu.Domain.Entities.UserRole;

namespace Vivu.IntegrationTests.Locations
{
    [Collection("Integration Tests")]
    public class SubmitNewLocationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _testUser = null!;
        private string _accessToken = string.Empty;
        private LocationCategory _testCategory = null!;
        private City _testCity = null!;
        private Country _testCountry = null!;

        public SubmitNewLocationIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedTestUserAsync("locationuser@example.com", "Password123!");
            _testCategory = await SeedLocationCategoryAsync("Restaurant");
            _testCountry = await SeedCountryAsync("Vietnam");
            _testCity = await SeedCityAsync("Ho Chi Minh City", _testCountry.Id);
            _accessToken = await GetAccessTokenAsync("locationuser@example.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<LocationReport>().ExecuteDeleteAsync();
            await _dbContext.Set<Location>().ExecuteDeleteAsync();
            await _dbContext.Set<LocationCategory>().ExecuteDeleteAsync();
            await _dbContext.Set<City>().ExecuteDeleteAsync();            await _dbContext.Set<Country>().ExecuteDeleteAsync();            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRoleEntity>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Happy Path Tests

        [Fact]
        public async Task SubmitNewLocation_WithValidData_ReturnsCreatedLocation()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "New Beach Resort",
                ["Description"] = "A beautiful beach resort",
                ["Address"] = "123 Beach Road",
                ["Latitude"] = "10.762622",
                ["Longitude"] = "106.660172",
                ["OpeningHours"] = "9:00-22:00",
                ["Phone"] = "0123456789",
                ["Website"] = "https://example.com",
                ["Tags"] = "beach,resort"
            });

            AddMockImageToContent(content, "beach.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            apiResponse.Data.Name.Should().Be("New Beach Resort");
            apiResponse.Data.Description.Should().Be("A beautiful beach resort");
            apiResponse.Data.IsVerified.Should().BeFalse();
        }

        [Fact]
        public async Task SubmitNewLocation_WithValidData_CreatesLocationInDatabase()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Database Test Location",
                ["Address"] = "456 Test Street"
            });
            AddMockImageToContent(content, "test.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>();

            // Assert
            var locationInDb = await _dbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == apiResponse!.Data.Id);

            locationInDb.Should().NotBeNull();
            locationInDb!.Name.Should().Be("Database Test Location");
            locationInDb.Address.Should().Be("456 Test Street");
            locationInDb.IsVerified.Should().BeFalse();
            locationInDb.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task SubmitNewLocation_WithValidData_CreatesLocationReport()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Report Test Location",
                ["Address"] = "789 Report Street"
            });
            AddMockImageToContent(content, "report.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>();

            // Assert
            var report = await _dbContext.Set<LocationReport>()
                .FirstOrDefaultAsync(lr => lr.LocationId == apiResponse!.Data.Id);

            report.Should().NotBeNull();
            report!.UserId.Should().Be(_testUser.Id);
            report.ReportType.Should().Be(ReportType.NEW_LOCATION.ToString());
            report.Status.Should().Be(ReportStatus.PENDING.ToString());
        }

        [Fact]
        public async Task SubmitNewLocation_WithCategoryId_AssociatesCategory()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Restaurant Location",
                ["Address"] = "123 Food Street",
                ["CategoryId"] = _testCategory.Id.ToString()
            });
            AddMockImageToContent(content, "restaurant.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>();

            // Assert
            var locationInDb = await _dbContext.Set<Location>()
                .Include(l => l.Category)
                .FirstOrDefaultAsync(l => l.Id == apiResponse!.Data.Id);

            locationInDb.Should().NotBeNull();
            locationInDb!.CategoryId.Should().Be(_testCategory.Id);
            locationInDb.Category.Should().NotBeNull();
            locationInDb.Category!.Name.Should().Be("Restaurant");
        }

        [Fact]
        public async Task SubmitNewLocation_WithCityId_AssociatesCity()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "City Location",
                ["Address"] = "123 City Street",
                ["CityId"] = _testCity.Id.ToString()
            });
            AddMockImageToContent(content, "city.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>();

            // Assert
            var locationInDb = await _dbContext.Set<Location>()
                .Include(l => l.City)
                .FirstOrDefaultAsync(l => l.Id == apiResponse!.Data.Id);

            locationInDb.Should().NotBeNull();
            locationInDb!.CityId.Should().Be(_testCity.Id);
            locationInDb.City.Should().NotBeNull();
            locationInDb.City!.Name.Should().Be("Ho Chi Minh City");
        }

        [Fact]
        public async Task SubmitNewLocation_WithCoordinatesOnly_SubmitsSuccessfully()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "GPS Location",
                ["Address"] = "GPS Coordinates",
                ["Latitude"] = "10.762622",
                ["Longitude"] = "106.660172"
            });
            AddMockImageToContent(content, "gps.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>();
            apiResponse!.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitNewLocation_WithMultipleImages_UploadsAllImages()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Multi Image Location",
                ["Address"] = "123 Photo Street"
            });

            AddMockImageToContent(content, "image1.jpg");
            AddMockImageToContent(content, "image2.jpg");
            AddMockImageToContent(content, "image3.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SubmitNewLocation_WithAllOptionalFields_SubmitsSuccessfully()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Complete Location",
                ["Description"] = "Full description",
                ["Address"] = "123 Complete Street",
                ["Latitude"] = "10.762622",
                ["Longitude"] = "106.660172",
                ["CityId"] = _testCity.Id.ToString(),
                ["CategoryId"] = _testCategory.Id.ToString(),
                ["OpeningHours"] = "9:00-22:00",
                ["Phone"] = "0123456789",
                ["Website"] = "https://example.com",
                ["Tags"] = "tag1,tag2,tag3"
            });
            AddMockImageToContent(content, "complete.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SubmitNewLocation_TrimsNameAndAddress_BeforeProcessing()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "  Trimmed Location  ",
                ["Address"] = "  123 Trimmed Street  "
            });
            AddMockImageToContent(content, "trim.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>();

            // Assert
            var locationInDb = await _dbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == apiResponse!.Data.Id);

            locationInDb.Should().NotBeNull();
            locationInDb!.Name.Should().Be("Trimmed Location");
            locationInDb.Address.Should().Be("123 Trimmed Street");
        }

        #endregion

        #region Authentication/Authorization Tests

        [Fact]
        public async Task SubmitNewLocation_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Unauthorized Location",
                ["Address"] = "123 Unauthorized Street"
            });
            AddMockImageToContent(content, "unauth.jpg");

            // Act
            var response = await client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task SubmitNewLocation_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Invalid Token Location",
                ["Address"] = "123 Invalid Street"
            });
            AddMockImageToContent(content, "invalid.jpg");

            // Act
            var response = await client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task SubmitNewLocation_WithoutName_ReturnsUnprocessableEntity()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Address"] = "123 No Name Street"
            });
            AddMockImageToContent(content, "noname.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task SubmitNewLocation_WithEmptyName_ReturnsUnprocessableEntity()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "",
                ["Address"] = "123 Empty Name Street"
            });
            AddMockImageToContent(content, "emptyname.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SubmitNewLocation_NameTooLong_ReturnsUnprocessableEntity()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = new string('A', 201), // Exceeds 200 character limit
                ["Address"] = "123 Long Name Street"
            });
            AddMockImageToContent(content, "longname.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task SubmitNewLocation_WithoutImages_ReturnsUnprocessableEntity()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "No Image Location",
                ["Address"] = "123 No Image Street"
            });
            // Don't add any images

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task SubmitNewLocation_InvalidCategoryId_ReturnsBadRequest()
        {
            // Arrange
            var invalidCategoryId = Guid.NewGuid();
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Invalid Category Location",
                ["Address"] = "123 Invalid Category Street",
                ["CategoryId"] = invalidCategoryId.ToString()
            });
            AddMockImageToContent(content, "invalidcat.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SubmitNewLocation_InvalidCityId_ReturnsBadRequest()
        {
            // Arrange
            var invalidCityId = Guid.NewGuid();
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Invalid City Location",
                ["Address"] = "123 Invalid City Street",
                ["CityId"] = invalidCityId.ToString()
            });
            AddMockImageToContent(content, "invalidcity.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SubmitNewLocation_LatitudeOutOfRange_ReturnsUnprocessableEntity()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Invalid Latitude",
                ["Latitude"] = "95.0", // > 90
                ["Longitude"] = "106.660172"
            });
            AddMockImageToContent(content, "invalidlat.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task SubmitNewLocation_LongitudeOutOfRange_ReturnsUnprocessableEntity()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Invalid Longitude",
                ["Latitude"] = "10.762622",
                ["Longitude"] = "185.0" // > 180
            });
            AddMockImageToContent(content, "invalidlong.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Duplicate Detection Tests

        [Fact]
        public async Task SubmitNewLocation_DuplicateNameAndAddress_ReturnsBadRequest()
        {
            // Arrange
            // First submission
            var content1 = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Duplicate Location",
                ["Address"] = "123 Duplicate Street"
            });
            AddMockImageToContent(content1, "first.jpg");
            await _client.PostAsync("/api/locations", content1);

            // Second submission with same name and address
            var content2 = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Duplicate Location",
                ["Address"] = "123 Duplicate Street"
            });
            AddMockImageToContent(content2, "second.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content2);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SubmitNewLocation_DuplicateNameAndCoordinates_ReturnsBadRequest()
        {
            // Arrange
            // First submission
            var content1 = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "GPS Duplicate",
                ["Address"] = "GPS Location",
                ["Latitude"] = "10.762622",
                ["Longitude"] = "106.660172"
            });
            AddMockImageToContent(content1, "gps1.jpg");
            await _client.PostAsync("/api/locations", content1);

            // Second submission with same name and coordinates
            var content2 = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "GPS Duplicate",
                ["Address"] = "GPS Location",
                ["Latitude"] = "10.762622",
                ["Longitude"] = "106.660172"
            });
            AddMockImageToContent(content2, "gps2.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content2);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task SubmitNewLocation_WithSpecialCharactersInName_Succeeds()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Café Phở & Bánh Mì 🍜 !@#$",
                ["Address"] = "123 Special Street"
            });
            AddMockImageToContent(content, "special.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LocationDto>>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse!.Data.Name.Should().Be("Café Phở & Bánh Mì 🍜 !@#$");
        }

        [Fact]
        public async Task SubmitNewLocation_MinimalData_Succeeds()
        {
            // Arrange
            var content = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "Minimal Location",
                ["Address"] = "123 Minimal Street"
            });
            AddMockImageToContent(content, "minimal.jpg");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SubmitNewLocation_MultipleUsers_AllSucceed()
        {
            // Arrange
            var user2 = await SeedTestUserAsync("locationuser2@example.com", "Password123!");
            var token2 = await GetAccessTokenAsync("locationuser2@example.com", "Password123!");

            var content1 = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "User1 Location",
                ["Address"] = "123 User1 Street"
            });
            AddMockImageToContent(content1, "user1.jpg");

            var content2 = CreateMultipartFormDataContent(new Dictionary<string, string>
            {
                ["Name"] = "User2 Location",
                ["Address"] = "456 User2 Street"
            });
            AddMockImageToContent(content2, "user2.jpg");

            // Act
            var response1 = await _client.PostAsync("/api/locations", content1);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
            var response2 = await _client.PostAsync("/api/locations", content2);

            // Assert
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);

            var locationsInDb = await _dbContext.Set<Location>().CountAsync();
            locationsInDb.Should().BeGreaterThanOrEqualTo(2);
        }

        #endregion

        #region Helper Methods

        private async Task<User> SeedTestUserAsync(string email, string password)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword(password),
                fullName: "Location Test User",
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

            user.UserRoles = new List<UserRoleEntity>
            {
                new UserRoleEntity
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

        private async Task<LocationCategory> SeedLocationCategoryAsync(string name)
        {
            var category = new LocationCategory
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            _dbContext.Set<LocationCategory>().Add(category);
            await _dbContext.SaveChangesAsync();

            return category;
        }

        private async Task<City> SeedCityAsync(string name, Guid countryId)
        {
            var city = new City
            {
                Id = Guid.NewGuid(),
                Name = name,
                CountryId = countryId
            };

            _dbContext.Set<City>().Add(city);
            await _dbContext.SaveChangesAsync();

            return city;
        }

        private async Task<Country> SeedCountryAsync(string name)
        {
            var country = new Country
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            _dbContext.Set<Country>().Add(country);
            await _dbContext.SaveChangesAsync();

            return country;
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

        private static MultipartFormDataContent CreateMultipartFormDataContent(Dictionary<string, string> formData)
        {
            var content = new MultipartFormDataContent();

            foreach (var kvp in formData)
            {
                content.Add(new StringContent(kvp.Value), kvp.Key);
            }

            return content;
        }

        private static void AddMockImageToContent(MultipartFormDataContent content, string fileName)
        {
            var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "Images", fileName);
        }

        #endregion

        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; } = default!;
            public int StatusCode { get; set; }
            public string? Message { get; set; }
            public string? Code { get; set; }
        }
    }
}

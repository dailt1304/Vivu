using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.Interfaces.Auth;

namespace Vivu.IntegrationTests.Locations
{
    [Collection("Integration Tests")]
    public class GetLocationByIdIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly GeometryFactory _geometryFactory;

        public GetLocationByIdIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        }

        public async Task InitializeAsync()
        {
            await CleanupDatabaseAsync();
            await AuthenticateAsync(); 
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<LocationDetail>().ExecuteDeleteAsync();
            await _dbContext.Set<Domain.Entities.Location>().ExecuteDeleteAsync();
            await _dbContext.Set<LocationCategory>().ExecuteDeleteAsync();
            await _dbContext.Set<City>().ExecuteDeleteAsync();
            await _dbContext.Set<Country>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
        }

        private async Task AuthenticateAsync()
        {
            var userId = Guid.NewGuid();
            var passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var user = User.Create(
                email: "testuser@example.com",
                passwordHash: passwordHasher.HashPassword("Password123!"),
                fullName: "Test User"
            );

            typeof(User).GetProperty("Id")!.SetValue(user, userId);
            user.VerifyEmail();

            var role = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = "User",
                RoleDescription = "Standard User"
            };

            user.AssignRole(role.Id);

            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();

            var tokenService = _scope.ServiceProvider.GetRequiredService<IAuthTokenProcess>();
            var token = tokenService.GenerateToken(user, new[] { "User" });

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        #region Helper Methods (Create Data)
        private async Task<Domain.Entities.Location> CreateLocationAsync(
            string name, Guid cityId, Guid categoryId, double lat, double lon,
            bool isVerified = true, string? imagesJson = null)
        {
            var location = new Domain.Entities.Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                CityId = cityId,
                CategoryId = categoryId,
                Latitude = lat,
                Longitude = lon,
                LocationPoint = _geometryFactory.CreatePoint(new Coordinate(lon, lat)),
                IsVerified = isVerified,
                IsDeleted = false,
                RatingAverage = 5.0m,
                CreatedDate = DateTime.UtcNow
            };

            if (imagesJson != null)
            {
                location.LocationDetail = new LocationDetail
                {
                    LocationId = location.Id,
                    Images = imagesJson, 
                    OpeningHours = "8:00 - 22:00",
                    Phone = "0909090909"
                };
            }

            await _dbContext.Set<Domain.Entities.Location>().AddAsync(location);
            await _dbContext.SaveChangesAsync();
            return location;
        }
        #endregion

        [Fact]
        public async Task GetById_ShouldReturnLocation_WithImagesAndNearby_WhenExistsAndVerified()
        {
            var country = new Country { Id = Guid.NewGuid(), Name = "Vietnam", Code = "VN" };
            var city = new City { Id = Guid.NewGuid(), Name = "Hanoi", CountryId = country.Id, Latitude = 21, Longitude = 105 };
            var category = new LocationCategory { Id = Guid.NewGuid(), Name = "Cafe" };

            await _dbContext.AddRangeAsync(country, city, category);
            await _dbContext.SaveChangesAsync();

            var targetLat = 21.0285;
            var targetLon = 105.8542;
            var targetLocation = await CreateLocationAsync(
                "Target Cafe", city.Id, category.Id, targetLat, targetLon,
                isVerified: true,
                imagesJson: "[\"image1.jpg\", \"image2.jpg\"]");

            await CreateLocationAsync("Nearby Cafe", city.Id, category.Id, 21.0290, 105.8545, true);

            await CreateLocationAsync("Far Cafe", city.Id, category.Id, 21.8000, 105.9000, true);

            var response = await _client.GetAsync($"/api/Locations/{targetLocation.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<LocationDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(targetLocation.Id);
            apiResponse.Data.Name.Should().Be("Target Cafe");

            apiResponse.Data.LocationDetail.Should().NotBeNull();

            apiResponse.Data.NearbyLocations.Should().NotBeNull();
            apiResponse.Data.NearbyLocations.Should().HaveCount(1);
            apiResponse.Data.NearbyLocations!.First().Name.Should().Be("Nearby Cafe");
            apiResponse.Data.NearbyLocations!.First().DistanceInMeters.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetById_ShouldReturnFailure_WhenLocationNotVerified()
        {
            var country = new Country { Id = Guid.NewGuid(), Name = "VN", Code = "VN" };
            var city = new City { Id = Guid.NewGuid(), Name = "DN", CountryId = country.Id };
            var category = new LocationCategory { Id = Guid.NewGuid(), Name = "Food" };
            await _dbContext.AddRangeAsync(country, city, category);
            await _dbContext.SaveChangesAsync();

            var unverifiedLocation = await CreateLocationAsync(
                "Pending Approval", city.Id, category.Id, 10, 10,
                isVerified: false); 

            var response = await _client.GetAsync($"/api/Locations/{unverifiedLocation.Id}");


            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<LocationDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Contain("verified"); 
            apiResponse.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var randomId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/Locations/{randomId}");


            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<LocationDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Contain("not found");
        }
    }
}

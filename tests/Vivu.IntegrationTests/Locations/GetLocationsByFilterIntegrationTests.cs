using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;

namespace Vivu.IntegrationTests.Locations
{

    [Collection("Integration Tests")]
    public class GetLocationsByFilterIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private string _accessToken = string.Empty;
        private readonly VivuDbContext _dbContext;
        private readonly GeometryFactory _geometryFactory;

        public GetLocationsByFilterIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        }
        private class TestPaginatedList<T>
        {
            public List<T> Items { get; set; }
            public int PageNumber { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
            public bool HasNextPage { get; set; }
            public bool HasPreviousPage { get; set; }
        }

        public async Task InitializeAsync()
        {
            await CleanupDatabaseAsync();
            var (userId, token) = await CreateAndAuthenticateTestUserAsync();
            _accessToken = token;

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
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
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
            await _dbContext.Set<Country>().ExecuteDeleteAsync();
        }

        #region Helper Methods

        private async Task<Country> CreateTestCountryAsync(string name = "Vietnam")
        {
            var country = new Country
            {
                Id = Guid.NewGuid(),
                Name = name,
                Code = "VN",
                CreatedDate = DateTime.UtcNow
            };

            await _dbContext.Set<Country>().AddAsync(country);
            await _dbContext.SaveChangesAsync();
            return country;
        }

        private async Task<City> CreateTestCityAsync(Guid countryId, string name, double latitude, double longitude)
        {
            var city = new City
            {
                Id = Guid.NewGuid(),
                Name = name,
                CountryId = countryId,
                Latitude = (decimal)latitude,
                Longitude = (decimal)longitude,
                CreatedDate = DateTime.UtcNow
            };

            await _dbContext.Set<City>().AddAsync(city);
            await _dbContext.SaveChangesAsync();
            return city;
        }

        private async Task<LocationCategory> CreateTestCategoryAsync(string name, string? iconUrl = null)
        {
            var category = new LocationCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                IconUrl = iconUrl,
                CreatedDate = DateTime.UtcNow
            };

            await _dbContext.Set<LocationCategory>().AddAsync(category);
            await _dbContext.SaveChangesAsync();
            return category;
        }

        private async Task<Domain.Entities.Location> CreateTestLocationAsync(
            string name,
            Guid cityId,
            Guid categoryId,
            double latitude,
            double longitude,
            decimal rating,
            bool isVerified = true,
            bool withDetail = false)
        {
            var location = new Domain.Entities.Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = $"Description for {name}",
                Address = $"Address of {name}",
                Latitude = latitude,
                Longitude = longitude,
                CityId = cityId,
                CategoryId = categoryId,
                RatingAverage = rating,
                RatingCount = (int)(rating * 20),
                IsVerified = isVerified,
                IsDeleted = false,
                LocationPoint = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude)),
                CreatedDate = DateTime.UtcNow
            };

            if (withDetail)
            {
                location.LocationDetail = new LocationDetail
                {
                    LocationId = location.Id,
                    OpeningHours = "9:00-22:00",
                    Phone = "0123456789",
                    Website = $"https://{name.ToLower().Replace(" ", "")}.com",
                    Tags = "test,location",
                    Images = "[\"img1.jpg\",\"img2.jpg\"]"
                };
            }

            await _dbContext.Set<Domain.Entities.Location>().AddAsync(location);
            await _dbContext.SaveChangesAsync();
            return location;
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task GetLocations_WithNoFilters_ShouldReturnAllLocations()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Location 1", city.Id, category.Id, 21.0285, 105.8542, 4.5m, true, true);
            await CreateTestLocationAsync("Location 2", city.Id, category.Id, 21.0300, 105.8550, 4.8m, false);
            await CreateTestLocationAsync("Location 3", city.Id, category.Id, 21.0250, 105.8530, 3.5m, true);

            // Act
            var response = await _client.GetAsync("/api/Locations/get-by-filter?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().HaveCountGreaterThanOrEqualTo(1);
            apiResponse.Data.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GetLocations_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            for (int i = 1; i <= 5; i++)
            {
                await CreateTestLocationAsync(
                    $"Location {i}",
                    city.Id,
                    category.Id,
                    21.0285 + (i * 0.001),
                    105.8542 + (i * 0.001),
                    4.0m + (i * 0.1m),
                    i % 2 == 0); // alternate verified/unverified
            }

            // Act
            var response = await _client.GetAsync("/api/Locations/get-by-filter?PageNumber=1&PageSize=2&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.PageSize.Should().Be(2);
            apiResponse.Data.HasNextPage.Should().BeTrue();
        }

        #endregion

        #region Filter Tests

        [Fact]
        public async Task GetLocations_WithCityFilter_ShouldReturnOnlyLocationsInCity()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city1 = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var city2 = await CreateTestCityAsync(country.Id, "Ho Chi Minh", 10.8231, 106.6297);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Hanoi Location 1", city1.Id, category.Id, 21.0285, 105.8542, 4.5m, false);
            await CreateTestLocationAsync("Hanoi Location 2", city1.Id, category.Id, 21.0300, 105.8550, 4.8m, false);
            await CreateTestLocationAsync("HCMC Location 1", city2.Id, category.Id, 10.8231, 106.6297, 4.2m, false);

            // Act
            var response = await _client.GetAsync($"/api/Locations/get-by-filter?CityId={city1.Id}&PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.Items.Should().OnlyContain(l => l.CityId == city1.Id);
        }

        [Fact]
        public async Task GetLocations_WithCategoryFilter_ShouldReturnOnlyLocationsInCategory()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category1 = await CreateTestCategoryAsync("Restaurant");
            var category2 = await CreateTestCategoryAsync("Cafe");

            await CreateTestLocationAsync("Restaurant 1", city.Id, category1.Id, 21.0285, 105.8542, 4.5m, false);
            await CreateTestLocationAsync("Restaurant 2", city.Id, category1.Id, 21.0300, 105.8550, 4.8m, false);
            await CreateTestLocationAsync("Cafe 1", city.Id, category2.Id, 21.0250, 105.8530, 4.2m, false);

            // Act
            var response = await _client.GetAsync($"/api/Locations/get-by-filter?CategoryId={category1.Id}&PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.Items.Should().OnlyContain(l => l.CategoryId == category1.Id);
        }

        [Fact]
        public async Task GetLocations_WithMinRatingFilter_ShouldReturnOnlyHighRatedLocations()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Low Rating", city.Id, category.Id, 21.0285, 105.8542, 3.5m, false);
            await CreateTestLocationAsync("High Rating 1", city.Id, category.Id, 21.0300, 105.8550, 4.5m, false);
            await CreateTestLocationAsync("High Rating 2", city.Id, category.Id, 21.0250, 105.8530, 4.8m, false);

            // Act
            var response = await _client.GetAsync("/api/Locations/get-by-filter?MinRating=4.0&PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.Items.Should().OnlyContain(l => l.RatingAverage >= 4.0m);
        }

        [Fact]
        public async Task GetLocations_WithVerifiedOnlyTrue_ShouldReturnOnlyVerifiedLocations()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Verified 1", city.Id, category.Id, 21.0285, 105.8542, 4.5m, true);
            await CreateTestLocationAsync("Unverified 1", city.Id, category.Id, 21.0300, 105.8550, 4.8m, false);
            await CreateTestLocationAsync("Verified 2", city.Id, category.Id, 21.0250, 105.8530, 4.2m, true);

            // Act
            var response = await _client.GetAsync("/api/Locations/get-by-filter?IsVerifiedOnly=true&PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.Items.Should().OnlyContain(l => l.IsVerified == true);
        }

        [Fact]
        public async Task GetLocations_WithVerifiedOnlyFalse_ShouldReturnOnlyUnverifiedLocations()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Verified 1", city.Id, category.Id, 21.0285, 105.8542, 4.5m, true);
            await CreateTestLocationAsync("Unverified 1", city.Id, category.Id, 21.0300, 105.8550, 4.8m, false);
            await CreateTestLocationAsync("Unverified 2", city.Id, category.Id, 21.0250, 105.8530, 4.2m, false);

            // Act
            var response = await _client.GetAsync("/api/Locations/get-by-filter?IsVerifiedOnly=false&PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.Items.Should().OnlyContain(l => l.IsVerified == false);
        }

        #endregion

        #region Distance Filter Tests

        [Fact]
        public async Task GetLocations_WithUserCoordinates_ShouldCalculateDistance()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Location 1", city.Id, category.Id, 21.0285, 105.8542, 4.5m, false);
            await CreateTestLocationAsync("Location 2", city.Id, category.Id, 21.0300, 105.8550, 4.8m, false);

            // Act
            var response = await _client.GetAsync(
                "/api/Locations/get-by-filter?UserLatitude=21.0285&UserLongitude=105.8542&PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().NotBeEmpty();
            apiResponse.Data.Items.Should().OnlyContain(l => l.DistanceInMeters.HasValue);
        }

        [Fact]
        public async Task GetLocations_WithRadiusFilter_ShouldReturnOnlyNearbyLocations()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            // Very close location
            await CreateTestLocationAsync("Very Close", city.Id, category.Id, 21.0285, 105.8542, 4.5m, false);
            // Moderately close location  
            await CreateTestLocationAsync("Moderate", city.Id, category.Id, 21.0286, 105.8543, 4.8m, false);
            // Far location
            await CreateTestLocationAsync("Far", city.Id, category.Id, 21.0500, 105.8700, 4.2m, false);

            // Act - 1km radius
            var response = await _client.GetAsync(
                "/api/Locations/get-by-filter?UserLatitude=21.0285&UserLongitude=105.8542&RadiusInMeters=0.02&PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Should return nearby locations only
            apiResponse!.Data!.Items.Should().NotBeEmpty();
            apiResponse.Data.Items.Should().NotContain(l => l.Name == "Far");
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task GetLocations_WithRatingSortDescending_ShouldSortByRatingDescending()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Low", city.Id, category.Id, 21.0285, 105.8542, 3.5m, false);
            await CreateTestLocationAsync("Medium", city.Id, category.Id, 21.0300, 105.8550, 4.5m, false);
            await CreateTestLocationAsync("High", city.Id, category.Id, 21.0250, 105.8530, 4.8m, false);

            // Act
            var response = await _client.GetAsync(
                "/api/Locations/get-by-filter?SortBy=rating&IsDescending=true&PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().BeInDescendingOrder(l => l.RatingAverage);
            apiResponse.Data.Items.First().RatingAverage.Should().Be(4.8m);
        }

        [Fact]
        public async Task GetLocations_WithDistanceSort_ShouldSortByDistanceAscending()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Far", city.Id, category.Id, 21.0400, 105.8600, 4.5m, false);
            await CreateTestLocationAsync("Close", city.Id, category.Id, 21.0286, 105.8543, 4.8m, false);
            await CreateTestLocationAsync("Very Close", city.Id, category.Id, 21.0285, 105.8542, 4.2m, false);

            // Act
            var response = await _client.GetAsync(
                "/api/Locations/get-by-filter?SortBy=distance&UserLatitude=21.0285&UserLongitude=105.8542&IsDescending=false&PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().BeInAscendingOrder(l => l.DistanceInMeters);
        }

        [Fact]
        public async Task GetLocations_WithNameSort_ShouldSortByNameAlphabetically()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Zebra Restaurant", city.Id, category.Id, 21.0285, 105.8542, 4.5m, false);
            await CreateTestLocationAsync("Alpha Cafe", city.Id, category.Id, 21.0300, 105.8550, 4.8m, false);
            await CreateTestLocationAsync("Beta Bistro", city.Id, category.Id, 21.0250, 105.8530, 4.2m, false);

            // Act
            var response = await _client.GetAsync(
                "/api/Locations/get-by-filter?SortBy=name&IsDescending=false&PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().BeInAscendingOrder(l => l.Name);
            apiResponse.Data.Items.First().Name.Should().Be("Alpha Cafe");
        }

        #endregion

        #region DTO Mapping Tests

        [Fact]
        public async Task GetLocations_ShouldIncludeCityInformation()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Location 1", city.Id, category.Id, 21.0285, 105.8542, 4.5m, false);

            // Act
            var response = await _client.GetAsync("/api/Locations/get-by-filter?PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var location = apiResponse!.Data!.Items.First();
            location.City.Should().NotBeNull();
            location.City!.Name.Should().Be("Hanoi");
            location.City.Latitude.Should().Be((decimal)21.0285);
        }

        [Fact]
        public async Task GetLocations_ShouldIncludeCategoryInformation()
        {
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant", "restaurant-icon.png");

            await CreateTestLocationAsync("Location 1", city.Id, category.Id, 21.0285, 105.8542, 4.5m, false);

            // Act
            var response = await _client.GetAsync("/api/Locations/get-by-filter?PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var location = apiResponse!.Data!.Items.First();
            location.Category.Should().NotBeNull();
            location.Category!.Name.Should().Be("Restaurant");
            location.Category.IconUrl.Should().Be("restaurant-icon.png");
        }

        [Fact]
        public async Task GetLocations_ShouldIncludeLocationDetail_WhenPresent()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("With Detail", city.Id, category.Id, 21.0285, 105.8542, 4.5m, false, withDetail: true);

            // Act
            var response = await _client.GetAsync("/api/Locations/get-by-filter?PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var location = apiResponse!.Data!.Items.First();
            location.LocationDetail.Should().NotBeNull();
            location.LocationDetail!.OpeningHours.Should().NotBeNullOrEmpty();
            location.LocationDetail.Phone.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Combined Filters Tests

        [Fact]
        public async Task GetLocations_WithMultipleFilters_ShouldApplyAllFilters()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city1 = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var city2 = await CreateTestCityAsync(country.Id, "HCMC", 10.8231, 106.6297);
            var category1 = await CreateTestCategoryAsync("Restaurant");
            var category2 = await CreateTestCategoryAsync("Cafe");

            await CreateTestLocationAsync("Match All", city1.Id, category1.Id, 21.0285, 105.8542, 4.5m, true);
            await CreateTestLocationAsync("Wrong City", city2.Id, category1.Id, 10.8231, 106.6297, 4.8m, true);
            await CreateTestLocationAsync("Wrong Category", city1.Id, category2.Id, 21.0300, 105.8550, 4.7m, true);
            await CreateTestLocationAsync("Low Rating", city1.Id, category1.Id, 21.0250, 105.8530, 3.5m, true);
            await CreateTestLocationAsync("Not Verified", city1.Id, category1.Id, 21.0270, 105.8560, 4.6m, false);

            // Act
            var response = await _client.GetAsync(
                $"/api/Locations/get-by-filter?CityId={city1.Id}&CategoryId={category1.Id}&MinRating=4.0&IsVerifiedOnly=true&PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().HaveCount(1);
            apiResponse.Data.Items.First().Name.Should().Be("Match All");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetLocations_WithNoResults_ShouldReturnEmptyList()
        {
            // Arrange
            var nonExistentCityId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync(
                $"/api/Locations/get-by-filter?CityId={nonExistentCityId}&PageNumber=1&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetLocations_WithInvalidPageNumber_ShouldReturnEmptyResults()
        {
            // Arrange
            var country = await CreateTestCountryAsync();
            var city = await CreateTestCityAsync(country.Id, "Hanoi", 21.0285, 105.8542);
            var category = await CreateTestCategoryAsync("Restaurant");

            await CreateTestLocationAsync("Location 1", city.Id, category.Id, 21.0285, 105.8542, 4.5m, false);

            // Act
            var response = await _client.GetAsync("/api/Locations/get-by-filter?PageNumber=999&PageSize=10&IsVerifiedOnly=false");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestPaginatedList<LocationDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse!.Data!.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().BeGreaterThan(0);
        }

        private async Task<(Guid userId, string token)> CreateAndAuthenticateTestUserAsync()
        {
            var userId = Guid.NewGuid();
            var passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var user = User.Create(
                email: "testuser@example.com",
                passwordHash: passwordHasher.HashPassword("Password123!"),
                fullName: "Test User"
            );

            // Override ID for consistency
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

            return (userId, token);
        }

        #endregion
    }
}

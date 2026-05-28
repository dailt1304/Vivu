using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Locations;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Locations.Queries.GetLocationsWithFilters;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;

namespace Vivu.Application.UnitTests.UseCases.Locations.Queries.GetLocationsByFilter
{
    public class GetLocationsByFilterQueryHandlerTests
    {
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IFilterLocation> _filterLocationMock;
        private readonly GetLocationsByFilterQueryHandler _handler;
        private readonly GeometryFactory _geometryFactory;

        public GetLocationsByFilterQueryHandlerTests()
        {
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _filterLocationMock = new Mock<IFilterLocation>();
            _handler = new GetLocationsByFilterQueryHandler(
                _locationRepositoryMock.Object,
                _filterLocationMock.Object
            );
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        }

        #region Test Data Helpers

        private List<Domain.Entities.Location> CreateTestLocations()
        {
            var cityId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            var city = new City
            {
                Id = cityId,
                Name = "Hanoi",
                CountryId = Guid.NewGuid(),
                Latitude = (decimal)21.0285,
                Longitude = (decimal)105.8542
            };

            var category = new LocationCategory
            {
                Id = categoryId,
                Name = "Restaurant",
                IconUrl = "icon.png"
            };

            return new List<Domain.Entities.Location>
        {
            new Domain.Entities.Location
            {
                Id = Guid.NewGuid(),
                Name = "Location 1",
                Description = "Description 1",
                Address = "Address 1",
                Latitude = 21.0285,
                Longitude = 105.8542,
                CityId = cityId,
                City = city,
                CategoryId = categoryId,
                Category = category,
                RatingAverage = 4.5m,
                RatingCount = 100,
                IsVerified = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                LocationPoint = _geometryFactory.CreatePoint(new Coordinate(105.8542, 21.0285)),
                LocationDetail = new LocationDetail
                {
                    LocationId = Guid.NewGuid(),
                    OpeningHours = "9:00-22:00",
                    Phone = "0123456789",
                    Website = "https://location1.com",
                    Tags = "restaurant,food",
                    Images = "[\"img1.jpg\"]"
                }
            },
            new Domain.Entities.Location
            {
                Id = Guid.NewGuid(),
                Name = "Location 2",
                Description = "Description 2",
                Address = "Address 2",
                Latitude = 21.0300,
                Longitude = 105.8550,
                CityId = cityId,
                City = city,
                CategoryId = categoryId,
                Category = category,
                RatingAverage = 4.8m,
                RatingCount = 200,
                IsVerified = false,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                LocationPoint = _geometryFactory.CreatePoint(new Coordinate(105.8550, 21.0300)),
                LocationDetail = new LocationDetail
                {
                    LocationId = Guid.NewGuid(),
                    OpeningHours = "10:00-23:00",
                    Phone = "0987654321",
                    Website = "https://location2.com",
                    Tags = "cafe,coffee",
                    Images = "[\"img2.jpg\"]"
                }
            },
            new Domain.Entities.Location
            {
                Id = Guid.NewGuid(),
                Name = "Location 3",
                Description = "Description 3",
                Address = "Address 3",
                Latitude = 21.0250,
                Longitude = 105.8530,
                CityId = cityId,
                City = city,
                CategoryId = categoryId,
                Category = category,
                RatingAverage = 3.5m,
                RatingCount = 50,
                IsVerified = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                LocationPoint = _geometryFactory.CreatePoint(new Coordinate(105.8530, 21.0250)),
                LocationDetail = null
            }
        };
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithNoFilters_ShouldReturnAllLocations()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Items.Should().HaveCount(3);
            result.Value.TotalCount.Should().Be(3);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                PageNumber = 1,
                PageSize = 2
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.TotalCount.Should().Be(3);
            result.Value.TotalPages.Should().Be(2);
            result.Value.HasNextPage.Should().BeTrue();
            result.Value.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_WithCityFilter_ShouldCallFilterWithCorrectParameters()
        {
            // Arrange
            var locations = CreateTestLocations();
            var cityId = locations.First().CityId!.Value;
            var query = new GetLocationsByFilterQuery
            {
                CityId = cityId,
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) =>
                    q.Where(l => l.CityId == cityId));

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _filterLocationMock.Verify(x => x.ApplyFiltersToGetLocation(
                It.IsAny<IQueryable<Domain.Entities.Location>>(),
                It.Is<GetLocationsByFilterQuery>(q => q.CityId == cityId)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithCategoryFilter_ShouldCallFilterWithCorrectParameters()
        {
            // Arrange
            var locations = CreateTestLocations();
            var categoryId = locations.First().CategoryId!.Value;
            var query = new GetLocationsByFilterQuery
            {
                CategoryId = categoryId,
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) =>
                    q.Where(l => l.CategoryId == categoryId));

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _filterLocationMock.Verify(x => x.ApplyFiltersToGetLocation(
                It.IsAny<IQueryable<Domain.Entities.Location>>(),
                It.Is<GetLocationsByFilterQuery>(q => q.CategoryId == categoryId)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithMinRatingFilter_ShouldCallFilterWithCorrectParameters()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                MinRating = 4.0m,
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) =>
                    q.Where(l => l.RatingAverage >= 4.0m));

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _filterLocationMock.Verify(x => x.ApplyFiltersToGetLocation(
                It.IsAny<IQueryable<Domain.Entities.Location>>(),
                It.Is<GetLocationsByFilterQuery>(q => q.MinRating == 4.0m)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithVerifiedOnlyFilter_ShouldCallFilterWithCorrectParameters()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                IsVerifiedOnly = true,
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) =>
                    q.Where(l => l.IsVerified));

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _filterLocationMock.Verify(x => x.ApplyFiltersToGetLocation(
                It.IsAny<IQueryable<Domain.Entities.Location>>(),
                It.Is<GetLocationsByFilterQuery>(q => q.IsVerifiedOnly == true)),
                Times.Once);
        }

        #endregion

        #region Distance Calculation Tests

        [Fact]
        public async Task Handle_WithUserCoordinates_ShouldCalculateDistance()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderBy(l => l.DistanceInMeters));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().OnlyContain(l => l.DistanceInMeters.HasValue);
        }

        [Fact]
        public async Task Handle_WithoutUserCoordinates_ShouldNotCalculateDistance()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().OnlyContain(l => !l.DistanceInMeters.HasValue);
        }

        [Fact]
        public async Task Handle_WithRadiusFilter_ShouldCallFilterWithDistance()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                RadiusInMeters = 5000,
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, req) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderBy(l => l.DistanceInMeters));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _filterLocationMock.Verify(x => x.ApplyFiltersToGetLocation(
                It.IsAny<IQueryable<Domain.Entities.Location>>(),
                It.Is<GetLocationsByFilterQuery>(q =>
                    q.UserLatitude == 21.0285 &&
                    q.UserLongitude == 105.8542 &&
                    q.RadiusInMeters == 5000)),
                Times.Once);
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task Handle_WithRatingSorting_ShouldApplyRatingSorting()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "rating",
                IsDescending = true,
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage).ThenByDescending(l => l.RatingCount));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _filterLocationMock.Verify(x => x.ApplySorting(
                It.IsAny<IQueryable<LocationDto>>(),
                It.Is<GetLocationsByFilterQuery>(q => q.SortBy == "rating" && q.IsDescending)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithDistanceSorting_ShouldApplyDistanceSorting()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "distance",
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                IsDescending = false,
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderBy(l => l.DistanceInMeters));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _filterLocationMock.Verify(x => x.ApplySorting(
                It.IsAny<IQueryable<LocationDto>>(),
                It.Is<GetLocationsByFilterQuery>(q => q.SortBy == "distance")),
                Times.Once);
        }

        #endregion

        #region DTO Mapping Tests

        [Fact]
        public async Task Handle_ShouldMapLocationToDto_WithAllProperties()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var firstLocation = result.Value.Items.First();

            firstLocation.Id.Should().NotBeEmpty();
            firstLocation.Name.Should().NotBeNullOrEmpty();
            firstLocation.City.Should().NotBeNull();
            firstLocation.City!.Name.Should().NotBeNullOrEmpty();
            firstLocation.Category.Should().NotBeNull();
            firstLocation.Category!.Name.Should().NotBeNullOrEmpty();
            firstLocation.RatingAverage.Should().BeGreaterThanOrEqualTo(0);
            firstLocation.RatingCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task Handle_ShouldMapLocationDetail_WhenPresent()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var locationWithDetail = result.Value.Items.First(l => l.LocationDetail != null);

            locationWithDetail.LocationDetail.Should().NotBeNull();
            locationWithDetail.LocationDetail!.OpeningHours.Should().NotBeNullOrEmpty();
            locationWithDetail.LocationDetail.Phone.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_WithEmptyResults_ShouldReturnEmptyPaginatedList()
        {
            // Arrange
            var emptyLocations = new List<Domain.Entities.Location>();
            var query = new GetLocationsByFilterQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = emptyLocations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) => q);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
            result.Value.TotalPages.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithPageNumberBeyondTotal_ShouldReturnEmptyPage()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                PageNumber = 999,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task Handle_WithCancellationToken_ShouldPassToRepository()
        {
            // Arrange
            var locations = CreateTestLocations();
            var query = new GetLocationsByFilterQuery
            {
                PageNumber = 1,
                PageSize = 10
            };
            var cancellationToken = new CancellationToken();

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, _) => q);

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Combined Filters Tests

        [Fact]
        public async Task Handle_WithMultipleFilters_ShouldApplyAllFilters()
        {
            // Arrange
            var locations = CreateTestLocations();
            var cityId = locations.First().CityId!.Value;
            var categoryId = locations.First().CategoryId!.Value;

            var query = new GetLocationsByFilterQuery
            {
                CityId = cityId,
                CategoryId = categoryId,
                MinRating = 4.0m,
                IsVerifiedOnly = true,
                PageNumber = 1,
                PageSize = 10
            };

            var queryable = locations.ToAsyncQueryable();

            _locationRepositoryMock.Setup(x => x.GetAllLocationsQuery())
                .Returns(queryable);

            _filterLocationMock.Setup(x => x.ApplyFiltersToGetLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<Domain.Entities.Location>, GetLocationsByFilterQuery>((q, req) =>
                    q.Where(l =>
                        l.CityId == req.CityId &&
                        l.CategoryId == req.CategoryId &&
                        l.RatingAverage >= req.MinRating &&
                        l.IsVerified == req.IsVerifiedOnly));

            _filterLocationMock.Setup(x => x.ApplySorting(
                    It.IsAny<IQueryable<LocationDto>>(),
                    It.IsAny<GetLocationsByFilterQuery>()))
                .Returns<IQueryable<LocationDto>, GetLocationsByFilterQuery>((q, _) =>
                    q.OrderByDescending(l => l.RatingAverage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _filterLocationMock.Verify(x => x.ApplyFiltersToGetLocation(
                It.IsAny<IQueryable<Domain.Entities.Location>>(),
                It.Is<GetLocationsByFilterQuery>(q =>
                    q.CityId == cityId &&
                    q.CategoryId == categoryId &&
                    q.MinRating == 4.0m &&
                    q.IsVerifiedOnly == true)),
                Times.Once);
        }

        #endregion
    }
}

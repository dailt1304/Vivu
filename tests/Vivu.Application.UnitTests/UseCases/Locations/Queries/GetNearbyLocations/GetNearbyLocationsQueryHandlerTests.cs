using AutoMapper;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Locations;
using Vivu.Application.UseCases.Locations.Queries.GetNearbyLocations;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Queries.GetNearbyLocations
{
    public class GetNearbyLocationsQueryHandlerTests
    {
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IFilterLocation> _filterLocationMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetNearbyLocationsQueryHandler _handler;
        private readonly GeometryFactory _geometryFactory;

        public GetNearbyLocationsQueryHandlerTests()
        {
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _filterLocationMock = new Mock<IFilterLocation>();
            _mapperMock = new Mock<IMapper>();
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

            _handler = new GetNearbyLocationsQueryHandler(
                _locationRepositoryMock.Object,
                _filterLocationMock.Object,
                _mapperMock.Object
            );
        }

        #region Helpers

        private static GetNearbyLocationsQuery CreateQuery(
            double lat = 21.0285,
            double lon = 105.8542,
            double radiusInMeters = 10000,
            int limit = 20,
            bool isVerifiedOnly = true,
            Guid? categoryId = null,
            decimal? minRating = null)
        {
            return new GetNearbyLocationsQuery
            {
                Latitude = lat,
                Longitude = lon,
                RadiusInMeters = radiusInMeters,
                Limit = limit,
                IsVerifiedOnly = isVerifiedOnly,
                CategoryId = categoryId,
                MinRating = minRating
            };
        }

        private Domain.Entities.Location CreateLocationWithPoint(
            string name,
            double lat,
            double lon,
            bool isVerified = true,
            decimal rating = 4.0m,
            Guid? categoryId = null)
        {
            var loc = Domain.Entities.Location.Create(
                name: name,
                description: null,
                address: $"Address of {name}",
                latitude: lat,
                longitude: lon,
                cityId: null,
                categoryId: categoryId,
                isVerified: isVerified
            );
            // Update RatingAverage via reflection since there's no setter
            typeof(Domain.Entities.Location).GetProperty("RatingAverage")!.SetValue(loc, rating);
            return loc;
        }

        private NearbyLocationDto ToNearbyDto(Domain.Entities.Location loc)
        {
            return new NearbyLocationDto
            {
                Id = loc.Id,
                Name = loc.Name,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                IsVerified = loc.IsVerified,
                DistanceInMeters = 100
            };
        }

        private void SetupFilterPassthrough(GetNearbyLocationsQuery query)
        {
            _filterLocationMock
                .Setup(x => x.ApplyFiltersToGetNearbyLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(), query))
                .Returns((IQueryable<Domain.Entities.Location> q, GetNearbyLocationsQuery r) => q);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithLocationsInRadius_ShouldReturnSuccess()
        {
            // Arrange
            var query = CreateQuery();
            var nearbyLoc = CreateLocationWithPoint("Nearby Cafe", 21.0290, 105.8545);
            var locations = new List<Domain.Entities.Location> { nearbyLoc };
            var expectedDtos = new List<NearbyLocationDto> { ToNearbyDto(nearbyLoc) };

            _locationRepositoryMock
                .Setup(x => x.GetPointLocation(query.Limit, It.IsAny<Point>()))
                .Returns(locations.AsQueryable().BuildMock());
            SetupFilterPassthrough(query);
            _mapperMock
                .Setup(x => x.Map<List<NearbyLocationDto>>(
                    It.IsAny<List<Domain.Entities.Location>>(),
                    It.IsAny<Action<IMappingOperationOptions>>()))
                .Returns(expectedDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().HaveCount(1);
            result.Value[0].Name.Should().Be("Nearby Cafe");
        }

        [Fact]
        public async Task Handle_WithNoLocations_ShouldReturnEmptyList()
        {
            // Arrange
            var query = CreateQuery();

            _locationRepositoryMock
                .Setup(x => x.GetPointLocation(query.Limit, It.IsAny<Point>()))
                .Returns(new List<Domain.Entities.Location>().AsQueryable().BuildMock());
            SetupFilterPassthrough(query);
            _mapperMock
                .Setup(x => x.Map<List<NearbyLocationDto>>(
                    It.IsAny<List<Domain.Entities.Location>>(),
                    It.IsAny<Action<IMappingOperationOptions>>()))
                .Returns(new List<NearbyLocationDto>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldPassCorrectLimitToRepository()
        {
            // Arrange
            var query = CreateQuery(limit: 5);

            _locationRepositoryMock
                .Setup(x => x.GetPointLocation(5, It.IsAny<Point>()))
                .Returns(new List<Domain.Entities.Location>().AsQueryable().BuildMock());
            SetupFilterPassthrough(query);
            _mapperMock
                .Setup(x => x.Map<List<NearbyLocationDto>>(
                    It.IsAny<List<Domain.Entities.Location>>(),
                    It.IsAny<Action<IMappingOperationOptions>>()))
                .Returns(new List<NearbyLocationDto>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(x => x.GetPointLocation(5, It.IsAny<Point>()), Times.Once);
        }

        #endregion

        #region Filter Tests

        [Fact]
        public async Task Handle_ShouldCallApplyFilters_WithCorrectQuery()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var query = CreateQuery(categoryId: categoryId, minRating: 3.5m, isVerifiedOnly: true);

            _locationRepositoryMock
                .Setup(x => x.GetPointLocation(query.Limit, It.IsAny<Point>()))
                .Returns(new List<Domain.Entities.Location>().AsQueryable().BuildMock());
            _filterLocationMock
                .Setup(x => x.ApplyFiltersToGetNearbyLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(), query))
                .Returns((IQueryable<Domain.Entities.Location> q, GetNearbyLocationsQuery r) => q);
            _mapperMock
                .Setup(x => x.Map<List<NearbyLocationDto>>(
                    It.IsAny<List<Domain.Entities.Location>>(),
                    It.IsAny<Action<IMappingOperationOptions>>()))
                .Returns(new List<NearbyLocationDto>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _filterLocationMock.Verify(
                x => x.ApplyFiltersToGetNearbyLocation(
                    It.IsAny<IQueryable<Domain.Entities.Location>>(), query),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldApplyRadiusFilter_ExcludingLocationsOutsideRadius()
        {
            // Arrange — very small radius (500m)
            var query = CreateQuery(lat: 21.0285, lon: 105.8542, radiusInMeters: 500);

            // Location very close (within ~100m)
            var closeLocation = CreateLocationWithPoint("Close Cafe", 21.0288, 105.8543);
            // Location far away (~20km)
            var farLocation = CreateLocationWithPoint("Far Cafe", 21.2000, 106.0000);

            var allLocations = new List<Domain.Entities.Location> { closeLocation, farLocation };

            _locationRepositoryMock
                .Setup(x => x.GetPointLocation(query.Limit, It.IsAny<Point>()))
                .Returns(allLocations.AsQueryable().BuildMock());
            SetupFilterPassthrough(query);

            List<Domain.Entities.Location>? capturedLocations = null;
            _mapperMock
                .Setup(x => x.Map<List<NearbyLocationDto>>(
                    It.IsAny<List<Domain.Entities.Location>>(),
                    It.IsAny<Action<IMappingOperationOptions>>()))
                .Callback<object, Action<IMappingOperationOptions>>(
                    (locs, opts) => capturedLocations = locs as List<Domain.Entities.Location>)
                .Returns(new List<NearbyLocationDto>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert — only close location should be passed to mapper
            capturedLocations.Should().NotBeNull();
            capturedLocations!.Should().ContainSingle();
            capturedLocations[0].Name.Should().Be("Close Cafe");
        }

        #endregion

        #region Mapper Tests

        [Fact]
        public async Task Handle_ShouldPassUserPointToMapper()
        {
            // Arrange
            var query = CreateQuery(lat: 21.0, lon: 105.0);
            var location = CreateLocationWithPoint("Test Location", 21.001, 105.001);

            _locationRepositoryMock
                .Setup(x => x.GetPointLocation(query.Limit, It.IsAny<Point>()))
                .Returns(new List<Domain.Entities.Location> { location }.AsQueryable().BuildMock());
            SetupFilterPassthrough(query);

            Point? capturedUserPoint = null;
            _mapperMock
                .Setup(x => x.Map<List<NearbyLocationDto>>(
                    It.IsAny<List<Domain.Entities.Location>>(),
                    It.IsAny<Action<IMappingOperationOptions>>()))
                .Callback<object, Action<IMappingOperationOptions>>((locs, optsAction) =>
                {
                    var opts = new Mock<IMappingOperationOptions>();
                    var itemsDict = new Dictionary<string, object>();
                    opts.Setup(o => o.Items).Returns(itemsDict);
                    optsAction(opts.Object);
                    if (itemsDict.ContainsKey("UserPoint"))
                        capturedUserPoint = itemsDict["UserPoint"] as Point;
                })
                .Returns(new List<NearbyLocationDto>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            capturedUserPoint.Should().NotBeNull();
            capturedUserPoint!.X.Should().BeApproximately(105.0, 0.0001); // longitude
            capturedUserPoint.Y.Should().BeApproximately(21.0, 0.0001);  // latitude
        }

        [Fact]
        public async Task Handle_ShouldReturnMappedDtos()
        {
            // Arrange
            var query = CreateQuery();
            var location = CreateLocationWithPoint("Mapped Location", 21.03, 105.86);
            var expectedDto = new NearbyLocationDto { Id = location.Id, Name = "Mapped Location", DistanceInMeters = 500 };

            _locationRepositoryMock
                .Setup(x => x.GetPointLocation(query.Limit, It.IsAny<Point>()))
                .Returns(new List<Domain.Entities.Location> { location }.AsQueryable().BuildMock());
            SetupFilterPassthrough(query);
            _mapperMock
                .Setup(x => x.Map<List<NearbyLocationDto>>(
                    It.IsAny<List<Domain.Entities.Location>>(),
                    It.IsAny<Action<IMappingOperationOptions>>()))
                .Returns(new List<NearbyLocationDto> { expectedDto });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle();
            result.Value[0].Name.Should().Be("Mapped Location");
            result.Value[0].DistanceInMeters.Should().Be(500);
        }

        #endregion

        #region Repository Interaction Tests

        [Fact]
        public async Task Handle_ShouldCallGetPointLocation_Once()
        {
            // Arrange
            var query = CreateQuery();

            _locationRepositoryMock
                .Setup(x => x.GetPointLocation(It.IsAny<int>(), It.IsAny<Point>()))
                .Returns(new List<Domain.Entities.Location>().AsQueryable().BuildMock());
            SetupFilterPassthrough(query);
            _mapperMock
                .Setup(x => x.Map<List<NearbyLocationDto>>(
                    It.IsAny<List<Domain.Entities.Location>>(),
                    It.IsAny<Action<IMappingOperationOptions>>()))
                .Returns(new List<NearbyLocationDto>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(
                x => x.GetPointLocation(It.IsAny<int>(), It.IsAny<Point>()),
                Times.Once);
        }

        #endregion
    }
}

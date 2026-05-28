using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.UseCases.Locations.Queries.GetLocatioinById;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;

namespace Vivu.Application.UnitTests.UseCases.Locations.Queries.GetLocationById
{
    public class GetLocationByIdQueryHandlerTests
    {
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetLocationByIdQueryHandler>> _loggerMock;
        private readonly GetLocationByIdQueryHandler _handler;
        private readonly GeometryFactory _geometryFactory;

        public GetLocationByIdQueryHandlerTests()
        {
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<GetLocationByIdQueryHandler>>();
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

            _handler = new GetLocationByIdQueryHandler(
                _locationRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenLocationNotFound()
        {
            var locationId = Guid.NewGuid();
            var query = new GetLocationByIdQuery(locationId);

            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync((Domain.Entities.Location)null!);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.NotFoundById(locationId));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not found")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenLocationIsNotVerified()
        {
            var locationId = Guid.NewGuid();
            var location = new Domain.Entities.Location { Id = locationId, IsVerified = false };
            var query = new GetLocationByIdQuery(locationId);

            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.NotVerified);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WithCorrectData_WhenLocationExistsAndVerified()
        {
            var locationId = Guid.NewGuid();
            var locationPoint = _geometryFactory.CreatePoint(new Coordinate(105.8, 21.0));

            var location = new Domain.Entities.Location
            {
                Id = locationId,
                IsVerified = true,
                LocationPoint = locationPoint,
                LocationDetail = new LocationDetail
                {
                    Images = "[\"img1.jpg\", \"img2.jpg\"]"
                }
            };

            var expectedDto = new LocationDto { Id = locationId };

            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);

            _mapperMock.Setup(x => x.Map<LocationDto>(location))
                .Returns(expectedDto);

            _locationRepositoryMock.Setup(x => x.GetNearbyLocationsAsync(
                    It.IsAny<Point>(), It.IsAny<Guid>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Domain.Entities.Location>());

            var query = new GetLocationByIdQuery(locationId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(expectedDto);

        }

        [Fact]
        public async Task Handle_ShouldHandleInvalidJsonImages_Gracefully()
        {
            var locationId = Guid.NewGuid();
            var location = new Domain.Entities.Location
            {
                Id = locationId,
                IsVerified = true,
                LocationDetail = new LocationDetail
                {
                    Images = "Invalid Json String"
                }
            };

            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mapperMock.Setup(x => x.Map<LocationDto>(location))
                .Returns(new LocationDto());

            var query = new GetLocationByIdQuery(locationId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue(); 

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Cannot deserialize images")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldMapNearbyLocations_WhenCoordinatesExist()
        {
            var locationId = Guid.NewGuid();
            var originPoint = _geometryFactory.CreatePoint(new Coordinate(100, 20));

            var location = new Domain.Entities.Location
            {
                Id = locationId,
                IsVerified = true,
                LocationPoint = originPoint
            };

            var nearbyId = Guid.NewGuid();
            var nearbyPoint = _geometryFactory.CreatePoint(new Coordinate(100.001, 20.001));
            var nearbyLocation = new Domain.Entities.Location { Id = nearbyId, LocationPoint = nearbyPoint };
            var nearbyList = new List<Domain.Entities.Location> { nearbyLocation };

            var nearbyDto = new NearbyLocationDto { Id = nearbyId };

            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);

            _locationRepositoryMock.Setup(x => x.GetNearbyLocationsAsync(
                    originPoint, locationId, It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(nearbyList);

            _mapperMock.Setup(x => x.Map<NearbyLocationDto>(nearbyLocation))
                .Returns(nearbyDto);

            _mapperMock.Setup(x => x.Map<LocationDto>(location))
                .Returns(new LocationDto()); 

            var query = new GetLocationByIdQuery(locationId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.NearbyLocations.Should().HaveCount(1);
            result.Value.NearbyLocations.First().Id.Should().Be(nearbyId);


            result.Value.NearbyLocations.First().DistanceInMeters.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Handle_ShouldNotFetchNearby_WhenLocationPointIsNull()
        {
            var locationId = Guid.NewGuid();
            var location = new Domain.Entities.Location
            {
                Id = locationId,
                IsVerified = true,
                LocationPoint = null
            };

            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mapperMock.Setup(x => x.Map<LocationDto>(location))
                .Returns(new LocationDto());

            var query = new GetLocationByIdQuery(locationId);

            await _handler.Handle(query, CancellationToken.None);

            _locationRepositoryMock.Verify(x => x.GetNearbyLocationsAsync(
                It.IsAny<Point>(), It.IsAny<Guid>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}

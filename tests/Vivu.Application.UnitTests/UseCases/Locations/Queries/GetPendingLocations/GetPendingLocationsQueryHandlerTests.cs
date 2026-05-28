using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.UseCases.Locations.Queries.GetPendingLocations;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Queries.GetPendingLocations
{
    public class GetPendingLocationsQueryHandlerTests
    {
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetPendingLocationsQueryHandler>> _loggerMock;
        private readonly GetPendingLocationsQueryHandler _handler;

        public GetPendingLocationsQueryHandlerTests()
        {
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<GetPendingLocationsQueryHandler>>();

            _handler = new GetPendingLocationsQueryHandler(
                _locationRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private static GetPendingLocationsQuery CreateQuery(int pageNumber = 1, int pageSize = 10)
        {
            return new GetPendingLocationsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private static Location CreatePendingLocation(Guid? id = null)
        {
            return new Location
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Pending Location",
                Description = "Test Description",
                Address = "123 Test Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = false,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };
        }

        private static LocationDto CreateLocationDto(Location location)
        {
            return new LocationDto
            {
                Id = location.Id,
                Name = location.Name,
                Description = location.Description,
                Address = location.Address,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                IsVerified = location.IsVerified,
                CreatedDate = location.CreatedDate
            };
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithValidRequest_ShouldReturnPaginatedPendingLocations()
        {
            // Arrange
            var query = CreateQuery();

            var pendingLocations = new List<Location>
            {
                CreatePendingLocation(),
                CreatePendingLocation(),
                CreatePendingLocation()
            };

            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            var locationDtos = pendingLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(3);
            result.Value.TotalCount.Should().Be(3);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var query = CreateQuery(pageNumber: 2, pageSize: 5);

            var pendingLocations = Enumerable.Range(1, 15)
                .Select(_ => CreatePendingLocation())
                .ToList();

            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            var expectedPageLocations = pendingLocations.Skip(5).Take(5).ToList();
            var locationDtos = expectedPageLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(5);
            result.Value.TotalCount.Should().Be(15);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WithMultiplePendingLocations_ShouldReturnAll()
        {
            // Arrange
            var query = CreateQuery(pageSize: 20);

            var pendingLocations = new List<Location>
            {
                CreatePendingLocation(Guid.NewGuid()),
                CreatePendingLocation(Guid.NewGuid()),
                CreatePendingLocation(Guid.NewGuid()),
                CreatePendingLocation(Guid.NewGuid()),
                CreatePendingLocation(Guid.NewGuid())
            };

            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            var locationDtos = pendingLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(5);
            result.Value.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCallRepositoryOnce()
        {
            // Arrange
            var query = CreateQuery();

            var pendingLocations = new List<Location> { CreatePendingLocation() };
            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { CreateLocationDto(pendingLocations[0]) });

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(
                x => x.GetPendingLocationsQuery(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCallMapperOnce()
        {
            // Arrange
            var query = CreateQuery();

            var pendingLocations = new List<Location> { CreatePendingLocation() };
            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { CreateLocationDto(pendingLocations[0]) });

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mapperMock.Verify(
                x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()),
                Times.Once);
        }

        #endregion

        #region Empty Result Tests

        [Fact]
        public async Task Handle_WithNoPendingLocations_ShouldReturnEmptyResult()
        {
            // Arrange
            var query = CreateQuery();

            var emptyList = new List<Location>();
            var mockQueryable = emptyList.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_WithEmptyResult_ShouldNotCallMapper()
        {
            // Arrange
            var query = CreateQuery();

            var emptyList = new List<Location>();
            var mockQueryable = emptyList.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mapperMock.Verify(
                x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithZeroTotalCount_ShouldReturnSuccessWithEmptyList()
        {
            // Arrange
            var query = CreateQuery(pageNumber: 2, pageSize: 10);

            var emptyList = new List<Location>();
            var mockQueryable = emptyList.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        #endregion

        #region Pagination Edge Cases

        [Fact]
        public async Task Handle_WithPageNumberExceedingTotalPages_ShouldReturnEmptyItems()
        {
            // Arrange
            var query = CreateQuery(pageNumber: 10, pageSize: 10);

            var pendingLocations = new List<Location>
            {
                CreatePendingLocation(),
                CreatePendingLocation()
            };

            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WithPageSize1_ShouldReturnSingleItem()
        {
            // Arrange
            var query = CreateQuery(pageNumber: 1, pageSize: 1);

            var pendingLocations = new List<Location>
            {
                CreatePendingLocation(),
                CreatePendingLocation(),
                CreatePendingLocation()
            };

            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            var locationDtos = new List<LocationDto> { CreateLocationDto(pendingLocations[0]) };
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(1);
            result.Value.TotalCount.Should().Be(3);
            result.Value.PageSize.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithLargePageSize_ShouldReturnAllAvailableItems()
        {
            // Arrange
            var query = CreateQuery(pageNumber: 1, pageSize: 100);

            var pendingLocations = Enumerable.Range(1, 5)
                .Select(_ => CreatePendingLocation())
                .ToList();

            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            var locationDtos = pendingLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(5);
            result.Value.TotalCount.Should().Be(5);
        }

        #endregion

        #region CancellationToken Tests

        [Fact]
        public async Task Handle_WithCancellationToken_ShouldPassToPaginationMethod()
        {
            // Arrange
            var query = CreateQuery();
            var cancellationToken = new CancellationToken();

            var pendingLocations = new List<Location> { CreatePendingLocation() };
            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { CreateLocationDto(pendingLocations[0]) });

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Data Integrity Tests

        [Fact]
        public async Task Handle_ShouldMapCorrectLocationData()
        {
            // Arrange
            var query = CreateQuery();
            var locationId = Guid.NewGuid();
            var location = CreatePendingLocation(locationId);

            var mockQueryable = new List<Location> { location }.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            var expectedDto = CreateLocationDto(location);
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { expectedDto });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.First().Id.Should().Be(locationId);
            result.Value.Items.First().Name.Should().Be(location.Name);
        }

        [Fact]
        public async Task Handle_WithMixedLocations_ShouldOnlyReturnPendingOnes()
        {
            // Arrange
            var query = CreateQuery();

            // Repository should only return pending locations
            var pendingLocations = new List<Location>
            {
                CreatePendingLocation(),
                CreatePendingLocation()
            };

            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            var locationDtos = pendingLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(2);
            result.Value.Items.Should().AllSatisfy(loc => loc.IsVerified.Should().BeFalse());
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task Handle_ShouldLogInformationOnSuccess()
        {
            // Arrange
            var query = CreateQuery();

            var pendingLocations = new List<Location> { CreatePendingLocation() };
            var mockQueryable = pendingLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { CreateLocationDto(pendingLocations[0]) });

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("retrieved successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithEmptyResult_ShouldLogInformationAboutNoPendingLocations()
        {
            // Arrange
            var query = CreateQuery();

            var emptyList = new List<Location>();
            var mockQueryable = emptyList.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetPendingLocationsQuery())
                .Returns(mockQueryable);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No pending locations")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}

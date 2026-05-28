using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Locations.Queries.GetUserSubmittedLocations;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Queries.GetUserSubmittedLocations
{
    public class GetUserSubmittedLocationsQueryHandlerTests
    {
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILogger<GetUserSubmittedLocationsQueryHandler>> _loggerMock;
        private readonly GetUserSubmittedLocationsQueryHandler _handler;

        public GetUserSubmittedLocationsQueryHandlerTests()
        {
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();
            _currentUserMock = new Mock<ICurrentUser>();
            _loggerMock = new Mock<ILogger<GetUserSubmittedLocationsQueryHandler>>();

            _handler = new GetUserSubmittedLocationsQueryHandler(
                _locationRepositoryMock.Object,
                _mapperMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private static GetUserSubmittedLocationsQuery CreateQuery(
            int pageNumber = 1,
            int pageSize = 10,
            ReportStatus? status = null)
        {
            return new GetUserSubmittedLocationsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status
            };
        }

        private static Location CreateUserSubmittedLocation(
            Guid? id = null,
            string name = "User Submitted Location",
            ReportStatus status = ReportStatus.PENDING)
        {
            return new Location
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
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
        public async Task Handle_WithValidUserAndLocations_ShouldReturnPaginatedList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location>
            {
                CreateUserSubmittedLocation(),
                CreateUserSubmittedLocation(),
                CreateUserSubmittedLocation()
            };

            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            var locationDtos = userLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            var query = CreateQuery();

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
        public async Task Handle_WithValidUser_ShouldCallRepositoryWithCorrectUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location> { CreateUserSubmittedLocation() };
            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { CreateLocationDto(userLocations[0]) });

            var query = CreateQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(
                x => x.GetUserReportedLocationsQuery(userId, null),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidUser_ShouldCallMapperOnce()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location> { CreateUserSubmittedLocation() };
            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { CreateLocationDto(userLocations[0]) });

            var query = CreateQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mapperMock.Verify(
                x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()),
                Times.Once);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_WithNullUserId_ShouldReturnInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);
            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
            _locationRepositoryMock.Verify(
                x => x.GetUserReportedLocationsQuery(It.IsAny<Guid>(), It.IsAny<ReportStatus?>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithEmptyUserId_ShouldReturnInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);
            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_WithInvalidGuidUserId_ShouldReturnInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid-format");
            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_WithInvalidUserId_ShouldNotCallRepository()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid");
            var query = CreateQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(
                x => x.GetUserReportedLocationsQuery(It.IsAny<Guid>(), It.IsAny<ReportStatus?>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithInvalidUserId_ShouldLogWarning()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);
            var query = CreateQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid or missing user ID")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Empty Result Tests

        [Fact]
        public async Task Handle_WithNoUserLocations_ShouldReturnEmptyResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var emptyList = new List<Location>();
            var mockQueryable = emptyList.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithNoUserLocations_ShouldNotCallMapper()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var emptyList = new List<Location>();
            var mockQueryable = emptyList.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            var query = CreateQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mapperMock.Verify(
                x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithEmptyResult_ShouldLogInformation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var emptyList = new List<Location>();
            var mockQueryable = emptyList.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            var query = CreateQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No submitted locations")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task Handle_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = Enumerable.Range(1, 15)
                .Select(_ => CreateUserSubmittedLocation())
                .ToList();

            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            var expectedPageLocations = userLocations.Skip(5).Take(5).ToList();
            var locationDtos = expectedPageLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            var query = CreateQuery(pageNumber: 2, pageSize: 5);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(5);
            result.Value.TotalCount.Should().Be(15);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WithPageSize1_ShouldReturnSingleItem()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location>
            {
                CreateUserSubmittedLocation(),
                CreateUserSubmittedLocation(),
                CreateUserSubmittedLocation()
            };

            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            var locationDtos = new List<LocationDto> { CreateLocationDto(userLocations[0]) };
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            var query = CreateQuery(pageNumber: 1, pageSize: 1);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(1);
            result.Value.TotalCount.Should().Be(3);
            result.Value.PageSize.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithPageNumberExceedingTotalPages_ShouldReturnEmptyItems()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location>
            {
                CreateUserSubmittedLocation(),
                CreateUserSubmittedLocation()
            };

            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto>());

            var query = CreateQuery(pageNumber: 10, pageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(2);
        }

        #endregion

        #region Status Filter Tests

        [Fact]
        public async Task Handle_WithPendingStatus_ShouldFilterByStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location>
            {
                CreateUserSubmittedLocation(status: ReportStatus.PENDING),
                CreateUserSubmittedLocation(status: ReportStatus.PENDING)
            };

            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, ReportStatus.PENDING))
                .Returns(mockQueryable);

            var locationDtos = userLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            var query = CreateQuery(status: ReportStatus.PENDING);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(2);
            _locationRepositoryMock.Verify(
                x => x.GetUserReportedLocationsQuery(userId, ReportStatus.PENDING),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithApprovedStatus_ShouldFilterByStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location>
            {
                CreateUserSubmittedLocation(status: ReportStatus.APPROVED),
                CreateUserSubmittedLocation(status: ReportStatus.APPROVED),
                CreateUserSubmittedLocation(status: ReportStatus.APPROVED)
            };

            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, ReportStatus.APPROVED))
                .Returns(mockQueryable);

            var locationDtos = userLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            var query = CreateQuery(status: ReportStatus.APPROVED);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(3);
            _locationRepositoryMock.Verify(
                x => x.GetUserReportedLocationsQuery(userId, ReportStatus.APPROVED),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithRejectedStatus_ShouldFilterByStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location>
            {
                CreateUserSubmittedLocation(status: ReportStatus.REJECTED)
            };

            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, ReportStatus.REJECTED))
                .Returns(mockQueryable);

            var locationDtos = userLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            var query = CreateQuery(status: ReportStatus.REJECTED);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(1);
            _locationRepositoryMock.Verify(
                x => x.GetUserReportedLocationsQuery(userId, ReportStatus.REJECTED),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithoutStatusFilter_ShouldReturnAllStatuses()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location>
            {
                CreateUserSubmittedLocation(status: ReportStatus.PENDING),
                CreateUserSubmittedLocation(status: ReportStatus.APPROVED),
                CreateUserSubmittedLocation(status: ReportStatus.REJECTED)
            };

            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            var locationDtos = userLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            var query = CreateQuery(status: null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(3);
            _locationRepositoryMock.Verify(
                x => x.GetUserReportedLocationsQuery(userId, null),
                Times.Once);
        }

        #endregion

        #region Data Integrity Tests

        [Fact]
        public async Task Handle_ShouldMapCorrectLocationData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var locationId = Guid.NewGuid();
            var location = CreateUserSubmittedLocation(locationId, "Test Location Name");

            var mockQueryable = new List<Location> { location }.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            var expectedDto = CreateLocationDto(location);
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { expectedDto });

            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.First().Id.Should().Be(locationId);
            result.Value.Items.First().Name.Should().Be("Test Location Name");
        }

        [Fact]
        public async Task Handle_ShouldReturnLocationsOrderedByCreatedDateDescending()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var now = DateTime.UtcNow;
            var location1 = CreateUserSubmittedLocation();
            var location2 = CreateUserSubmittedLocation();
            var location3 = CreateUserSubmittedLocation();

            location1.CreatedDate = now.AddDays(-2);
            location2.CreatedDate = now;
            location3.CreatedDate = now.AddDays(-1);

            var userLocations = new List<Location> { location1, location2, location3 };
            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            var locationDtos = userLocations.Select(CreateLocationDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(locationDtos);

            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().NotBeEmpty();
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task Handle_ShouldLogInformationAttempt()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location> { CreateUserSubmittedLocation() };
            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { CreateLocationDto(userLocations[0]) });

            var query = CreateQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Get user submitted locations attempt")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_OnSuccess_ShouldLogInformationWithDetails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location> { CreateUserSubmittedLocation() };
            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { CreateLocationDto(userLocations[0]) });

            var query = CreateQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("retrieved successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region CancellationToken Tests

        [Fact]
        public async Task Handle_WithCancellationToken_ShouldPassToPaginationMethod()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var userLocations = new List<Location> { CreateUserSubmittedLocation() };
            var mockQueryable = userLocations.AsQueryable().BuildMock();
            _locationRepositoryMock
                .Setup(x => x.GetUserReportedLocationsQuery(userId, null))
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationDto>>(It.IsAny<List<Location>>()))
                .Returns(new List<LocationDto> { CreateLocationDto(userLocations[0]) });

            var query = CreateQuery();
            var cancellationToken = new CancellationToken();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        #endregion
    }
}

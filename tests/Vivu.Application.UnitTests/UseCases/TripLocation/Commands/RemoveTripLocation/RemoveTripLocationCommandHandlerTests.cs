using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.TripLocation.Commands.RemoveTripLocation;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Application.Interfaces.Trips;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripLocation.Commands.RemoveTripLocation
{
    public class RemoveTripLocationCommandHandlerTests
    {
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ITripLocationRepository> _tripLocationRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<RemoveTripLocationHandler>> _loggerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ITripHubService> _tripHubServiceMock;
        private readonly RemoveTripLocationHandler _handler;

        public RemoveTripLocationCommandHandlerTests()
        {
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _tripLocationRepositoryMock = new Mock<ITripLocationRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<RemoveTripLocationHandler>>();
            _currentUserMock = new Mock<ICurrentUser>();
            _tripHubServiceMock = new Mock<ITripHubService>();

            _handler = new RemoveTripLocationHandler(
                _tripDayRepositoryMock.Object,
                _tripLocationRepositoryMock.Object,
                _tripMemberRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object,
                _tripHubServiceMock.Object);
        }

        #region Authentication Tests

        [Fact]
        public async Task Handle_UserNotAuthenticated_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new RemoveTripLocationCommand
            {
                TripLocationId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_UserIdIsNull_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new RemoveTripLocationCommand
            {
                TripLocationId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region TripLocation Validation Tests

        [Fact]
        public async Task Handle_TripLocationNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var command = new RemoveTripLocationCommand
            {
                TripLocationId = tripLocationId
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync((Domain.Entities.TripLocation)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("TripLocation.NotFound");
            result.Error.Message.Should().Contain(tripLocationId.ToString());
        }

        #endregion

        #region TripDay Validation Tests

        [Fact]
        public async Task Handle_TripDayNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();

            var tripLocation = Domain.Entities.TripLocation.Create(tripDayId, Guid.NewGuid(), 0);
            typeof(Domain.Entities.TripLocation).GetProperty("Id")!.SetValue(tripLocation, tripLocationId);

            var command = new RemoveTripLocationCommand
            {
                TripLocationId = tripLocationId
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(tripLocation);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundByTripDay(tripDayId));
        }

        #endregion

        #region Access Control Tests

        [Fact]
        public async Task Handle_TripOwnerNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var tripLocation = Domain.Entities.TripLocation.Create(tripDayId, Guid.NewGuid(), 0);
            typeof(Domain.Entities.TripLocation).GetProperty("Id")!.SetValue(tripLocation, tripLocationId);
            tripLocation.TripDay = tripDay;

            var command = new RemoveTripLocationCommand
            {
                TripLocationId = tripLocationId
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(tripLocation);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync((Guid?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));
        }

        [Fact]
        public async Task Handle_UserNotTripOwner_ReturnsAccessDeniedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var tripLocation = Domain.Entities.TripLocation.Create(tripDayId, Guid.NewGuid(), 0);
            typeof(Domain.Entities.TripLocation).GetProperty("Id")!.SetValue(tripLocation, tripLocationId);
            tripLocation.TripDay = tripDay;

            var command = new RemoveTripLocationCommand
            {
                TripLocationId = tripLocationId
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(tripLocation);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);
        }

        #endregion

        #region Successful Removal Tests

        [Fact]
        public async Task Handle_ValidCommand_RemovesAndReturnsTripLocation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Central Park" };
            var tripLocation = Domain.Entities.TripLocation.Create(tripDayId, locationId, 2);
            typeof(Domain.Entities.TripLocation).GetProperty("Id")!.SetValue(tripLocation, tripLocationId);
            tripLocation.TripDay = tripDay;
            tripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Central Park",
                OrderIndex = 2
            };

            var command = new RemoveTripLocationCommand
            {
                TripLocationId = tripLocationId
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(tripLocation);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(tripLocation))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(tripLocationId);
            result.Value.TripDayId.Should().Be(tripDayId);
            result.Value.LocationId.Should().Be(locationId);
            result.Value.LocationName.Should().Be("Central Park");
            result.Value.OrderIndex.Should().Be(2);

            _tripLocationRepositoryMock.Verify(x => x.Remove(tripLocation), Times.Once);
            _tripLocationRepositoryMock.Verify(x => x.ReorderAfterDeleteAsync(tripDayId, 2), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommandWithCompleteData_RemovesLocationSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Museum", Address = "123 Main St" };
            var tripLocation = Domain.Entities.TripLocation.Create(
                tripDayId, 
                locationId, 
                1, 
                new TimeSpan(9, 0, 0), 
                new TimeSpan(12, 0, 0), 
                "Morning visit", 
                "Car");
            typeof(Domain.Entities.TripLocation).GetProperty("Id")!.SetValue(tripLocation, tripLocationId);
            tripLocation.TripDay = tripDay;
            tripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Museum",
                LocationAddress = "123 Main St",
                OrderIndex = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                Note = "Morning visit",
                TransportMode = "Car"
            };

            var command = new RemoveTripLocationCommand
            {
                TripLocationId = tripLocationId
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(tripLocation);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(tripLocation))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.LocationName.Should().Be("Museum");
            result.Value.StartTime.Should().Be(new TimeSpan(9, 0, 0));
            result.Value.EndTime.Should().Be(new TimeSpan(12, 0, 0));
            result.Value.Note.Should().Be("Morning visit");
            result.Value.TransportMode.Should().Be("Car");
        }

        [Fact]
        public async Task Handle_ValidCommand_CallsAllRepositoriesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var orderIndex = 3;

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location" };
            var tripLocation = Domain.Entities.TripLocation.Create(tripDayId, locationId, orderIndex);
            typeof(Domain.Entities.TripLocation).GetProperty("Id")!.SetValue(tripLocation, tripLocationId);
            tripLocation.TripDay = tripDay;
            tripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Test Location",
                OrderIndex = orderIndex
            };

            var command = new RemoveTripLocationCommand
            {
                TripLocationId = tripLocationId
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(tripLocation);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(tripLocation))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripLocationRepositoryMock.Verify(x => x.GetByIdAsync(tripLocationId), Times.Once);
            _tripMemberRepositoryMock.Verify(x => x.GetOwnerIdByTripId(tripId), Times.Once);
            _tripLocationRepositoryMock.Verify(x => x.Remove(It.Is<Domain.Entities.TripLocation>(
                tl => tl.Id == tripLocationId)), Times.Once);
            _tripLocationRepositoryMock.Verify(x => x.ReorderAfterDeleteAsync(tripDayId, orderIndex), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
            _mapperMock.Verify(x => x.Map<TripLocationResponse>(tripLocation), Times.Once);
        }

        [Fact]
        public async Task Handle_RemovesLocationAtIndexZero_ReordersCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "First Location" };
            var tripLocation = Domain.Entities.TripLocation.Create(tripDayId, locationId, 0);
            typeof(Domain.Entities.TripLocation).GetProperty("Id")!.SetValue(tripLocation, tripLocationId);
            tripLocation.TripDay = tripDay;
            tripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "First Location",
                OrderIndex = 0
            };

            var command = new RemoveTripLocationCommand
            {
                TripLocationId = tripLocationId
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(tripLocation);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(tripLocation))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripLocationRepositoryMock.Verify(x => x.ReorderAfterDeleteAsync(tripDayId, 0), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_MapsResponseBeforeDeleting()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location" };
            var tripLocation = Domain.Entities.TripLocation.Create(tripDayId, locationId, 1);
            typeof(Domain.Entities.TripLocation).GetProperty("Id")!.SetValue(tripLocation, tripLocationId);
            tripLocation.TripDay = tripDay;
            tripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Test Location",
                OrderIndex = 1
            };

            var command = new RemoveTripLocationCommand
            {
                TripLocationId = tripLocationId
            };

            var callOrder = new List<string>();

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(tripLocation);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(tripLocation))
                .Returns(response)
                .Callback(() => callOrder.Add("Map"));
            _tripLocationRepositoryMock.Setup(x => x.Remove(tripLocation))
                .Callback(() => callOrder.Add("Remove"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            callOrder.Should().HaveCount(2);
            callOrder[0].Should().Be("Map"); // Map should be called before Remove
            callOrder[1].Should().Be("Remove");
        }

        #endregion
    }
}

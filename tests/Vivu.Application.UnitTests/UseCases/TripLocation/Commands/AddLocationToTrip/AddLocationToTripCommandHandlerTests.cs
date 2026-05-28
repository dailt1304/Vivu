using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.TripLocation.Commands.AddLocationToTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Vivu.Application.Interfaces.Trips;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripLocation.Commands.AddLocationToTrip
{
    public class AddLocationToTripCommandHandlerTests
    {
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<ITripLocationRepository> _tripLocationRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<AddLocationToTripHandler>> _loggerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ITripHubService> _tripHubServiceMock;
        private readonly AddLocationToTripHandler _handler;

        public AddLocationToTripCommandHandlerTests()
        {
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _tripLocationRepositoryMock = new Mock<ITripLocationRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<AddLocationToTripHandler>>();
            _currentUserMock = new Mock<ICurrentUser>();
            _tripHubServiceMock = new Mock<ITripHubService>();

            _handler = new AddLocationToTripHandler(
                _tripDayRepositoryMock.Object,
                _locationRepositoryMock.Object,
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
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 1
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
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 1
            };

            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region TripDay Validation Tests

        [Fact]
        public async Task Handle_TripDayNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = Guid.NewGuid(),
                OrderIndex = 1
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync((TripDay)null!);

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
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = Guid.NewGuid(),
                OrderIndex = 1
            };

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
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
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = Guid.NewGuid(),
                OrderIndex = 1
            };

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);
        }

        #endregion

        #region Location Validation Tests

        [Fact]
        public async Task Handle_LocationNotFound_ReturnsLocationNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1
            };

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync((Location)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.NotFoundById(locationId));
        }

        #endregion

        #region Time Conflict Tests

        [Fact]
        public async Task Handle_TimeConflictExists_ReturnsTimeConflictError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var startTime = new TimeSpan(9, 0, 0);
            var endTime = new TimeSpan(12, 0, 0);

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1,
                StartTime = startTime,
                EndTime = endTime
            };

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location" };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _tripLocationRepositoryMock.Setup(x => x.ExistsTimeConflictAsync(tripDayId, locationId, startTime, endTime, null))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.TripLocation.TimeConflict);
        }

        [Fact]
        public async Task Handle_NoTimeConflict_ProceedsSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var startTime = new TimeSpan(9, 0, 0);
            var endTime = new TimeSpan(12, 0, 0);

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1,
                StartTime = startTime,
                EndTime = endTime,
                Note = "Visit museum",
                TransportMode = "Walk"
            };

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location", Address = "123 Test St" };
            var createdTripLocation = Domain.Entities.TripLocation.Create(
                tripDayId, locationId, 1, startTime, endTime, "Visit museum", "Walk");
            createdTripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = createdTripLocation.Id,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Test Location",
                OrderIndex = 1,
                StartTime = startTime,
                EndTime = endTime,
                Note = "Visit museum",
                TransportMode = "Walk"
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _tripLocationRepositoryMock.Setup(x => x.ExistsTimeConflictAsync(tripDayId, locationId, startTime, endTime, null))
                .ReturnsAsync(false);
            _tripLocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripLocation>()))
                .ReturnsAsync(createdTripLocation);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.LocationName.Should().Be("Test Location");
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_OnlyStartTimeProvided_SkipsTimeConflictCheck()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var startTime = new TimeSpan(9, 0, 0);

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1,
                StartTime = startTime
            };

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location" };
            var createdTripLocation = Domain.Entities.TripLocation.Create(
                tripDayId, locationId, 1, startTime);
            createdTripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = createdTripLocation.Id,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Test Location",
                OrderIndex = 1,
                StartTime = startTime
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _tripLocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripLocation>()))
                .ReturnsAsync(createdTripLocation);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // Verify ExistsTimeConflictAsync was never called
            _tripLocationRepositoryMock.Invocations.Should().NotContain(
                inv => inv.Method.Name == nameof(ITripLocationRepository.ExistsTimeConflictAsync));
        }

        #endregion

        #region Successful Creation Tests

        [Fact]
        public async Task Handle_ValidCommand_CreatesAndReturnsTripLocation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 2,
                Note = "Morning visit",
                TransportMode = "Car"
            };

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Central Park", Address = "NYC" };
            var createdTripLocation = Domain.Entities.TripLocation.Create(
                tripDayId, locationId, 2, null, null, "Morning visit", "Car");
            createdTripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = createdTripLocation.Id,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Central Park",
                LocationAddress = "NYC",
                OrderIndex = 2,
                Note = "Morning visit",
                TransportMode = "Car"
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _tripLocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripLocation>()))
                .ReturnsAsync(createdTripLocation);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(createdTripLocation.Id);
            result.Value.TripDayId.Should().Be(tripDayId);
            result.Value.LocationId.Should().Be(locationId);
            result.Value.LocationName.Should().Be("Central Park");
            result.Value.OrderIndex.Should().Be(2);
            result.Value.Note.Should().Be("Morning visit");
            result.Value.TransportMode.Should().Be("Car");

            _tripLocationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.TripLocation>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommandWithMinimalData_CreatesLocationSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1
            };

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Basic Location" };
            var createdTripLocation = Domain.Entities.TripLocation.Create(tripDayId, locationId, 1);
            createdTripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = createdTripLocation.Id,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Basic Location",
                OrderIndex = 1
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _tripLocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripLocation>()))
                .ReturnsAsync(createdTripLocation);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.LocationName.Should().Be("Basic Location");
            result.Value.StartTime.Should().BeNull();
            result.Value.EndTime.Should().BeNull();
            result.Value.Note.Should().BeNull();
            result.Value.TransportMode.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ValidCommand_CallsAllRepositoriesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var command = new AddLocationToTripCommand
            {
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1
            };

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location" };
            var createdTripLocation = Domain.Entities.TripLocation.Create(tripDayId, locationId, 1);
            createdTripLocation.Location = location;

            var response = new TripLocationResponse
            {
                Id = createdTripLocation.Id,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Test Location",
                OrderIndex = 1
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _tripLocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripLocation>()))
                .ReturnsAsync(createdTripLocation);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripDayRepositoryMock.Verify(x => x.GetByIdAsync(tripDayId), Times.Once);
            _tripMemberRepositoryMock.Verify(x => x.GetOwnerIdByTripId(tripId), Times.Once);
            _locationRepositoryMock.Verify(x => x.GetByIdAsync(locationId), Times.Once);
            _tripLocationRepositoryMock.Verify(x => x.AddAsync(It.Is<Domain.Entities.TripLocation>(
                tl => tl.TripDayId == tripDayId && 
                      tl.LocationId == locationId && 
                      tl.OrderIndex == 1)), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
            _mapperMock.Verify(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()), Times.Once);
        }

        #endregion
    }
}

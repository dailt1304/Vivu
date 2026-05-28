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
using Vivu.Application.UseCases.TripLocation.Commands.UpdateTripLocation;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Vivu.Application.Interfaces.Trips;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripLocation.Commands.UpdateTripLocation
{
    public class UpdateTripLocationCommandHandlerTests
    {
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<ITripLocationRepository> _tripLocationRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<UpdateTripLocationHandler>> _loggerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ITripHubService> _tripHubServiceMock;
        private readonly UpdateTripLocationHandler _handler;

        public UpdateTripLocationCommandHandlerTests()
        {
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _tripLocationRepositoryMock = new Mock<ITripLocationRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<UpdateTripLocationHandler>>();
            _currentUserMock = new Mock<ICurrentUser>();
            _tripHubServiceMock = new Mock<ITripHubService>();

            _handler = new UpdateTripLocationHandler(
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
            var command = new UpdateTripLocationCommand
            {
                TripLocationId = Guid.NewGuid(),
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
            var command = new UpdateTripLocationCommand
            {
                TripLocationId = Guid.NewGuid(),
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

        #region TripLocation Validation Tests

        [Fact]
        public async Task Handle_TripLocationNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 1
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
            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = Guid.NewGuid(),
                OrderIndex = 1
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
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
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = Guid.NewGuid(),
                OrderIndex = 1
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
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
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = Guid.NewGuid(),
                OrderIndex = 1
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
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
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
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
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var startTime = new TimeSpan(9, 0, 0);
            var endTime = new TimeSpan(12, 0, 0);

            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1,
                StartTime = startTime,
                EndTime = endTime
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location" };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _tripLocationRepositoryMock.Setup(x => x.ExistsTimeConflictAsync(
                tripDayId, locationId, startTime, endTime, tripLocationId))
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
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var startTime = new TimeSpan(9, 0, 0);
            var endTime = new TimeSpan(12, 0, 0);

            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 2,
                StartTime = startTime,
                EndTime = endTime,
                Note = "Updated visit",
                TransportMode = "Bus"
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Updated Location", Address = "456 New St" };

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Updated Location",
                OrderIndex = 2,
                StartTime = startTime,
                EndTime = endTime,
                Note = "Updated visit",
                TransportMode = "Bus"
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _tripLocationRepositoryMock.Setup(x => x.ExistsTimeConflictAsync(
                tripDayId, locationId, startTime, endTime, tripLocationId))
                .ReturnsAsync(false);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.LocationName.Should().Be("Updated Location");
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_OnlyStartTimeProvided_SkipsTimeConflictCheck()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid(); 
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var startTime = new TimeSpan(9, 0, 0);

            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1,
                StartTime = startTime
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location" };

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Test Location",
                OrderIndex = 1,
                StartTime = startTime
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
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

        [Fact]
        public async Task Handle_NoTimesProvided_SkipsTimeConflictCheck()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1,
                StartTime = null,
                EndTime = null
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location" };

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Test Location",
                OrderIndex = 1
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
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

        #region Successful Update Tests

        [Fact]
        public async Task Handle_ValidCommand_UpdatesAndReturnsTripLocation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 3,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(16, 30, 0),
                Note = "Afternoon museum visit",
                TransportMode = "Metro"
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Art Museum", Address = "Downtown" };

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Art Museum",
                LocationAddress = "Downtown",
                OrderIndex = 3,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(16, 30, 0),
                Note = "Afternoon museum visit",
                TransportMode = "Metro"
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _tripLocationRepositoryMock.Setup(x => x.ExistsTimeConflictAsync(
                tripDayId, locationId, command.StartTime.Value, command.EndTime.Value, tripLocationId))
                .ReturnsAsync(false);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(tripLocationId);
            result.Value.TripDayId.Should().Be(tripDayId);
            result.Value.LocationId.Should().Be(locationId);
            result.Value.LocationName.Should().Be("Art Museum");
            result.Value.OrderIndex.Should().Be(3);
            result.Value.StartTime.Should().Be(new TimeSpan(14, 0, 0));
            result.Value.EndTime.Should().Be(new TimeSpan(16, 30, 0));
            result.Value.Note.Should().Be("Afternoon museum visit");
            result.Value.TransportMode.Should().Be("Metro");

            _tripLocationRepositoryMock.Verify(x => x.Update(It.Is<Domain.Entities.TripLocation>(
                tl => tl.Id == tripLocationId)), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommandWithMinimalData_UpdatesLocationSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 1
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 5);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Basic Location" };

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Basic Location",
                OrderIndex = 1
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.LocationName.Should().Be("Basic Location");
            result.Value.OrderIndex.Should().Be(1);
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
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                OrderIndex = 2
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1);
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Test Location" };

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = locationId,
                LocationName = "Test Location",
                OrderIndex = 2
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripLocationRepositoryMock.Verify(x => x.GetByIdAsync(tripLocationId), Times.Once);
            _tripDayRepositoryMock.Verify(x => x.GetByIdAsync(tripDayId), Times.Once);
            _tripMemberRepositoryMock.Verify(x => x.GetOwnerIdByTripId(tripId), Times.Once);
            _locationRepositoryMock.Verify(x => x.GetByIdAsync(locationId), Times.Once);
            _tripLocationRepositoryMock.Verify(x => x.Update(It.Is<Domain.Entities.TripLocation>(
                tl => tl.Id == tripLocationId && 
                      tl.TripDayId == tripDayId && 
                      tl.LocationId == locationId && 
                      tl.OrderIndex == 2)), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
            _mapperMock.Verify(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateToDifferentTripDay_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var oldTripDayId = Guid.NewGuid();
            var newTripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = newTripDayId,
                LocationId = locationId,
                OrderIndex = 1
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                oldTripDayId, locationId, 3);
            existingTripLocation.Id = tripLocationId;
            var newTripDay = new TripDay { Id = newTripDayId, TripId = tripId };
            var location = new Location { Id = locationId, Name = "Moved Location" };

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = newTripDayId,
                LocationId = locationId,
                LocationName = "Moved Location",
                OrderIndex = 1
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(newTripDayId))
                .ReturnsAsync(newTripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TripDayId.Should().Be(newTripDayId);
            _tripLocationRepositoryMock.Verify(x => x.Update(It.Is<Domain.Entities.TripLocation>(
                tl => tl.TripDayId == newTripDayId)), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateLocationAndTimes_UpdatesAllFieldsSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripLocationId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var oldLocationId = Guid.NewGuid();
            var newLocationId = Guid.NewGuid();

            var command = new UpdateTripLocationCommand
            {
                TripLocationId = tripLocationId,
                TripDayId = tripDayId,
                LocationId = newLocationId,
                OrderIndex = 5,
                StartTime = new TimeSpan(10, 30, 0),
                EndTime = new TimeSpan(13, 45, 0),
                Note = "Changed location and time",
                TransportMode = "Taxi"
            };

            var existingTripLocation = Domain.Entities.TripLocation.Create(
                tripDayId, oldLocationId, 2, new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0));
            existingTripLocation.Id = tripLocationId;
            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var newLocation = new Location { Id = newLocationId, Name = "New Location" };

            var response = new TripLocationResponse
            {
                Id = tripLocationId,
                TripDayId = tripDayId,
                LocationId = newLocationId,
                LocationName = "New Location",
                OrderIndex = 5,
                StartTime = new TimeSpan(10, 30, 0),
                EndTime = new TimeSpan(13, 45, 0),
                Note = "Changed location and time",
                TransportMode = "Taxi"
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripLocationRepositoryMock.Setup(x => x.GetByIdAsync(tripLocationId))
                .ReturnsAsync(existingTripLocation);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(newLocationId))
                .ReturnsAsync(newLocation);
            _tripLocationRepositoryMock.Setup(x => x.ExistsTimeConflictAsync(
                tripDayId, newLocationId, command.StartTime.Value, command.EndTime.Value, tripLocationId))
                .ReturnsAsync(false);
            _mapperMock.Setup(x => x.Map<TripLocationResponse>(It.IsAny<Domain.Entities.TripLocation>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.LocationId.Should().Be(newLocationId);
            result.Value.LocationName.Should().Be("New Location");
            result.Value.OrderIndex.Should().Be(5);
            result.Value.StartTime.Should().Be(new TimeSpan(10, 30, 0));
            result.Value.EndTime.Should().Be(new TimeSpan(13, 45, 0));
            result.Value.Note.Should().Be("Changed location and time");
            result.Value.TransportMode.Should().Be("Taxi");
        }

        #endregion
    }
}

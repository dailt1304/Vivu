using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.TripDay.Commands.RemoveTripDay;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;
using TripLocationEntity = Vivu.Domain.Entities.TripLocation;

namespace Vivu.Application.UnitTests.UseCases.TripDays.Commands.RemoveTripDay
{
    public class RemoveTripDayCommandHandlerTests
    {
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ITripLocationRepository> _tripLocationRepositoryMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<RemoveTripDayHandler>> _loggerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly RemoveTripDayHandler _handler;

        public RemoveTripDayCommandHandlerTests()
        {
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _tripLocationRepositoryMock = new Mock<ITripLocationRepository>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<RemoveTripDayHandler>>();
            _currentUserMock = new Mock<ICurrentUser>();

            _handler = new RemoveTripDayHandler(
                _tripDayRepositoryMock.Object,
                _tripLocationRepositoryMock.Object,
                _tripRepositoryMock.Object,
                _tripMemberRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidCommand_RemovesTripDaySuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(userId, tripId);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", 1);
            var expectedResponse = new TripDayResponse
            {
                Id = tripDayId,
                TripId = tripId,
                Title = "Day 1",
                DateIndex = 1
            };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripLocationRepository(tripDayId, new List<TripLocationEntity>());
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(tripDayId);

            _tripDayRepositoryMock.Verify(x => x.Remove(It.IsAny<TripDay>()), Times.Once);
            _tripDayRepositoryMock.Verify(x => x.ReorderDayIndexAfterUpdateAsync(tripId, 1), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TripDayWithLocations_RemovesTripDayAndAllLocations()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(userId, tripId);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", 1);
            var tripLocations = new List<TripLocationEntity>
            {
                CreateTripLocation(Guid.NewGuid(), tripDayId, "Location 1"),
                CreateTripLocation(Guid.NewGuid(), tripDayId, "Location 2"),
                CreateTripLocation(Guid.NewGuid(), tripDayId, "Location 3")
            };
            var expectedResponse = new TripDayResponse { Id = tripDayId };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripLocationRepository(tripDayId, tripLocations);
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripLocationRepositoryMock.Verify(x => x.Remove(It.IsAny<TripLocationEntity>()), Times.Exactly(3));
            _tripDayRepositoryMock.Verify(x => x.Remove(It.IsAny<TripDay>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TripDayWithoutLocations_RemovesOnlyTripDay()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(userId, tripId);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", 1);
            var expectedResponse = new TripDayResponse { Id = tripDayId };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripLocationRepository(tripDayId, new List<TripLocationEntity>());
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripLocationRepositoryMock.Verify(x => x.Remove(It.IsAny<TripLocationEntity>()), Times.Never);
            _tripDayRepositoryMock.Verify(x => x.Remove(It.IsAny<TripDay>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_CallsReorderDayIndexAfterUpdate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var dayIndex = 3;
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(userId, tripId);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 3", dayIndex);
            var expectedResponse = new TripDayResponse { Id = tripDayId };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripLocationRepository(tripDayId, new List<TripLocationEntity>());
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripDayRepositoryMock.Verify(x => x.ReorderDayIndexAfterUpdateAsync(tripId, dayIndex), Times.Once);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_UserNotAuthenticated_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new RemoveTripDayCommand { TripDayId = Guid.NewGuid() };
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new RemoveTripDayCommand { TripDayId = Guid.NewGuid() };
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        [Fact]
        public async Task Handle_EmptyUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new RemoveTripDayCommand { TripDayId = Guid.NewGuid() };
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        #endregion

        #region TripDay Not Found Tests

        [Fact]
        public async Task Handle_TripDayNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            SetupCurrentUser(userId);
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync((TripDay?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.NotFoundByTripDay(tripDayId).Code);
        }

        #endregion

        #region Trip Not Found Tests

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.NotFoundById(tripId).Code);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Handle_OwnerNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(userId, tripId);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync((Guid?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.NotFoundById(tripId).Code);
        }

        [Fact]
        public async Task Handle_UserNotOwner_ReturnsAccessDeniedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(ownerId, tripId);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.AccessDenied.Code);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_RemovingFirstTripDay_ReordersRemainingDays()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(userId, tripId);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", 1); // First day
            var expectedResponse = new TripDayResponse { Id = tripDayId };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripLocationRepository(tripDayId, new List<TripLocationEntity>());
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripDayRepositoryMock.Verify(x => x.ReorderDayIndexAfterUpdateAsync(tripId, 1), Times.Once);
        }

        [Fact]
        public async Task Handle_RemovingMiddleTripDay_ReordersRemainingDays()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(userId, tripId);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 5", 5); // Middle day
            var expectedResponse = new TripDayResponse { Id = tripDayId };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripLocationRepository(tripDayId, new List<TripLocationEntity>());
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripDayRepositoryMock.Verify(x => x.ReorderDayIndexAfterUpdateAsync(tripId, 5), Times.Once);
        }

        #endregion

        #region Helper Methods

        private void SetupCurrentUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
        }

        private void SetupTripDayRepository(TripDay tripDay)
        {
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDay.Id))
                .ReturnsAsync(tripDay);
            _tripDayRepositoryMock.Setup(x => x.Remove(It.IsAny<TripDay>()));
            _tripDayRepositoryMock.Setup(x => x.ReorderDayIndexAfterUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupTripRepository(Trip trip)
        {
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
        }

        private void SetupTripMemberRepository(Guid tripId, Guid ownerId)
        {
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
        }

        private void SetupTripLocationRepository(Guid tripDayId, List<TripLocationEntity> tripLocations)
        {
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(tripLocations);
            _tripLocationRepositoryMock.Setup(x => x.Remove(It.IsAny<TripLocationEntity>()));
        }

        private void SetupMapper(TripDayResponse response)
        {
            _mapperMock.Setup(x => x.Map<TripDayResponse>(It.IsAny<TripDay>()))
                .Returns(response);
        }

        private void SetupUnitOfWork()
        {
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        private static Trip CreateTrip(Guid userId, Guid tripId)
        {
            var trip = Trip.Create(
                userId: userId,
                title: "Test Trip",
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(10),
                isPublic: false
            );

            // Use reflection to set the Id
            var idProperty = typeof(Trip).GetProperty("Id");
            idProperty?.SetValue(trip, tripId);

            return trip;
        }

        private static TripDay CreateTripDay(Guid tripDayId, Guid tripId, string? title, int dayIndex)
        {
            var tripDay = TripDay.Create(
                TripId: tripId,
                Tittle: title,
                DayDate: DateTime.UtcNow,
                DayIndex: dayIndex
            );

            // Use reflection to set the Id
            var idProperty = typeof(TripDay).GetProperty("Id");
            idProperty?.SetValue(tripDay, tripDayId);

            return tripDay;
        }

        private static TripLocationEntity CreateTripLocation(Guid tripLocationId, Guid tripDayId, string note)
        {
            var tripLocation = TripLocationEntity.Create(
                tripDayId: tripDayId,
                locationId: Guid.NewGuid(), // Create a mock location ID
                orderIndex: 1,
                note: note
            );

            // Use reflection to set the Id
            var idProperty = typeof(TripLocationEntity).GetProperty("Id");
            idProperty?.SetValue(tripLocation, tripLocationId);

            return tripLocation;
        }

        #endregion
    }
}

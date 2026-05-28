using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.TripDay.Commands.ReorderTripDay;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripDays.Commands.ReorderTripDay
{
    public class ReorderTripDayCommandHandlerTests
    {
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ReorderTripDayHandler>> _loggerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly ReorderTripDayHandler _handler;

        public ReorderTripDayCommandHandlerTests()
        {
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ReorderTripDayHandler>>();
            _currentUserMock = new Mock<ICurrentUser>();

            _handler = new ReorderTripDayHandler(
                _tripDayRepositoryMock.Object,
                _tripMemberRepositoryMock.Object,
                _tripRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object
            );
        }

        #region Authentication Tests

        [Fact]
        public async Task Handle_UserNotAuthenticated_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Trip Existence Tests

        [Fact]
        public async Task Handle_TripNotFound_ReturnsTripNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            SetupCurrentUser(userId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Handle_OwnerNotFound_ReturnsTripNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            var trip = CreateTrip(userId, tripId);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync((Guid?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));
        }

        [Fact]
        public async Task Handle_UserNotOwner_ReturnsAccessDeniedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            var trip = CreateTrip(ownerId, tripId);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);
        }

        #endregion

        #region Trip Days Validation Tests

        [Fact]
        public async Task Handle_NoTripDaysFound_ReturnsTripNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            var trip = CreateTrip(userId, tripId);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(new List<TripDay>());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));
        }

        [Fact]
        public async Task Handle_InvalidTripDayIds_ReturnsInvalidIdsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId1 = Guid.NewGuid();
            var tripDayId2 = Guid.NewGuid();
            var invalidId = Guid.NewGuid();

            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { tripDayId1, invalidId }
            };

            var trip = CreateTrip(userId, tripId);
            var tripDays = new List<TripDay>
            {
                CreateTripDay(tripDayId1, tripId, "Day 1", 1),
                CreateTripDay(tripDayId2, tripId, "Day 2", 2)
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(tripDays);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TripDay.InvalidIds");
            result.Error.Message.Should().Contain("invalid");
        }

        [Fact]
        public async Task Handle_MismatchTripDayCount_ReturnsInvalidCountError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId1 = Guid.NewGuid();
            var tripDayId2 = Guid.NewGuid();
            var tripDayId3 = Guid.NewGuid();

            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { tripDayId1, tripDayId2 } // Missing tripDayId3
            };

            var trip = CreateTrip(userId, tripId);
            var tripDays = new List<TripDay>
            {
                CreateTripDay(tripDayId1, tripId, "Day 1", 1),
                CreateTripDay(tripDayId2, tripId, "Day 2", 2),
                CreateTripDay(tripDayId3, tripId, "Day 3", 3)
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(tripDays);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TripDay.InvalidCount");
            result.Error.Message.Should().Contain("Expected 3 trip days, but received 2");
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidReorderCommand_UpdatesDayIndexesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId1 = Guid.NewGuid();
            var tripDayId2 = Guid.NewGuid();
            var tripDayId3 = Guid.NewGuid();

            // Reorder: 3, 1, 2
            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { tripDayId3, tripDayId1, tripDayId2 }
            };

            var trip = CreateTrip(userId, tripId);
            var tripDay1 = CreateTripDay(tripDayId1, tripId, "Day 1", 1);
            var tripDay2 = CreateTripDay(tripDayId2, tripId, "Day 2", 2);
            var tripDay3 = CreateTripDay(tripDayId3, tripId, "Day 3", 3);
            var tripDays = new List<TripDay> { tripDay1, tripDay2, tripDay3 };

            var expectedResponses = new List<TripDayResponse>
            {
                new TripDayResponse { Id = tripDayId3, TripId = tripId, Title = "Day 3", DateIndex = 1 },
                new TripDayResponse { Id = tripDayId1, TripId = tripId, Title = "Day 1", DateIndex = 2 },
                new TripDayResponse { Id = tripDayId2, TripId = tripId, Title = "Day 2", DateIndex = 3 }
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(tripDays);
            _mapperMock.Setup(x => x.Map<List<TripDayResponse>>(It.IsAny<List<TripDay>>()))
                .Returns(expectedResponses);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().HaveCount(3);

            // Verify DayIndex updates
            tripDay3.DayIndex.Should().Be(1);
            tripDay1.DayIndex.Should().Be(2);
            tripDay2.DayIndex.Should().Be(3);

            // Verify repository calls
            _tripDayRepositoryMock.Verify(x => x.Update(It.IsAny<TripDay>()), Times.Exactly(3));
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_SingleTripDay_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();

            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { tripDayId }
            };

            var trip = CreateTrip(userId, tripId);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", 1);
            var tripDays = new List<TripDay> { tripDay };

            var expectedResponses = new List<TripDayResponse>
            {
                new TripDayResponse { Id = tripDayId, TripId = tripId, Title = "Day 1", DateIndex = 1 }
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(tripDays);
            _mapperMock.Setup(x => x.Map<List<TripDayResponse>>(It.IsAny<List<TripDay>>()))
                .Returns(expectedResponses);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            tripDay.DayIndex.Should().Be(1);
        }

        [Fact]
        public async Task Handle_ReverseTripDays_UpdatesDayIndexesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId1 = Guid.NewGuid();
            var tripDayId2 = Guid.NewGuid();
            var tripDayId3 = Guid.NewGuid();
            var tripDayId4 = Guid.NewGuid();

            // Reverse order: 4, 3, 2, 1
            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { tripDayId4, tripDayId3, tripDayId2, tripDayId1 }
            };

            var trip = CreateTrip(userId, tripId);
            var tripDay1 = CreateTripDay(tripDayId1, tripId, "Day 1", 1);
            var tripDay2 = CreateTripDay(tripDayId2, tripId, "Day 2", 2);
            var tripDay3 = CreateTripDay(tripDayId3, tripId, "Day 3", 3);
            var tripDay4 = CreateTripDay(tripDayId4, tripId, "Day 4", 4);
            var tripDays = new List<TripDay> { tripDay1, tripDay2, tripDay3, tripDay4 };

            var expectedResponses = new List<TripDayResponse>
            {
                new TripDayResponse { Id = tripDayId4, TripId = tripId, Title = "Day 4", DateIndex = 1 },
                new TripDayResponse { Id = tripDayId3, TripId = tripId, Title = "Day 3", DateIndex = 2 },
                new TripDayResponse { Id = tripDayId2, TripId = tripId, Title = "Day 2", DateIndex = 3 },
                new TripDayResponse { Id = tripDayId1, TripId = tripId, Title = "Day 1", DateIndex = 4 }
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(tripDays);
            _mapperMock.Setup(x => x.Map<List<TripDayResponse>>(It.IsAny<List<TripDay>>()))
                .Returns(expectedResponses);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(4);

            tripDay4.DayIndex.Should().Be(1);
            tripDay3.DayIndex.Should().Be(2);
            tripDay2.DayIndex.Should().Be(3);
            tripDay1.DayIndex.Should().Be(4);
        }

        [Fact]
        public async Task Handle_MovingFirstToLast_UpdatesDayIndexesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId1 = Guid.NewGuid();
            var tripDayId2 = Guid.NewGuid();
            var tripDayId3 = Guid.NewGuid();

            // Move first to last: 2, 3, 1
            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { tripDayId2, tripDayId3, tripDayId1 }
            };

            var trip = CreateTrip(userId, tripId);
            var tripDay1 = CreateTripDay(tripDayId1, tripId, "Day 1", 1);
            var tripDay2 = CreateTripDay(tripDayId2, tripId, "Day 2", 2);
            var tripDay3 = CreateTripDay(tripDayId3, tripId, "Day 3", 3);
            var tripDays = new List<TripDay> { tripDay1, tripDay2, tripDay3 };

            var expectedResponses = new List<TripDayResponse>
            {
                new TripDayResponse { Id = tripDayId2, TripId = tripId, Title = "Day 2", DateIndex = 1 },
                new TripDayResponse { Id = tripDayId3, TripId = tripId, Title = "Day 3", DateIndex = 2 },
                new TripDayResponse { Id = tripDayId1, TripId = tripId, Title = "Day 1", DateIndex = 3 }
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(tripDays);
            _mapperMock.Setup(x => x.Map<List<TripDayResponse>>(It.IsAny<List<TripDay>>()))
                .Returns(expectedResponses);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            tripDay2.DayIndex.Should().Be(1);
            tripDay3.DayIndex.Should().Be(2);
            tripDay1.DayIndex.Should().Be(3);
        }

        [Fact]
        public async Task Handle_MovingLastToFirst_UpdatesDayIndexesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId1 = Guid.NewGuid();
            var tripDayId2 = Guid.NewGuid();
            var tripDayId3 = Guid.NewGuid();

            // Move last to first: 3, 1, 2
            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { tripDayId3, tripDayId1, tripDayId2 }
            };

            var trip = CreateTrip(userId, tripId);
            var tripDay1 = CreateTripDay(tripDayId1, tripId, "Day 1", 1);
            var tripDay2 = CreateTripDay(tripDayId2, tripId, "Day 2", 2);
            var tripDay3 = CreateTripDay(tripDayId3, tripId, "Day 3", 3);
            var tripDays = new List<TripDay> { tripDay1, tripDay2, tripDay3 };

            var expectedResponses = new List<TripDayResponse>
            {
                new TripDayResponse { Id = tripDayId3, TripId = tripId, Title = "Day 3", DateIndex = 1 },
                new TripDayResponse { Id = tripDayId1, TripId = tripId, Title = "Day 1", DateIndex = 2 },
                new TripDayResponse { Id = tripDayId2, TripId = tripId, Title = "Day 2", DateIndex = 3 }
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(tripDays);
            _mapperMock.Setup(x => x.Map<List<TripDayResponse>>(It.IsAny<List<TripDay>>()))
                .Returns(expectedResponses);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            tripDay3.DayIndex.Should().Be(1);
            tripDay1.DayIndex.Should().Be(2);
            tripDay2.DayIndex.Should().Be(3);
        }

        [Fact]
        public async Task Handle_ReorderingMultipleTripDays_CallsUpdateForEachTripDay()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId1 = Guid.NewGuid();
            var tripDayId2 = Guid.NewGuid();
            var tripDayId3 = Guid.NewGuid();
            var tripDayId4 = Guid.NewGuid();
            var tripDayId5 = Guid.NewGuid();

            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { tripDayId2, tripDayId4, tripDayId1, tripDayId5, tripDayId3 }
            };

            var trip = CreateTrip(userId, tripId);
            var tripDays = new List<TripDay>
            {
                CreateTripDay(tripDayId1, tripId, "Day 1", 1),
                CreateTripDay(tripDayId2, tripId, "Day 2", 2),
                CreateTripDay(tripDayId3, tripId, "Day 3", 3),
                CreateTripDay(tripDayId4, tripId, "Day 4", 4),
                CreateTripDay(tripDayId5, tripId, "Day 5", 5)
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(tripDays);
            _mapperMock.Setup(x => x.Map<List<TripDayResponse>>(It.IsAny<List<TripDay>>()))
                .Returns(new List<TripDayResponse>());
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _tripDayRepositoryMock.Verify(x => x.Update(It.IsAny<TripDay>()), Times.Exactly(5));
        }

        [Fact]
        public async Task Handle_ValidReorder_ReturnsTripDaysInRequestedOrder()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId1 = Guid.NewGuid();
            var tripDayId2 = Guid.NewGuid();
            var tripDayId3 = Guid.NewGuid();

            var command = new ReorderTripDayCommand
            {
                TripId = tripId,
                OrderedTripDayIds = new List<Guid> { tripDayId3, tripDayId1, tripDayId2 }
            };

            var trip = CreateTrip(userId, tripId);
            var tripDay1 = CreateTripDay(tripDayId1, tripId, "Day 1", 1);
            var tripDay2 = CreateTripDay(tripDayId2, tripId, "Day 2", 2);
            var tripDay3 = CreateTripDay(tripDayId3, tripId, "Day 3", 3);
            var tripDays = new List<TripDay> { tripDay1, tripDay2, tripDay3 };

            var expectedResponses = new List<TripDayResponse>
            {
                new TripDayResponse { Id = tripDayId3 },
                new TripDayResponse { Id = tripDayId1 },
                new TripDayResponse { Id = tripDayId2 }
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            _tripDayRepositoryMock.Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(tripDays);
            _mapperMock.Setup(x => x.Map<List<TripDayResponse>>(It.IsAny<List<TripDay>>()))
                .Returns(expectedResponses);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value[0].Id.Should().Be(tripDayId3);
            result.Value[1].Id.Should().Be(tripDayId1);
            result.Value[2].Id.Should().Be(tripDayId2);
        }

        #endregion

        #region Helper Methods

        private void SetupCurrentUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
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

        private void SetupUnitOfWork()
        {
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        private Trip CreateTrip(Guid userId, Guid tripId)
        {
            var trip = Trip.Create(
                userId: userId,
                title: "Test Trip",
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(10),
                endDate: DateTime.UtcNow.AddDays(15),
                isPublic: false
            );

            // Use reflection to set the Id
            var idProperty = typeof(Trip).GetProperty("Id");
            idProperty?.SetValue(trip, tripId);

            return trip;
        }

        private TripDay CreateTripDay(Guid tripDayId, Guid tripId, string title, int dayIndex)
        {
            var tripDay = TripDay.Create(
                TripId: tripId,
                Tittle: title,
                DayDate: DateTime.UtcNow.AddDays(dayIndex),
                DayIndex: dayIndex
            );

            // Use reflection to set Id since it's typically set by the database
            var idProperty = typeof(TripDay).GetProperty("Id");
            idProperty?.SetValue(tripDay, tripDayId);
            return tripDay;
        }

        #endregion
    }
}

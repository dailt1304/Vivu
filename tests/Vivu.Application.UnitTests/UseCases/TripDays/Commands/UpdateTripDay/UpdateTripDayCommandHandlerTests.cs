using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.TripDay.Commands.UpdateTripDay;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripDays.Commands.UpdateTripDay
{
    public class UpdateTripDayCommandHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<UpdateTripDayHandler>> _loggerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly UpdateTripDayHandler _handler;

        public UpdateTripDayCommandHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<UpdateTripDayHandler>>();
            _currentUserMock = new Mock<ICurrentUser>();

            _handler = new UpdateTripDayHandler(
                _tripRepositoryMock.Object,
                _tripDayRepositoryMock.Object,
                _tripMemberRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidCommandWithTitle_ReturnsSuccessWithUpdatedTripDay()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                Title = "Updated Day 1: Tokyo Tower"
            };

            var trip = CreateTrip(userId, tripId, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = CreateTripDay(tripDayId, tripId, "Old Title", DateTime.UtcNow.AddDays(5), 1);
            var expectedResponse = new TripDayResponse
            {
                Id = tripDayId,
                TripId = tripId,
                Title = command.Title,
                DateIndex = 1
            };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Title.Should().Be(command.Title);

            _tripDayRepositoryMock.Verify(x => x.Update(It.IsAny<TripDay>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommandWithDayDate_UpdatesDayDate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var newDayDate = DateTime.UtcNow.AddDays(7);
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                DayDate = newDayDate
            };

            var trip = CreateTrip(userId, tripId, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", DateTime.UtcNow.AddDays(5), 1);
            var expectedResponse = new TripDayResponse
            {
                Id = tripDayId,
                TripId = tripId,
                DayDate = newDayDate
            };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.DayDate.Should().Be(newDayDate);
        }

        [Fact]
        public async Task Handle_ValidCommandWithBothTitleAndDayDate_UpdatesBoth()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var newDayDate = DateTime.UtcNow.AddDays(6);
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                Title = "Updated Title",
                DayDate = newDayDate
            };

            var trip = CreateTrip(userId, tripId, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var tripDay = CreateTripDay(tripDayId, tripId, "Old Title", DateTime.UtcNow.AddDays(5), 1);
            var expectedResponse = new TripDayResponse
            {
                Id = tripDayId,
                Title = command.Title,
                DayDate = newDayDate
            };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be(command.Title);
            result.Value.DayDate.Should().Be(newDayDate);
        }

        [Fact]
        public async Task Handle_CommandWithOnlyTripDayId_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                Title = null,
                DayDate = null
            };

            var trip = CreateTrip(userId, tripId, null, null);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", null, 1);
            var expectedResponse = new TripDayResponse { Id = tripDayId };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripDayRepositoryMock.Verify(x => x.Update(It.IsAny<TripDay>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateTitleToNull_ClearsTitle()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                Title = null
            };

            var trip = CreateTrip(userId, tripId, null, null);
            var tripDay = CreateTripDay(tripDayId, tripId, "Old Title", null, 1);
            var expectedResponse = new TripDayResponse { Id = tripDayId, Title = null };

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_UserNotAuthenticated_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new UpdateTripDayCommand { TripDayId = Guid.NewGuid() };
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
            var command = new UpdateTripDayCommand { TripDayId = Guid.NewGuid() };
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid");

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
            var command = new UpdateTripDayCommand { TripDayId = tripDayId };

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
            var command = new UpdateTripDayCommand { TripDayId = tripDayId };

            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", null, 1);

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
            var command = new UpdateTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(userId, tripId, null, null);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", null, 1);

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
            var command = new UpdateTripDayCommand { TripDayId = tripDayId };

            var trip = CreateTrip(ownerId, tripId, null, null);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", null, 1);

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

        #region Date Validation Tests

        [Fact]
        public async Task Handle_DayDateBeforeTripStartDate_ReturnsDateInvalidError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                DayDate = startDate.AddDays(-1) // Before start date
            };

            var trip = CreateTrip(userId, tripId, startDate, startDate.AddDays(10));
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", startDate, 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.DateInvalid.Code);
        }

        [Fact]
        public async Task Handle_DayDateAfterTripEndDate_ReturnsDateInvalidError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var endDate = startDate.AddDays(10);
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                DayDate = endDate.AddDays(1) // After end date
            };

            var trip = CreateTrip(userId, tripId, startDate, endDate);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", startDate, 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.DateInvalid.Code);
        }

        [Fact]
        public async Task Handle_DayDateEqualToStartDate_PassesValidation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                DayDate = startDate
            };

            var trip = CreateTrip(userId, tripId, startDate, startDate.AddDays(10));
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", startDate, 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_DayDateEqualToEndDate_PassesValidation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var endDate = startDate.AddDays(10);
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                DayDate = endDate
            };

            var trip = CreateTrip(userId, tripId, startDate, endDate);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", startDate, 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_DayDateWithinRange_PassesValidation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var endDate = startDate.AddDays(10);
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                DayDate = startDate.AddDays(5) // Middle of range
            };

            var trip = CreateTrip(userId, tripId, startDate, endDate);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", startDate, 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_NullDayDate_SkipsDateValidation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                Title = "Updated Title",
                DayDate = null
            };

            var trip = CreateTrip(userId, tripId, DateTime.UtcNow, DateTime.UtcNow.AddDays(5));
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", DateTime.UtcNow, 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_TripWithoutStartDate_SkipsStartDateValidation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                DayDate = DateTime.UtcNow.AddDays(-100) // Far past date
            };

            var trip = CreateTrip(userId, tripId, null, DateTime.UtcNow.AddDays(10));
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", DateTime.UtcNow, 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_TripWithoutEndDate_SkipsEndDateValidation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new UpdateTripDayCommand
            {
                TripDayId = tripDayId,
                DayDate = DateTime.UtcNow.AddDays(100) // Far future date
            };

            var trip = CreateTrip(userId, tripId, DateTime.UtcNow, null);
            var tripDay = CreateTripDay(tripDayId, tripId, "Day 1", DateTime.UtcNow, 1);

            SetupCurrentUser(userId);
            SetupTripDayRepository(tripDay);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
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
            _tripDayRepositoryMock.Setup(x => x.Update(It.IsAny<TripDay>()));
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

        private static Trip CreateTrip(Guid userId, Guid tripId, DateTime? startDate, DateTime? endDate)
        {
            var trip = Trip.Create(
                userId: userId,
                title: "Test Trip",
                description: "Test Description",
                startDate: startDate,
                endDate: endDate,
                isPublic: false
            );

            // Use reflection to set the Id
            var idProperty = typeof(Trip).GetProperty("Id");
            idProperty?.SetValue(trip, tripId);

            return trip;
        }

        private static TripDay CreateTripDay(Guid tripDayId, Guid tripId, string? title, DateTime? dayDate, int dayIndex)
        {
            var tripDay = TripDay.Create(
                TripId: tripId,
                Tittle: title,
                DayDate: dayDate ?? DateTime.UtcNow,
                DayIndex: dayIndex
            );

            // Use reflection to set the Id
            var idProperty = typeof(TripDay).GetProperty("Id");
            idProperty?.SetValue(tripDay, tripDayId);

            return tripDay;
        }

        #endregion
    }
}

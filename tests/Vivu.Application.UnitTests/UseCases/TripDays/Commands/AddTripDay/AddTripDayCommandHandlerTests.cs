using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.TripDay.Commands.AddTripDay;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripDays.Commands.AddTripDay
{
    public class AddTripDayCommandHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<AddTripDayHandler>> _loggerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly AddTripDayHandler _handler;

        public AddTripDayCommandHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<AddTripDayHandler>>();
            _currentUserMock = new Mock<ICurrentUser>();

            _handler = new AddTripDayHandler(
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
        public async Task Handle_ValidCommand_ReturnsSuccessWithTripDayResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new AddTripDayCommand
            {
                TripId = tripId,
                Title = "Day 1: Explore Tokyo"
            };

            var trip = CreateTrip(userId, tripId, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
            var expectedResponse = new TripDayResponse
            {
                Id = Guid.NewGuid(),
                TripId = tripId,
                Title = command.Title,
                DateIndex = 1
            };

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripDayRepository(0);
            SetupMapper(expectedResponse);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Title.Should().Be(command.Title);

            _tripDayRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripDay>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TripWithStartDate_CalculatesDayDateAutomatically()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var command = new AddTripDayCommand
            {
                TripId = tripId,
                Title = "Day 1"
            };

            var trip = CreateTrip(userId, tripId, startDate, startDate.AddDays(7));
            TripDay? capturedTripDay = null;

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripDayRepository(0);
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .Callback<TripDay>(td => capturedTripDay = td)
                .ReturnsAsync((TripDay td) => td);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTripDay.Should().NotBeNull();
            capturedTripDay!.DayDate.Should().NotBeNull();
            capturedTripDay.DayDate!.Value.Date.Should().Be(startDate.Date);
            capturedTripDay.DayIndex.Should().Be(1);
        }

        [Fact]
        public async Task Handle_TripWithoutStartDate_UsesDayDateFromCommand()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var dayDate = DateTime.UtcNow.AddDays(10);
            var command = new AddTripDayCommand
            {
                TripId = tripId,
                Title = "Day 1",
                DayDate = dayDate
            };

            var trip = CreateTrip(userId, tripId, null, null);
            TripDay? capturedTripDay = null;

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripDayRepository(0);
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .Callback<TripDay>(td => capturedTripDay = td)
                .ReturnsAsync((TripDay td) => td);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTripDay.Should().NotBeNull();
            capturedTripDay!.DayDate.Should().NotBeNull();
            capturedTripDay.DayDate!.Value.Date.Should().Be(dayDate.Date);
        }

        [Fact]
        public async Task Handle_ExistingTripDays_CalculatesCorrectDayIndex()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new AddTripDayCommand { TripId = tripId, Title = "Day 3" };

            var trip = CreateTrip(userId, tripId, DateTime.UtcNow, DateTime.UtcNow.AddDays(10));
            TripDay? capturedTripDay = null;

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripDayRepository(2); // Already have 2 days
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .Callback<TripDay>(td => capturedTripDay = td)
                .ReturnsAsync((TripDay td) => td);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTripDay.Should().NotBeNull();
            capturedTripDay!.DayIndex.Should().Be(3);
        }

        [Fact]
        public async Task Handle_MultipleDays_CalculatesDayDateSequentially()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.Date;
            var command = new AddTripDayCommand { TripId = tripId };

            var trip = CreateTrip(userId, tripId, startDate, startDate.AddDays(10));
            TripDay? capturedTripDay = null;

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripDayRepository(2); // Already have 2 days, so next is day 3
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .Callback<TripDay>(td => capturedTripDay = td)
                .ReturnsAsync((TripDay td) => td);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTripDay.Should().NotBeNull();
            capturedTripDay!.DayDate!.Value.Date.Should().Be(startDate.AddDays(2));
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_UserNotAuthenticated_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new AddTripDayCommand { TripId = Guid.NewGuid() };
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
            var command = new AddTripDayCommand { TripId = Guid.NewGuid() };
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        #endregion

        #region Trip Not Found Tests

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new AddTripDayCommand { TripId = tripId };

            SetupCurrentUser(userId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.NotFoundById(tripId).Code);
        }

        #endregion

        #region Owner Check Tests

        [Fact]
        public async Task Handle_OwnerNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new AddTripDayCommand { TripId = tripId };

            var trip = CreateTrip(userId, tripId, null, null);

            SetupCurrentUser(userId);
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
            var command = new AddTripDayCommand { TripId = tripId };

            var trip = CreateTrip(ownerId, tripId, null, null);

            SetupCurrentUser(userId);
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
        public async Task Handle_DayDateBeforeStartDate_ReturnsDateInvalidError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var command = new AddTripDayCommand
            {
                TripId = tripId
            };

            var trip = CreateTrip(userId, tripId, startDate, startDate.AddDays(10));

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            // maxDayIndex = -1, newDayIndex = 0
            // dayDate = startDate + (0-1) = startDate - 1 day (before StartDate)
            SetupTripDayRepository(-1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.DateInvalid.Code);
        }

        [Fact]
        public async Task Handle_DayDateAfterEndDate_ReturnsDateInvalidError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(10);
            var command = new AddTripDayCommand
            {
                TripId = tripId
            };

            var trip = CreateTrip(userId, tripId, startDate, endDate);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            // maxDayIndex = 11, newDayIndex = 12
            // dayDate = startDate + (12-1) = startDate + 11 days (after EndDate which is startDate + 10)
            SetupTripDayRepository(11);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.DateInvalid.Code);
        }

        [Fact]
        public async Task Handle_DayDateWithinRange_PassesValidation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var endDate = startDate.AddDays(10);
            var command = new AddTripDayCommand
            {
                TripId = tripId,
                DayDate = startDate.AddDays(5)
            };

            var trip = CreateTrip(userId, tripId, startDate, endDate);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            SetupTripDayRepository(0);
            SetupMapper(new TripDayResponse());
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_AutoCalculatedDateBeforeStartDate_ReturnsDateInvalidError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(5).Date;
            var command = new AddTripDayCommand { TripId = tripId };

            // Trip starts in future but we're adding a day that would be before start
            var trip = CreateTrip(userId, tripId, startDate, startDate.AddDays(10));

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId);
            // Max index = -1, so new index = 0, dayDate = startDate + (0-1) = startDate - 1 day
            SetupTripDayRepository(-1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.DateInvalid.Code);
        }

        #endregion

        #region Helper Methods

        private Trip CreateTrip(Guid userId, Guid tripId, DateTime? startDate, DateTime? endDate)
        {
            var trip = Trip.Create(
                userId: userId,
                title: "Test Trip",
                description: "Test Description",
                startDate: startDate,
                endDate: endDate
            );

            typeof(Trip).GetProperty("Id")!.SetValue(trip, tripId);
            return trip;
        }

        private void SetupCurrentUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
        }

        private void SetupTripRepository(Trip trip)
        {
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        }

        private void SetupTripMemberRepository(Guid tripId, Guid ownerId)
        {
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
        }

        private void SetupTripDayRepository(int maxDayIndex)
        {
            _tripDayRepositoryMock.Setup(x => x.GetMaxIndexByTripIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(maxDayIndex);

            _tripDayRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .ReturnsAsync((TripDay td) => td);
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

        #endregion
    }
}

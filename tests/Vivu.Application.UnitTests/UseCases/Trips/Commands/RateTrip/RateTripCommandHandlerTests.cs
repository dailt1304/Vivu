using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.RateTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.RateTrip
{
    public class RateTripCommandHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripRatingRepository> _tripRatingRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILogger<RateTripCommandHandler>> _loggerMock;
        private readonly RateTripCommandHandler _handler;

        public RateTripCommandHandlerTests()
        {
            _tripRepositoryMock      = new Mock<ITripRepository>();
            _tripRatingRepositoryMock = new Mock<ITripRatingRepository>();
            _unitOfWorkMock          = new Mock<IUnitOfWork>();
            _currentUserMock         = new Mock<ICurrentUser>();
            _loggerMock              = new Mock<ILogger<RateTripCommandHandler>>();

            _tripRatingRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<TripRating>()))
                .ReturnsAsync((TripRating rating) => rating);

            _handler = new RateTripCommandHandler(
                _tripRepositoryMock.Object,
                _tripRatingRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Trip CreateTrip(Guid userId, Guid? tripId = null, string status = "completed", TripRating? rating = null)
        {
            var trip = Trip.Create(
                userId: userId,
                title: "Test Trip",
                description: "A completed trip",
                startDate: DateTime.UtcNow.AddDays(-10),
                endDate: DateTime.UtcNow.AddDays(-1));

            if (tripId.HasValue)
                typeof(Trip).GetProperty("Id")!.SetValue(trip, tripId.Value);

            typeof(Trip).GetProperty("Status")!.SetValue(trip, status);
            typeof(Trip).GetProperty("TripRating")!.SetValue(trip, rating);

            return trip;
        }

        private void SetupUser(string? userId)
            => _currentUserMock.Setup(u => u.Id).Returns(userId!);

        private void SetupTrip(Trip? trip, Guid tripId)
            => _tripRepositoryMock
                .Setup(r => r.GetTripWithRatingAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);

        private static RateTripCommand MakeCommand(Guid? tripId = null) => new RateTripCommand
        {
            TripId = tripId ?? Guid.NewGuid(),
            Rating = 4,
            ReviewContent = "Great trip!"
        };

        #endregion

        #region Auth Guard

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public async Task Handle_InvalidUserId_ReturnsAuthError(string? userId)
        {
            SetupUser(userId);

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public async Task Handle_InvalidUserId_NeverCallsSaveChanges(string? userId)
        {
            SetupUser(userId);

            await _handler.Handle(MakeCommand(), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Trip Not Found

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundError()
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());
            SetupTrip(null, tripId);

            var result = await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.NotFoundById(tripId).Code);
        }

        [Fact]
        public async Task Handle_TripNotFound_NeverCallsSaveChanges()
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());
            SetupTrip(null, tripId);

            await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Not Trip Owner

        [Fact]
        public async Task Handle_UserIsNotOwner_ReturnsNotOwnerError()
        {
            var userId     = Guid.NewGuid();
            var tripId     = Guid.NewGuid();
            var otherOwner = Guid.NewGuid();
            SetupUser(userId.ToString());

            var trip = CreateTrip(otherOwner, tripId);
            SetupTrip(trip, tripId);

            var result = await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.NotOwner.Code);
        }

        [Fact]
        public async Task Handle_UserIsNotOwner_NeverCallsSaveChanges()
        {
            var userId     = Guid.NewGuid();
            var tripId     = Guid.NewGuid();
            var otherOwner = Guid.NewGuid();
            SetupUser(userId.ToString());

            var trip = CreateTrip(otherOwner, tripId);
            SetupTrip(trip, tripId);

            await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Trip Not Completed

        [Theory]
        [InlineData("planning")]
        [InlineData("ongoing")]
        public async Task Handle_TripNotCompleted_ReturnsNotCompletedError(string status)
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var trip = CreateTrip(userId, tripId, status);
            SetupTrip(trip, tripId);

            var result = await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.NotCompleted.Code);
        }

        [Fact]
        public async Task Handle_TripNotCompleted_NeverCallsSaveChanges()
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var trip = CreateTrip(userId, tripId, "planning");
            SetupTrip(trip, tripId);

            await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Already Rated

        [Fact]
        public async Task Handle_TripAlreadyRated_ReturnsAlreadyRatedError()
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var existingRating = TripRating.Create(tripId, userId, 3, "old review");
            var trip = CreateTrip(userId, tripId, "completed", existingRating);
            SetupTrip(trip, tripId);

            var result = await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.AlreadyRated.Code);
        }

        [Fact]
        public async Task Handle_TripAlreadyRated_NeverCallsSaveChanges()
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var existingRating = TripRating.Create(tripId, userId, 3, "old review");
            var trip = CreateTrip(userId, tripId, "completed", existingRating);
            SetupTrip(trip, tripId);

            await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccess()
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var trip = CreateTrip(userId, tripId);
            SetupTrip(trip, tripId);

            var result = await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ValidRequest_AddsRatingWithCorrectValues()
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var trip = CreateTrip(userId, tripId);
            SetupTrip(trip, tripId);

            var command = new RateTripCommand
            {
                TripId = tripId,
                Rating = 5,
                ReviewContent = "Amazing experience!"
            };

            await _handler.Handle(command, CancellationToken.None);

            _tripRatingRepositoryMock.Verify(
                r => r.AddAsync(It.Is<TripRating>(tr =>
                    tr.TripId == tripId &&
                    tr.UserId == userId &&
                    tr.Rating == 5 &&
                    tr.ReviewContent == "Amazing experience!")),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_CallsSaveChangesOnce()
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var trip = CreateTrip(userId, tripId);
            SetupTrip(trip, tripId);

            await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequestWithoutReview_ReturnsSuccess()
        {
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var trip = CreateTrip(userId, tripId);
            SetupTrip(trip, tripId);

            var command = new RateTripCommand
            {
                TripId = tripId,
                Rating = 3,
                ReviewContent = null
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        #endregion
    }
}

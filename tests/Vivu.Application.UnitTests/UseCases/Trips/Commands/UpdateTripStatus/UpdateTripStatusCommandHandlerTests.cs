using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.UseCases.Trips.Commands.UpdateTripStatus;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.UpdateTripStatus
{
    public class UpdateTripStatusCommandHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IUnitOfWork>     _unitOfWorkMock;
        private readonly Mock<ILogger<UpdateTripStatusCommandHandler>> _loggerMock;
        private readonly UpdateTripStatusCommandHandler _handler;

        public UpdateTripStatusCommandHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _unitOfWorkMock     = new Mock<IUnitOfWork>();
            _loggerMock         = new Mock<ILogger<UpdateTripStatusCommandHandler>>();

            _handler = new UpdateTripStatusCommandHandler(
                _tripRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Trip CreateTrip(
            string status,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var trip = Trip.Create(
                userId: Guid.NewGuid(),
                title: "Test Trip",
                startDate: startDate,
                endDate: endDate);
            trip.Status = status;
            return trip;
        }

        private void SetupTrips(List<Trip> trips)
            => _tripRepositoryMock
                .Setup(r => r.GetTripsForStatusUpdateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(trips);

        private static UpdateTripStatusCommand MakeCommand() => new UpdateTripStatusCommand();

        #endregion

        #region No Trips

        [Fact]
        public async Task Handle_NoTrips_ReturnsSuccessWithZero()
        {
            SetupTrips(new List<Trip>());

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
        }

        [Fact]
        public async Task Handle_NoTrips_DoesNotCallSaveChanges()
        {
            SetupTrips(new List<Trip>());

            await _handler.Handle(MakeCommand(), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region No Status Change Needed

        [Fact]
        public async Task Handle_TripNotYetStarted_StatusUnchanged()
        {
            var trip = CreateTrip("planning",
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(10));
            SetupTrips(new List<Trip> { trip });

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.Value.Should().Be(0);
            trip.Status.Should().Be("planning");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CompletedTrip_StatusUnchanged()
        {
            var trip = CreateTrip("completed",
                startDate: DateTime.UtcNow.AddDays(-10),
                endDate: DateTime.UtcNow.AddDays(-2));
            SetupTrips(new List<Trip> { trip });

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.Value.Should().Be(0);
            trip.Status.Should().Be("completed");
        }

        #endregion

        #region Planning → Ongoing

        [Fact]
        public async Task Handle_PlanningTripStartDatePassed_UpdatesToOngoing()
        {
            var trip = CreateTrip("planning",
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(5));
            SetupTrips(new List<Trip> { trip });

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(1);
            trip.Status.Should().Be("ongoing");
        }

        [Fact]
        public async Task Handle_PlanningTripStartDateExactlyNow_UpdatesToOngoing()
        {
            // Start date slightly in the past to ensure >= passes
            var trip = CreateTrip("planning",
                startDate: DateTime.UtcNow.AddSeconds(-1),
                endDate: DateTime.UtcNow.AddDays(5));
            SetupTrips(new List<Trip> { trip });

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            trip.Status.Should().Be("ongoing");
            result.Value.Should().Be(1);
        }

        #endregion

        #region Ongoing → Completed

        [Fact]
        public async Task Handle_OngoingTripEndDatePassed_UpdatesToCompleted()
        {
            var trip = CreateTrip("ongoing",
                startDate: DateTime.UtcNow.AddDays(-5),
                endDate: DateTime.UtcNow.AddDays(-1));
            SetupTrips(new List<Trip> { trip });

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(1);
            trip.Status.Should().Be("completed");
        }

        [Fact]
        public async Task Handle_OngoingTripEndDateNotYetPassed_StatusUnchanged()
        {
            var trip = CreateTrip("ongoing",
                startDate: DateTime.UtcNow.AddDays(-3),
                endDate: DateTime.UtcNow.AddDays(2));
            SetupTrips(new List<Trip> { trip });

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.Value.Should().Be(0);
            trip.Status.Should().Be("ongoing");
        }

        #endregion

        #region Multiple Trips

        [Fact]
        public async Task Handle_MultipleTripsMixedUpdates_ReturnsCorrectCount()
        {
            var planningToOngoing = CreateTrip("planning",
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(5));
            var ongoingToCompleted = CreateTrip("ongoing",
                startDate: DateTime.UtcNow.AddDays(-5),
                endDate: DateTime.UtcNow.AddDays(-1));
            var noChange = CreateTrip("planning",
                startDate: DateTime.UtcNow.AddDays(3),
                endDate: DateTime.UtcNow.AddDays(7));

            SetupTrips(new List<Trip> { planningToOngoing, ongoingToCompleted, noChange });

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.Value.Should().Be(2);
            planningToOngoing.Status.Should().Be("ongoing");
            ongoingToCompleted.Status.Should().Be("completed");
            noChange.Status.Should().Be("planning");
        }

        [Fact]
        public async Task Handle_AnyUpdates_CallsSaveChangesOnce()
        {
            var trip = CreateTrip("planning",
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(5));
            SetupTrips(new List<Trip> { trip });

            await _handler.Handle(MakeCommand(), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_MultipleUpdates_CallsSaveChangesOnlyOnce()
        {
            var trips = new List<Trip>
            {
                CreateTrip("planning", startDate: DateTime.UtcNow.AddDays(-1), endDate: DateTime.UtcNow.AddDays(5)),
                CreateTrip("ongoing",  startDate: DateTime.UtcNow.AddDays(-5), endDate: DateTime.UtcNow.AddDays(-1)),
            };
            SetupTrips(trips);

            await _handler.Handle(MakeCommand(), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}

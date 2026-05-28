using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.DeleteTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.DeleteTrip
{
    public class DeleteTripCommandHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IUnitOfWork>     _unitOfWorkMock;
        private readonly Mock<ICurrentUser>    _currentUserMock;
        private readonly Mock<ILogger<DeleteTripCommandHandler>> _loggerMock;
        private readonly DeleteTripCommandHandler _handler;

        public DeleteTripCommandHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _unitOfWorkMock     = new Mock<IUnitOfWork>();
            _currentUserMock    = new Mock<ICurrentUser>();
            _loggerMock         = new Mock<ILogger<DeleteTripCommandHandler>>();

            _handler = new DeleteTripCommandHandler(
                _tripRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Trip CreateTrip(Guid ownerId)
            => Trip.Create(userId: ownerId, title: "Test Trip");

        private void SetupUser(string? id = null)
            => _currentUserMock.Setup(u => u.Id).Returns(id ?? Guid.NewGuid().ToString());

        private void SetupTrip(Trip? trip, Guid? id = null)
        {
            var resolvedId = id ?? trip?.Id ?? Guid.NewGuid();
            _tripRepositoryMock
                .Setup(r => r.GetByIdAsync(resolvedId))
                .ReturnsAsync(trip);
        }

        private static DeleteTripCommand MakeCommand(Guid? tripId = null)
            => new DeleteTripCommand { TripId = tripId ?? Guid.NewGuid() };

        #endregion

        #region Auth Guard

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public async Task Handle_InvalidUserId_ReturnsAuthError(string? userId)
        {
            _currentUserMock.Setup(u => u.Id).Returns(userId!);

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
            _tripRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Trip Not Found

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundError()
        {
            var tripId = Guid.NewGuid();
            SetupUser();
            SetupTrip(null, tripId);

            var result = await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Contain("NotFound");
        }

        [Fact]
        public async Task Handle_TripNotFound_DoesNotCallSaveChanges()
        {
            var tripId = Guid.NewGuid();
            SetupUser();
            SetupTrip(null, tripId);

            await _handler.Handle(MakeCommand(tripId), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Access Denied

        [Fact]
        public async Task Handle_NonOwnerDeletes_ReturnsAccessDenied()
        {
            var ownerId = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            SetupUser(otherId.ToString());
            var trip = CreateTrip(ownerId);
            SetupTrip(trip);

            var result = await _handler.Handle(MakeCommand(trip.Id), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);
        }

        [Fact]
        public async Task Handle_NonOwnerDeletes_DoesNotCallSaveChanges()
        {
            var ownerId = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            SetupUser(otherId.ToString());
            var trip = CreateTrip(ownerId);
            SetupTrip(trip);

            await _handler.Handle(MakeCommand(trip.Id), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path

        [Fact]
        public async Task Handle_OwnerDeletes_ReturnsSuccess()
        {
            var ownerId = Guid.NewGuid();
            SetupUser(ownerId.ToString());
            var trip = CreateTrip(ownerId);
            SetupTrip(trip);

            var result = await _handler.Handle(MakeCommand(trip.Id), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_OwnerDeletes_SetsIsDeletedTrue()
        {
            var ownerId = Guid.NewGuid();
            SetupUser(ownerId.ToString());
            var trip = CreateTrip(ownerId);
            SetupTrip(trip);

            await _handler.Handle(MakeCommand(trip.Id), CancellationToken.None);

            trip.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_OwnerDeletes_CallsSaveChanges()
        {
            var ownerId = Guid.NewGuid();
            SetupUser(ownerId.ToString());
            var trip = CreateTrip(ownerId);
            SetupTrip(trip);

            await _handler.Handle(MakeCommand(trip.Id), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.TripMembers.Commands.LeaveTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;

namespace Vivu.Application.Tests.UseCases.TripMembers.Commands.LeaveTrip
{
    public class LeaveTripCommandHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPublisher> _publisherMock;
        private readonly Mock<ITripHubService> _tripHubServiceMock;
        private readonly Mock<ILogger<LeaveTripCommandHandler>> _loggerMock;
        private readonly LeaveTripCommandHandler _handler;

        public LeaveTripCommandHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _currentUserMock = new Mock<ICurrentUser>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _publisherMock = new Mock<IPublisher>();
            _tripHubServiceMock = new Mock<ITripHubService>();
            _loggerMock = new Mock<ILogger<LeaveTripCommandHandler>>();

            _handler = new LeaveTripCommandHandler(
                _tripRepositoryMock.Object,
                _tripMemberRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _userRepositoryMock.Object,
                _publisherMock.Object,
                _tripHubServiceMock.Object,
                _loggerMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, ownerId);
            var tripMember = TripMember.Create(tripId, userId, ownerId, "viewer");

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, tripMember);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ValidCommand_RemovesTripMember()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, ownerId);
            var tripMember = TripMember.Create(tripId, userId, ownerId, "viewer");

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, tripMember);
            SetupUnitOfWork();

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _tripMemberRepositoryMock.Verify(x => x.Remove(tripMember), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_LogsInformation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, ownerId);
            var tripMember = TripMember.Create(tripId, userId, ownerId, "viewer");

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, tripMember);
            SetupUnitOfWork();

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Leave trip attempt")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User left trip successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_EditorLeavingTrip_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, ownerId);
            var tripMember = TripMember.Create(tripId, userId, ownerId, "editor");

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, tripMember);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripMemberRepositoryMock.Verify(x => x.Remove(It.Is<TripMember>(tm => tm.Role == "editor")), Times.Once);
        }

        #endregion

        #region Failure Tests - Authentication

        [Fact]
        public async Task Handle_NullUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new LeaveTripCommand { TripId = Guid.NewGuid() };
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);

            _tripRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _tripMemberRepositoryMock.Verify(x => x.Remove(It.IsAny<TripMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmptyUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new LeaveTripCommand { TripId = Guid.NewGuid() };
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidGuidUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new LeaveTripCommand { TripId = Guid.NewGuid() };
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_LogsWarning()
        {
            // Arrange
            var command = new LeaveTripCommand { TripId = Guid.NewGuid() };
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid or missing user ID")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Tests - Trip Validation

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };

            SetupCurrentUser(userId);
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));

            _tripMemberRepositoryMock.Verify(x => x.Remove(It.IsAny<TripMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DeletedTrip_ReturnsTripDeletedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, Guid.NewGuid());
            trip.IsDeleted = true;

            SetupCurrentUser(userId);
            SetupTripRepository(trip);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.TripDeleted(tripId));

            _tripMemberRepositoryMock.Verify(x => x.Remove(It.IsAny<TripMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_TripNotFound_LogsWarning()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };

            SetupCurrentUser(userId);
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Trip not found")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Tests - Member Validation

        [Fact]
        public async Task Handle_OwnerCannotLeave_ReturnsOwnerCannotLeaveError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, userId); // User is the owner

            SetupCurrentUser(userId);
            SetupTripRepository(trip);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.TripMember.OwnerCannotLeave);

            _tripMemberRepositoryMock.Verify(x => x.GetByTripAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _tripMemberRepositoryMock.Verify(x => x.Remove(It.IsAny<TripMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UserNotMember_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, ownerId);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TripMember?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.TripMember.NotFound);

            _tripMemberRepositoryMock.Verify(x => x.Remove(It.IsAny<TripMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_OwnerCannotLeave_LogsWarning()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, userId);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Owner cannot leave trip")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotMember_LogsWarning()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, ownerId);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TripMember?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User is not a member")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_ViewerLeavingTrip_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, ownerId);
            var tripMember = TripMember.Create(tripId, userId, ownerId, "viewer");

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, tripMember);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripMemberRepositoryMock.Verify(x => x.Remove(It.Is<TripMember>(tm => tm.Role == "viewer")), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_DoesNotSaveChangesIfRemoveFails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new LeaveTripCommand { TripId = tripId };
            var trip = CreateTrip(tripId, ownerId);
            var tripMember = TripMember.Create(tripId, userId, ownerId, "viewer");

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, tripMember);

            _tripMemberRepositoryMock
                .Setup(x => x.Remove(It.IsAny<TripMember>()))
                .Throws(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Helper Methods

        private Trip CreateTrip(Guid tripId, Guid userId)
        {
            var trip = Trip.Create(
                userId: userId,
                title: "Test Trip",
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(7),
                endDate: DateTime.UtcNow.AddDays(14),
                isPublic: false,
                inviteCode: null
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
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
        }

        private void SetupTripMemberRepository(Guid tripId, Guid userId, TripMember? member)
        {
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
        }

        private void SetupUnitOfWork()
        {
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        #endregion
    }
}

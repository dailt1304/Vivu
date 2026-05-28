using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.UpdateTripVisibility;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.UpdateTripVisibility
{
    public class UpdateTripVisibilityCommandHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILogger<UpdateTripVisibilityCommandHandler>> _loggerMock;
        private readonly UpdateTripVisibilityCommandHandler _handler;

        public UpdateTripVisibilityCommandHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _currentUserMock = new Mock<ICurrentUser>();
            _loggerMock = new Mock<ILogger<UpdateTripVisibilityCommandHandler>>();

            _handler = new UpdateTripVisibilityCommandHandler(
                _tripRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private static UpdateTripVisibilityCommand CreateValidCommand(Guid tripId, bool isPublic)
        {
            return new UpdateTripVisibilityCommand
            {
                TripId = tripId,
                IsPublic = isPublic
            };
        }

        private static Trip CreateTrip(Guid userId, Guid? tripId = null, bool isComplete = true)
        {
            var trip = Trip.Create(
                userId: userId,
                title: isComplete ? "Complete Trip" : "Trip",
                description: isComplete ? "Complete Description" : null,
                startDate: isComplete ? DateTime.UtcNow.AddDays(2) : (DateTime?)null,
                endDate: isComplete ? DateTime.UtcNow.AddDays(6) : (DateTime?)null,
                isPublic: false
            );

            if (tripId.HasValue)
            {
                typeof(Trip).GetProperty("Id")!.SetValue(trip, tripId.Value);
            }

            return trip;
        }

        private static TripDto CreateTripDto(Trip trip)
        {
            return new TripDto
            {
                Id = trip.Id,
                UserId = trip.UserId,
                Title = trip.Title,
                Description = trip.Description,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Status = trip.Status,
                IsPublic = trip.IsPublic,
                CreatedAt = trip.CreatedDate,
                IsOwner = false,
                OwnerId = trip.UserId
            };
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_MakeTripPublic_ReturnsSuccessWithPublicTrip()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, isPublic: true);
            var trip = CreateTrip(userId, tripId, isComplete: true);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<object>(), It.IsAny<Action<IMappingOperationOptions<object, TripDto>>>()
))
                .Returns((object src, Action<IMappingOperationOptions<object, TripDto>> opts) => CreateTripDto((Trip)src));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.IsPublic.Should().BeTrue();

            _tripRepositoryMock.Verify(x => x.Update(It.IsAny<Trip>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_MakeTripPrivate_ReturnsSuccessWithPrivateTrip()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, isPublic: false);
            var trip = CreateTrip(userId, tripId, isComplete: true);
            trip.MakePublic(); // Start as public

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<object>(), It.IsAny<Action<IMappingOperationOptions<object, TripDto>>>()
))
                .Returns((object src, Action<IMappingOperationOptions<object, TripDto>> opts) => CreateTripDto((Trip)src));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.IsPublic.Should().BeFalse();

            _tripRepositoryMock.Verify(x => x.Update(It.IsAny<Trip>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand(Guid.NewGuid(), isPublic: true);
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);

            _tripRepositoryMock.Verify(
                x => x.GetByIdAsync(It.IsAny<Guid>()), 
                Times.Never);
        }

        [Fact]
        public async Task Handle_NullUserId_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand(Guid.NewGuid(), isPublic: true);
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_MalformedUserId_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand(Guid.NewGuid(), isPublic: true);
            _currentUserMock.Setup(x => x.Id).Returns("not-a-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Trip Not Found Tests

        [Fact]
        public async Task Handle_TripNotFound_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, isPublic: true);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Deleted Trip Tests

        [Fact]
        public async Task Handle_DeletedTrip_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, isPublic: true);
            var trip = CreateTrip(userId, tripId);
            trip.Delete(); // Mark as deleted

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AlreadyDeleted(tripId));

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Permission Tests

        [Fact]
        public async Task Handle_UserNotOwner_ReturnsFailure()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, isPublic: true);
            var trip = CreateTrip(ownerId, tripId);

            _currentUserMock.Setup(x => x.Id).Returns(otherUserId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Incomplete Trip Tests

        [Fact]
        public async Task Handle_IncompleteTripMadePublic_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, isPublic: true);
            var trip = CreateTrip(userId, tripId, isComplete: false);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Trip.Incomplete");

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_IncompleteTripMadePrivate_Succeeds()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, isPublic: false);
            var trip = CreateTrip(userId, tripId, isComplete: false);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<object>(), It.IsAny<Action<IMappingOperationOptions<object, TripDto>>>()
))
                .Returns((object src, Action<IMappingOperationOptions<object, TripDto>> opts) => CreateTripDto((Trip)src));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_AlreadyPublicTrip_MakePublicAgain_Succeeds()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, isPublic: true);
            var trip = CreateTrip(userId, tripId, isComplete: true);
            trip.MakePublic(); // Already public

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<object>(), It.IsAny<Action<IMappingOperationOptions<object, TripDto>>>()
))
                .Returns((object src, Action<IMappingOperationOptions<object, TripDto>> opts) => CreateTripDto((Trip)src));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.IsPublic.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_AlreadyPrivateTrip_MakePrivateAgain_Succeeds()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, isPublic: false);
            var trip = CreateTrip(userId, tripId, isComplete: true);
            // Already private by default

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<object>(), It.IsAny<Action<IMappingOperationOptions<object, TripDto>>>()
))
                .Returns((object src, Action<IMappingOperationOptions<object, TripDto>> opts) => CreateTripDto((Trip)src));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.IsPublic.Should().BeFalse();
        }

        #endregion
    }
}

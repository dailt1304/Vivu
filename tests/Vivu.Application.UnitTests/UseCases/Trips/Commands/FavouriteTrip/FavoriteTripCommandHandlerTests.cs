using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.FavouriteTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.FavouriteTrip
{
    public class FavoriteTripCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripFavoriteRepository> _favoriteRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<FavoriteTripCommandHandler>> _loggerMock;
        private readonly FavoriteTripCommandHandler _handler;

        public FavoriteTripCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _favoriteRepositoryMock = new Mock<ITripFavoriteRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<FavoriteTripCommandHandler>>();

            _handler = new FavoriteTripCommandHandler(
                _loggerMock.Object,
                _tripRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _favoriteRepositoryMock.Object,
                _currentUserMock.Object
            );
        }

        #region Helpers

        private static Trip CreatePublicTrip(Guid ownerId)
        {
            var trip = Trip.Create(
                userId: ownerId,
                title: "Public Test Trip",
                description: "A public trip",
                isPublic: true
            );
            return trip;
        }

        private static Trip CreatePrivateTrip(Guid ownerId)
        {
            var trip = Trip.Create(
                userId: ownerId,
                title: "Private Test Trip",
                description: "A private trip",
                isPublic: false
            );
            return trip;
        }

        private void SetupCurrentUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithValidPublicTrip_ShouldReturnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var trip = CreatePublicTrip(Guid.NewGuid());
            var command = new FavoriteTripCommand(trip.Id);

            SetupCurrentUser(userId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
            _favoriteRepositoryMock
                .Setup(x => x.GetTripFavouriteById(userId, trip.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TripFavorite?)null);
            _favoriteRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripFavorite>()))
                .ReturnsAsync((TripFavorite tf) => tf);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _favoriteRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripFavorite>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldCreateFavoriteWithCorrectUserAndTripIds()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var trip = CreatePublicTrip(Guid.NewGuid());
            var command = new FavoriteTripCommand(trip.Id);
            TripFavorite? capturedFavorite = null;

            SetupCurrentUser(userId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
            _favoriteRepositoryMock
                .Setup(x => x.GetTripFavouriteById(userId, trip.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TripFavorite?)null);
            _favoriteRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripFavorite>()))
                .Callback<TripFavorite>(tf => capturedFavorite = tf)
                .ReturnsAsync((TripFavorite tf) => tf);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedFavorite.Should().NotBeNull();
            capturedFavorite!.UserId.Should().Be(userId);
            capturedFavorite.TripId.Should().Be(trip.Id);
            capturedFavorite.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsNull_ShouldReturnInvalidTokenError()
        {
            // Arrange
            var command = new FavoriteTripCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
            _tripRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsEmpty_ShouldReturnInvalidTokenError()
        {
            // Arrange
            var command = new FavoriteTripCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsInvalidGuid_ShouldReturnInvalidTokenError()
        {
            // Arrange
            var command = new FavoriteTripCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns("not-a-valid-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Trip Not Found Tests

        [Fact]
        public async Task Handle_WhenTripNotFound_ShouldReturnTripNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new FavoriteTripCommand(tripId);

            SetupCurrentUser(userId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFound);
            _favoriteRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripFavorite>()), Times.Never);
        }

        #endregion

        #region Trip Visibility Tests

        [Fact]
        public async Task Handle_WhenTripIsPrivate_ShouldReturnNotPublicError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var trip = CreatePrivateTrip(Guid.NewGuid());
            var command = new FavoriteTripCommand(trip.Id);

            SetupCurrentUser(userId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotPublic);
            _favoriteRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripFavorite>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Already Favorited Tests

        [Fact]
        public async Task Handle_WhenAlreadyFavorited_ShouldReturnAlreadyFavoritedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var trip = CreatePublicTrip(Guid.NewGuid());
            var command = new FavoriteTripCommand(trip.Id);
            var existingFavorite = new TripFavorite
            {
                UserId = userId,
                TripId = trip.Id,
                CreatedAt = DateTime.UtcNow
            };

            SetupCurrentUser(userId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
            _favoriteRepositoryMock
                .Setup(x => x.GetTripFavouriteById(userId, trip.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingFavorite);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AlreadyFavorited);
            _favoriteRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripFavorite>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Repository Interaction Tests

        [Fact]
        public async Task Handle_ShouldNotSaveChanges_WhenValidationFails()
        {
            // Arrange
            var command = new FavoriteTripCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldPassCancellationTokenToRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var trip = CreatePublicTrip(Guid.NewGuid());
            var command = new FavoriteTripCommand(trip.Id);
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            SetupCurrentUser(userId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
            _favoriteRepositoryMock
                .Setup(x => x.GetTripFavouriteById(userId, trip.Id, token))
                .ReturnsAsync((TripFavorite?)null);
            _favoriteRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripFavorite>()))
                .ReturnsAsync((TripFavorite tf) => tf);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(token)).ReturnsAsync(1);

            // Act
            await _handler.Handle(command, token);

            // Assert
            _favoriteRepositoryMock.Verify(x => x.GetTripFavouriteById(userId, trip.Id, token), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ShouldLogWarning()
        {
            // Arrange
            var command = new FavoriteTripCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenTripNotFound_ShouldLogWarning()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new FavoriteTripCommand(tripId);

            SetupCurrentUser(userId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(tripId.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}

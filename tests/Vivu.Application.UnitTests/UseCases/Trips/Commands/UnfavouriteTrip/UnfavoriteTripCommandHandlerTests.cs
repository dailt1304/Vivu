using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.UnfavoriteTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.UnfavouriteTrip
{
    public class UnfavoriteTripCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripFavoriteRepository> _favoriteRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<UnfavoriteTripCommandHandler>> _loggerMock;
        private readonly UnfavoriteTripCommandHandler _handler;

        public UnfavoriteTripCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _favoriteRepositoryMock = new Mock<ITripFavoriteRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<UnfavoriteTripCommandHandler>>();

            _handler = new UnfavoriteTripCommandHandler(
                _loggerMock.Object,
                _tripRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _favoriteRepositoryMock.Object,
                _currentUserMock.Object
            );
        }

        #region Helpers

        private void SetupCurrentUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
        }

        private static TripFavorite CreateFavorite(Guid userId, Guid tripId)
        {
            return new TripFavorite
            {
                UserId = userId,
                TripId = tripId,
                CreatedAt = DateTime.UtcNow
            };
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WhenFavoriteExists_ShouldReturnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UnfavoriteTripCommand(tripId);
            var favorite = CreateFavorite(userId, tripId);

            SetupCurrentUser(userId);
            _favoriteRepositoryMock
                .Setup(x => x.GetTripFavouriteById(userId, tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(favorite);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _favoriteRepositoryMock.Verify(x => x.Remove(favorite), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldRemoveCorrectFavoriteEntity()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UnfavoriteTripCommand(tripId);
            var favorite = CreateFavorite(userId, tripId);
            TripFavorite? removedFavorite = null;

            SetupCurrentUser(userId);
            _favoriteRepositoryMock
                .Setup(x => x.GetTripFavouriteById(userId, tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(favorite);
            _favoriteRepositoryMock
                .Setup(x => x.Remove(It.IsAny<TripFavorite>()))
                .Callback<TripFavorite>(tf => removedFavorite = tf);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            removedFavorite.Should().NotBeNull();
            removedFavorite!.UserId.Should().Be(userId);
            removedFavorite.TripId.Should().Be(tripId);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsNull_ShouldReturnInvalidTokenError()
        {
            // Arrange
            var command = new UnfavoriteTripCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
            _favoriteRepositoryMock.Verify(x => x.Remove(It.IsAny<TripFavorite>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsEmpty_ShouldReturnInvalidTokenError()
        {
            // Arrange
            var command = new UnfavoriteTripCommand(Guid.NewGuid());
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
            var command = new UnfavoriteTripCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns("not-a-valid-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Not Favorited Tests

        [Fact]
        public async Task Handle_WhenFavoriteNotFound_ShouldReturnNotFavoritedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UnfavoriteTripCommand(tripId);

            SetupCurrentUser(userId);
            _favoriteRepositoryMock
                .Setup(x => x.GetTripFavouriteById(userId, tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TripFavorite?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFavorited);
            _favoriteRepositoryMock.Verify(x => x.Remove(It.IsAny<TripFavorite>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Repository Interaction Tests

        [Fact]
        public async Task Handle_ShouldNotSaveChanges_WhenValidationFails()
        {
            // Arrange
            var command = new UnfavoriteTripCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _favoriteRepositoryMock.Verify(x => x.Remove(It.IsAny<TripFavorite>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldPassCancellationTokenToRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UnfavoriteTripCommand(tripId);
            var favorite = CreateFavorite(userId, tripId);
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            SetupCurrentUser(userId);
            _favoriteRepositoryMock
                .Setup(x => x.GetTripFavouriteById(userId, tripId, token))
                .ReturnsAsync(favorite);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(token)).ReturnsAsync(1);

            // Act
            await _handler.Handle(command, token);

            // Assert
            _favoriteRepositoryMock.Verify(x => x.GetTripFavouriteById(userId, tripId, token), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldCallRepositoriesInCorrectOrder()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UnfavoriteTripCommand(tripId);
            var favorite = CreateFavorite(userId, tripId);
            var callOrder = new List<string>();

            SetupCurrentUser(userId);
            _favoriteRepositoryMock
                .Setup(x => x.GetTripFavouriteById(userId, tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(favorite)
                .Callback(() => callOrder.Add("GetFavorite"));
            _favoriteRepositoryMock
                .Setup(x => x.Remove(favorite))
                .Callback(() => callOrder.Add("Remove"));
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .Callback(() => callOrder.Add("SaveChanges"));

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            callOrder.Should().ContainInOrder("GetFavorite", "Remove", "SaveChanges");
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ShouldLogWarning()
        {
            // Arrange
            var command = new UnfavoriteTripCommand(Guid.NewGuid());
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

        #endregion
    }
}

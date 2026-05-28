using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Locations.Commands.DeleteLocation;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.DeleteLocation
{
    public class DeleteLocationCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<ITripLocationRepository> _tripLocationRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<DeleteLocationCommandHandler>> _loggerMock;
        private readonly DeleteLocationCommandHandler _handler;

        public DeleteLocationCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _tripLocationRepositoryMock = new Mock<ITripLocationRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<DeleteLocationCommandHandler>>();

            _handler = new DeleteLocationCommandHandler(
                _locationRepositoryMock.Object,
                _tripLocationRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private void SetupValidUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
        }

        private Location CreateTestLocation(Guid locationId, string name = "Test Location")
        {
            return new Location
            {
                Id = locationId,
                Name = name,
                Description = "Test Description",
                Address = "Test Address",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = true,
                IsDeleted = false
            };
        }

        private void SetupLocationRepository(Location? location, Guid locationId)
        {
            _locationRepositoryMock
                .Setup(x => x.GetByIdWithDetailsAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);
        }

        private void SetupHasActiveTrips(Guid locationId, bool hasActiveTrips)
        {
            _tripLocationRepositoryMock
                .Setup(x => x.HasActiveTripLocationsAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(hasActiveTrips);
        }

        private void SetupSuccessfulSave()
        {
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        #endregion

        #region Success Scenarios

        [Fact]
        public async Task Handle_ValidRequest_DeletesLocationSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            var command = new DeleteLocationCommand { LocationId = locationId };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupHasActiveTrips(locationId, false);
            SetupSuccessfulSave();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_CallsLocationDelete()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            var command = new DeleteLocationCommand { LocationId = locationId };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupHasActiveTrips(locationId, false);
            SetupSuccessfulSave();

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            location.IsDeleted.Should().BeTrue();
        }

        #endregion

        #region Authentication/Authorization Failures

        [Fact]
        public async Task Handle_NoUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            var command = new DeleteLocationCommand { LocationId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);

            _locationRepositoryMock.Verify(
                x => x.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid");

            var command = new DeleteLocationCommand { LocationId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);

            _locationRepositoryMock.Verify(
                x => x.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region Not Found Scenarios

        [Fact]
        public async Task Handle_NonExistentLocation_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            SetupValidUser(userId);
            SetupLocationRepository(null, locationId);

            var command = new DeleteLocationCommand { LocationId = locationId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.NotFoundById(locationId));

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region Already Deleted Scenarios

        [Fact]
        public async Task Handle_AlreadyDeletedLocation_ReturnsAlreadyDeletedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            location.IsDeleted = true;

            var command = new DeleteLocationCommand { LocationId = locationId };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.AlreadyDeleted);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region Used In Active Trips Scenarios

        [Fact]
        public async Task Handle_LocationUsedInActiveTrips_ReturnsUsedInActiveTripsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            var command = new DeleteLocationCommand { LocationId = locationId };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupHasActiveTrips(locationId, true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.UsedInActiveTrips);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_LocationNotUsedInActiveTrips_DeletesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            var command = new DeleteLocationCommand { LocationId = locationId };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupHasActiveTrips(locationId, false);
            SetupSuccessfulSave();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _tripLocationRepositoryMock.Verify(
                x => x.HasActiveTripLocationsAsync(locationId, It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}

using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Locations;
using Vivu.Application.UseCases.Locations.Commands.UpdateLocation;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.UpdateLocation
{
    public class UpdateLocationCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<UpdateLocationCommandHandler>> _loggerMock;
        private readonly UpdateLocationCommandHandler _handler;
        private readonly Mock<IHelperLocation> _helperLocation;

        public UpdateLocationCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _helperLocation = new Mock<IHelperLocation>();
            _loggerMock = new Mock<ILogger<UpdateLocationCommandHandler>>();

            _handler = new UpdateLocationCommandHandler(
                _locationRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _helperLocation.Object,
                _mapperMock.Object,
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
                LocationDetail = new LocationDetail
                {
                    LocationId = locationId,
                    OpeningHours = "9:00-22:00",
                    Phone = "0123456789",
                    Website = "https://test.com"
                }
            };
        }

        private void SetupLocationRepository(Location location)
        {
            _locationRepositoryMock
                .Setup(x => x.GetByIdWithDetailsAsync(location.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);
        }

        private void SetupSuccessfulUpdate(LocationDto expectedDto)
        {
            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(expectedDto);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        #endregion

        #region Success Scenarios

        [Fact]
        public async Task Handle_ValidRequest_UpdatesLocationSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            var command = new UpdateLocationCommand
            {
                LocationId = locationId,
                Name = "Updated Name",
                Description = "Updated Description",
                Address = "Updated Address",
                Latitude = 10.8,
                Longitude = 106.7
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                Name = "Updated Name",
                Description = "Updated Description",
                Address = "Updated Address",
                Latitude = 10.8,
                Longitude = 106.7
            };

            SetupValidUser(userId);
            SetupLocationRepository(location);
            SetupSuccessfulUpdate(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Name.Should().Be("Updated Name");
            result.Value.Description.Should().Be("Updated Description");

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_PartialUpdate_OnlyUpdatesProvidedFields()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            var command = new UpdateLocationCommand
            {
                LocationId = locationId,
                Name = "Only Name Updated"
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                Name = "Only Name Updated",
                Description = "Test Description", // Original value
                Address = "Test Address" // Original value
            };

            SetupValidUser(userId);
            SetupLocationRepository(location);
            SetupSuccessfulUpdate(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Name.Should().Be("Only Name Updated");

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateLocationDetails_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            var command = new UpdateLocationCommand
            {
                LocationId = locationId,
                OpeningHours = "8:00-23:00",
                Phone = "9876543210",
                Website = "https://updated.com"
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                Name = "Test Location"
            };

            SetupValidUser(userId);
            SetupLocationRepository(location);
            SetupSuccessfulUpdate(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            location.LocationDetail!.OpeningHours.Should().Be("8:00-23:00");
            location.LocationDetail.Phone.Should().Be("9876543210");
            location.LocationDetail.Website.Should().Be("https://updated.com");

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateCoordinates_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            var command = new UpdateLocationCommand
            {
                LocationId = locationId,
                Latitude = 21.028511,
                Longitude = 105.804817
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                Latitude = 21.028511,
                Longitude = 105.804817
            };

            SetupValidUser(userId);
            SetupLocationRepository(location);
            SetupSuccessfulUpdate(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            location.Latitude.Should().Be(21.028511);
            location.Longitude.Should().Be(105.804817);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateIsVerified_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            location.IsVerified = false;

            var command = new UpdateLocationCommand
            {
                LocationId = locationId,
                IsVerified = true
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                IsVerified = true
            };

            SetupValidUser(userId);
            SetupLocationRepository(location);
            SetupSuccessfulUpdate(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            location.IsVerified.Should().BeTrue();

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Authentication/Authorization Failures

        [Fact]
        public async Task Handle_NoUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = "Test"
            };

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

            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = "Test"
            };

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

            _locationRepositoryMock
                .Setup(x => x.GetByIdWithDetailsAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Location?)null);

            var command = new UpdateLocationCommand
            {
                LocationId = locationId,
                Name = "Test"
            };

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

        #region Edge Cases

        [Fact]
        public async Task Handle_LocationWithoutDetails_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            location.LocationDetail = null; // No details

            var command = new UpdateLocationCommand
            {
                LocationId = locationId,
                Name = "Updated Name",
                OpeningHours = "8:00-23:00" // Should not throw even without LocationDetail
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                Name = "Updated Name"
            };

            SetupValidUser(userId);
            SetupLocationRepository(location);
            SetupSuccessfulUpdate(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            location.Name.Should().Be("Updated Name");

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EmptyStrings_DoesNotUpdateThoseFields()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var originalName = location.Name;

            var command = new UpdateLocationCommand
            {
                LocationId = locationId,
                Name = string.Empty, // Empty string should not update
                Description = "Valid Description"
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                Name = originalName, // Should keep original
                Description = "Valid Description"
            };

            SetupValidUser(userId);
            SetupLocationRepository(location);
            SetupSuccessfulUpdate(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            location.Name.Should().Be(originalName); // Should not be updated to empty
            location.Description.Should().Be("Valid Description");

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}

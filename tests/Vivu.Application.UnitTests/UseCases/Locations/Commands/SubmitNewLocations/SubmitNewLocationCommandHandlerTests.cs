
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Files;
using Vivu.Application.UseCases.Locations.Commands.SubmitNewLocation;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.SubmitNewLocations
{
    public class SubmitNewLocationCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICityRepository> _cityRepositoryMock;
        private readonly Mock<ILocationReportRepository> _locationReportRepositoryMock;
        private readonly Mock<ILocationCategoryRepository> _locationCategoryRepositoryMock;
        private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<SubmitNewLocationCommandHandler>> _loggerMock;
        private readonly SubmitNewLocationCommandHandler _handler;

        public SubmitNewLocationCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cityRepositoryMock = new Mock<ICityRepository>();
            _locationReportRepositoryMock = new Mock<ILocationReportRepository>();
            _locationCategoryRepositoryMock = new Mock<ILocationCategoryRepository>();
            _cloudinaryServiceMock = new Mock<ICloudinaryService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<SubmitNewLocationCommandHandler>>();

            _handler = new SubmitNewLocationCommandHandler(
                _locationRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _cityRepositoryMock.Object,
                _locationReportRepositoryMock.Object,
                _locationCategoryRepositoryMock.Object,
                _cloudinaryServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private void SetupValidUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");
        }

        private void SetupSuccessfulImageUpload(string imageUrl = "https://cloudinary.com/test.jpg")
        {
            _cloudinaryServiceMock
                .Setup(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), "locations", null))
                .ReturnsAsync(imageUrl);
        }

        private void SetupSuccessfulSave()
        {
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(default))
                .ReturnsAsync(1);
        }

        private void SetupNoDuplicates(Guid userId)
        {
            _locationReportRepositoryMock
                .Setup(x => x.ExistsPendingNewLocationSubmissionAsync(userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<double?>(), It.IsAny<double?>(), default))
                .ReturnsAsync(false);

            _locationRepositoryMock
                .Setup(x => x.ExistsByNameAndAddressAsync(It.IsAny<string>(), It.IsAny<string>(), null, default))
                .ReturnsAsync(false);

            _locationRepositoryMock
                .Setup(x => x.ExistsByNameAndCoordinatesAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), null, default))
                .ReturnsAsync(false);
        }

        private static IFormFile CreateMockFormFile(long size = 1024, string fileName = "test.jpg")
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.Length).Returns(size);
            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.ContentType).Returns("image/jpeg");
            mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
            return mock.Object;
        }

        #endregion

        #region Success Scenarios

        [Fact]
        public async Task Handle_ValidRequest_SubmitsLocationSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "New Beach Resort",
                Description = "A beautiful beach resort",
                Address = "123 Beach Road",
                Latitude = 10.762622,
                Longitude = 106.660172,
                OpeningHours = "9:00-22:00",
                Phone = "0123456789",
                Website = "https://example.com",
                Tags = "beach,resort",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            var expectedDto = new LocationDto
            {
                Id = Guid.NewGuid(),
                Name = "New Beach Resort",
                Description = "A beautiful beach resort",
                IsVerified = false
            };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Name.Should().Be("New Beach Resort");
            result.Value.IsVerified.Should().BeFalse();

            _locationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Location>()), Times.Once);
            _locationReportRepositoryMock.Verify(x => x.AddAsync(It.IsAny<LocationReport>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WithCategoryId_ValidatesCategoryExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "Test Address",
                CategoryId = categoryId,
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            var category = new LocationCategory { Id = categoryId, Name = "Test Category" };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();
            SetupSuccessfulSave();

            _locationCategoryRepositoryMock
                .Setup(x => x.GetByIdAsync(categoryId))
                .ReturnsAsync(category);

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = Guid.NewGuid() });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _locationCategoryRepositoryMock.Verify(x => x.GetByIdAsync(categoryId), Times.Once);
        }

        [Fact]
        public async Task Handle_WithCityId_ValidatesCityExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cityId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "Test Address",
                CityId = cityId,
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            var city = new City { Id = cityId, Name = "Test City" };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();
            SetupSuccessfulSave();

            _cityRepositoryMock
                .Setup(x => x.GetByIdAsync(cityId))
                .ReturnsAsync(city);

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = Guid.NewGuid() });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _cityRepositoryMock.Verify(x => x.GetByIdAsync(cityId), Times.Once);
        }

        [Fact]
        public async Task Handle_WithCoordinatesOnly_SubmitsSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "GPS Location",
                Address = null,
                Latitude = 10.762622,
                Longitude = 106.660172,
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = Guid.NewGuid() });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _locationRepositoryMock.Verify(x => x.ExistsByNameAndCoordinatesAsync(
                It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), null, default), Times.Once);
        }

        [Fact]
        public async Task Handle_WithMultipleImages_UploadsAllImages()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "Test Address",
                Images = new List<IFormFile>
                {
                    CreateMockFormFile(1024, "image1.jpg"),
                    CreateMockFormFile(2048, "image2.jpg"),
                    CreateMockFormFile(3072, "image3.jpg")
                }
            };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = Guid.NewGuid() });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _cloudinaryServiceMock.Verify(
                x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), null),
                Times.Exactly(3));
        }

        [Fact]
        public async Task Handle_CreatesLocationReport_WithCorrectType()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "Test Address",
                Description = "Test Description",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            LocationReport? capturedReport = null;
            _locationReportRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<LocationReport>()))
                .Callback<LocationReport>(report => capturedReport = report);

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = Guid.NewGuid() });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedReport.Should().NotBeNull();
            capturedReport!.ReportType.Should().Be(ReportType.NEW_LOCATION.ToString());
            capturedReport.Status.Should().Be(ReportStatus.PENDING.ToString());
            capturedReport.UserId.Should().Be(userId);
        }

        #endregion

        #region Authentication/Authorization Failures

        [Fact]
        public async Task Handle_NoUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            var command = new SubmitNewLocationCommand
            {
                Name = "Test",
                Address = "Test",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_EmptyUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            var command = new SubmitNewLocationCommand
            {
                Name = "Test",
                Address = "Test",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

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
            _currentUserMock.Setup(x => x.Id).Returns("not-a-guid");

            var command = new SubmitNewLocationCommand
            {
                Name = "Test",
                Address = "Test",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Validation Failures

        [Fact]
        public async Task Handle_CategoryNotFound_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "Test Address",
                CategoryId = categoryId,
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);

            _locationCategoryRepositoryMock
                .Setup(x => x.GetByIdAsync(categoryId))
                .ReturnsAsync((LocationCategory?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.LocationCategory.NotFound);
        }

        [Fact]
        public async Task Handle_CityNotFound_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cityId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "Test Address",
                CityId = cityId,
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);

            _cityRepositoryMock
                .Setup(x => x.GetByIdAsync(cityId))
                .ReturnsAsync((City?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Cities.NotFound);
        }

        [Fact]
        public async Task Handle_DuplicatePendingSubmission_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "Test Address",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);

            _locationReportRepositoryMock
                .Setup(x => x.ExistsPendingNewLocationSubmissionAsync(
                    userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<double?>(), It.IsAny<double?>(), default))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Location.DuplicatePendingSubmission);
        }

        [Fact]
        public async Task Handle_DuplicateNameAndAddress_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "123 Main Street",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);

            _locationReportRepositoryMock
                .Setup(x => x.ExistsPendingNewLocationSubmissionAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<double?>(), It.IsAny<double?>(), default))
                .ReturnsAsync(false);

            _locationRepositoryMock
                .Setup(x => x.ExistsByNameAndAddressAsync("Test Location", "123 Main Street", null, default))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Location.DuplicatedNameAndAddress);
        }

        [Fact]
        public async Task Handle_DuplicateNameAndCoordinates_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = null,
                Latitude = 10.762622,
                Longitude = 106.660172,
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);

            _locationReportRepositoryMock
                .Setup(x => x.ExistsPendingNewLocationSubmissionAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<double?>(), It.IsAny<double?>(), default))
                .ReturnsAsync(false);

            _locationRepositoryMock
                .Setup(x => x.ExistsByNameAndCoordinatesAsync("Test Location", 10.762622, 106.660172, null, default))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Location.DuplicatedNameAndCoordinates);
        }

        #endregion

        #region Image Upload Failures

        [Fact]
        public async Task Handle_ImageUploadFails_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "Test Address",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);

            _cloudinaryServiceMock
                .Setup(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Upload failed"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Location.ImageUploadFailed);
        }

        #endregion

        #region Database Failures

        [Fact]
        public async Task Handle_DbUpdateException_ReturnsSaveFailed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "Test Address",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(default))
                .ThrowsAsync(new DbUpdateException("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Location.SaveFailed);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_WithBothAddressAndCoordinates_ChecksBothDuplicates()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "Test Location",
                Address = "123 Main Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = Guid.NewGuid() });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _locationRepositoryMock.Verify(
                x => x.ExistsByNameAndAddressAsync(It.IsAny<string>(), It.IsAny<string>(), null, default),
                Times.Once);
            _locationRepositoryMock.Verify(
                x => x.ExistsByNameAndCoordinatesAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), null, default),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithAllOptionalFields_SubmitsSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cityId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            var command = new SubmitNewLocationCommand
            {
                Name = "Complete Location",
                Description = "Full description",
                Address = "123 Street",
                Latitude = 10.762622,
                Longitude = 106.660172,
                CityId = cityId,
                CategoryId = categoryId,
                OpeningHours = "9:00-22:00",
                Phone = "0123456789",
                Website = "https://example.com",
                Tags = "tag1,tag2,tag3",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();
            SetupSuccessfulSave();

            _cityRepositoryMock
                .Setup(x => x.GetByIdAsync(cityId))
                .ReturnsAsync(new City { Id = cityId });

            _locationCategoryRepositoryMock
                .Setup(x => x.GetByIdAsync(categoryId))
                .ReturnsAsync(new LocationCategory { Id = categoryId });

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = Guid.NewGuid() });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_TrimsNameAndAddress_BeforeProcessing()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitNewLocationCommand
            {
                Name = "  Test Location  ",
                Address = "  123 Street  ",
                Images = new List<IFormFile> { CreateMockFormFile() }
            };

            SetupValidUser(userId);
            SetupNoDuplicates(userId);
            SetupSuccessfulImageUpload();
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = Guid.NewGuid() });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _locationRepositoryMock.Verify(
                x => x.ExistsByNameAndAddressAsync("Test Location", "123 Street", null, default),
                Times.Once);
        }

        #endregion
    }
}

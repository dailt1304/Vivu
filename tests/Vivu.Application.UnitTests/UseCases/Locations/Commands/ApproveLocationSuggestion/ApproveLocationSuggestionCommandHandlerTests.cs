using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Locations.Commands.ApproveLocationSuggestion;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.ApproveLocationSuggestion
{
    public class ApproveLocationSuggestionCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<ILocationReportRepository> _locationReportRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ApproveLocationSuggestionCommandHandler>> _loggerMock;
        private readonly ApproveLocationSuggestionCommandHandler _handler;

        public ApproveLocationSuggestionCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _locationReportRepositoryMock = new Mock<ILocationReportRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ApproveLocationSuggestionCommandHandler>>();

            _handler = new ApproveLocationSuggestionCommandHandler(
                _locationRepositoryMock.Object,
                _locationReportRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private void SetupValidUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");
        }

        private Location CreateTestLocation(Guid locationId, string name = "Test Location", bool isVerified = false)
        {
            return new Location
            {
                Id = locationId,
                Name = name,
                Description = "Test Description",
                Address = "Test Address",
                Latitude = 10.762622,
                Longitude = 106.660172,
                IsVerified = isVerified,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };
        }

        private LocationReport CreateTestLocationReport(
            Guid locationId,
            Guid userId,
            string status = "PENDING",
            string reportType = "NEW_LOCATION")
        {
            return new LocationReport
            {
                Id = Guid.NewGuid(),
                LocationId = locationId,
                UserId = userId,
                ReportType = reportType,
                ReportReason = "New location submission",
                Status = status,
                CreatedDate = DateTime.UtcNow
            };
        }

        private void SetupLocationRepository(Location? location, Guid locationId)
        {
            _locationRepositoryMock
                .Setup(x => x.GetByIdWithDetailsAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);
        }

        private void SetupPendingReport(LocationReport? report)
        {
            var reports = report != null
                ? new List<LocationReport> { report }
                : new List<LocationReport>();

            var asyncQueryable = reports.ToAsyncQueryable();

            _locationReportRepositoryMock
                .Setup(x => x.GetLocationReportsQuery(
                    ReportStatus.PENDING.ToString(),
                    ReportType.NEW_LOCATION.ToString()))
                .Returns(asyncQueryable);
        }

        private void SetupSuccessfulSave(int savedCount = 1)
        {
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(savedCount);
        }

        #endregion

        #region Success Scenarios

        [Fact]
        public async Task Handle_ValidRequest_ApprovesLocationSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId, isVerified: false);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Approved after verification"
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                Name = "Test Location",
                IsVerified = true
            };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.IsVerified.Should().BeTrue();

            location.IsVerified.Should().BeTrue();
            pendingReport.Status.Should().Be(ReportStatus.APPROVED.ToString());
            pendingReport.AdminNote.Should().Be("Approved after verification");

            _locationRepositoryMock.Verify(x => x.Update(location), Times.Once);
            _locationReportRepositoryMock.Verify(x => x.Update(pendingReport), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequestWithoutAdminNote_ApprovesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = null
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                Name = "Test Location",
                IsVerified = true
            };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            pendingReport.AdminNote.Should().BeNull();
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesReportStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Test note"
            };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = locationId });

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            pendingReport.Status.Should().Be(ReportStatus.APPROVED.ToString());
            pendingReport.AdminNote.Should().Be("Test note");
            pendingReport.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ValidRequest_SetsLocationAsVerified()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId, isVerified: false);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId
            };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = locationId });

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            location.IsVerified.Should().BeTrue();
            _locationRepositoryMock.Verify(x => x.Update(location), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReloadsLocationAfterSaving()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);
            var updatedLocation = CreateTestLocation(locationId, isVerified: true);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId
            };

            SetupValidUser(userId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave();

            _locationRepositoryMock
                .SetupSequence(x => x.GetByIdWithDetailsAsync(locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location)
                .ReturnsAsync(updatedLocation);

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = locationId, IsVerified = true });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _locationRepositoryMock.Verify(
                x => x.GetByIdWithDetailsAsync(locationId, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        #endregion

        #region Authentication/Authorization Failures

        [Fact]
        public async Task Handle_NoUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid()
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
        public async Task Handle_EmptyUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid()
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
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid()
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
            SetupLocationRepository(null, locationId);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.NotFoundById(locationId));

            _locationReportRepositoryMock.Verify(
                x => x.GetLocationReportsQuery(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region No Pending Report Scenarios

        [Fact]
        public async Task Handle_NoPendingReport_ReturnsNoPendingReportError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(null);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.NoPendingReport);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ReportNotForNewLocation_ReturnsNoPendingReportError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var differentLocationId = Guid.NewGuid();
            var pendingReport = CreateTestLocationReport(differentLocationId, userId);

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.NoPendingReport);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region Save Failure Scenarios

        [Fact]
        public async Task Handle_SaveChangesFails_ReturnsSaveFailedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Approved"
            };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave(0);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.SaveFailed);

            _locationRepositoryMock.Verify(x => x.Update(location), Times.Once);
            _locationReportRepositoryMock.Verify(x => x.Update(pendingReport), Times.Once);
        }

        [Fact]
        public async Task Handle_SaveChangesReturnsNegative_ReturnsSaveFailedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId
            };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave(-1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Location.SaveFailed);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_LocationAlreadyVerified_StillApproves()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId, isVerified: true);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId
            };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = locationId });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            location.IsVerified.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_LongAdminNote_ProcessesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var longNote = new string('A', 1000);
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = longNote
            };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = locationId });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            pendingReport.AdminNote.Should().Be(longNote);
        }

        #endregion
    }
}

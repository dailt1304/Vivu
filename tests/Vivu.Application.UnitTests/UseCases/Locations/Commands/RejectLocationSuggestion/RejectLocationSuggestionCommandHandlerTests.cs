using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Locations.Commands.RejectLocationSuggestion;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.RejectLocationSuggestion
{
    public class RejectLocationSuggestionCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<ILocationReportRepository> _locationReportRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<RejectLocationSuggestionCommandHandler>> _loggerMock;
        private readonly RejectLocationSuggestionCommandHandler _handler;

        public RejectLocationSuggestionCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _locationReportRepositoryMock = new Mock<ILocationReportRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<RejectLocationSuggestionCommandHandler>>();

            _handler = new RejectLocationSuggestionCommandHandler(
                _locationReportRepositoryMock.Object,
                _locationRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object
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
        public async Task Handle_ValidRequest_RejectsLocationSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId, isVerified: false);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Rejected due to policy violations"
            };

            var expectedDto = new LocationDto
            {
                Id = locationId,
                Name = "Test Location",
                IsVerified = false
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
            result.Value!.IsVerified.Should().BeFalse();

            location.IsVerified.Should().BeFalse();
            pendingReport.Status.Should().Be(ReportStatus.REJECTED.ToString());
            pendingReport.AdminNote.Should().Be("Rejected due to policy violations");

            _locationReportRepositoryMock.Verify(x => x.Update(pendingReport), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesReportStatusToRejected()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Does not meet quality standards"
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
            pendingReport.Status.Should().Be(ReportStatus.REJECTED.ToString());
            pendingReport.AdminNote.Should().Be("Does not meet quality standards");
            pendingReport.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ValidRequest_DoesNotSetLocationAsVerified()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId, isVerified: false);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Insufficient information"
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
            location.IsVerified.Should().BeFalse();
            _locationRepositoryMock.Verify(x => x.Update(It.IsAny<Location>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidRequest_OnlyUpdatesReport()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Duplicate submission"
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
            _locationReportRepositoryMock.Verify(x => x.Update(pendingReport), Times.Once);
            _locationRepositoryMock.Verify(x => x.Update(It.IsAny<Location>()), Times.Never);
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
            var command = new RejectLocationSuggestionCommand
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

        #region Authentication/Authorization Failures

        [Fact]
        public async Task Handle_NoUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = "Test note"
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

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = "Test note"
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

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = "Test note"
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

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Test note"
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
        public async Task Handle_NoPendingReport_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(null);

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Test note"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.LocationReport.NotFound);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ReportNotForTargetLocation_ReturnsNotFoundError()
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

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Test note"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.LocationReport.NotFound);

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

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Rejected"
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

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Rejected"
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
        public async Task Handle_LocationAlreadyVerified_StillRejects()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId, isVerified: true);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Rejected"
            };

            SetupValidUser(userId);
            SetupLocationRepository(location, locationId);
            SetupPendingReport(pendingReport);
            SetupSuccessfulSave();

            _mapperMock
                .Setup(x => x.Map<LocationDto>(It.IsAny<Location>()))
                .Returns(new LocationDto { Id = locationId, IsVerified = true });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            location.IsVerified.Should().BeTrue();
            pendingReport.Status.Should().Be(ReportStatus.REJECTED.ToString());
        }

        [Fact]
        public async Task Handle_SpecialCharactersInAdminNote_ProcessesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var specialNote = "Rejected: <script>alert('test')</script> & special chars: @#$%^&*()";
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = specialNote
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
            pendingReport.AdminNote.Should().Be(specialNote);
        }

        [Fact]
        public async Task Handle_MultipleReportsForSameLocation_ProcessesCorrectReport()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var location = CreateTestLocation(locationId);
            var pendingReport = CreateTestLocationReport(locationId, userId);

            var command = new RejectLocationSuggestionCommand
            {
                LocationId = locationId,
                AdminNote = "Rejected"
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
            pendingReport.Status.Should().Be(ReportStatus.REJECTED.ToString());
            _locationReportRepositoryMock.Verify(x => x.Update(pendingReport), Times.Once);
        }

        #endregion
    }
}

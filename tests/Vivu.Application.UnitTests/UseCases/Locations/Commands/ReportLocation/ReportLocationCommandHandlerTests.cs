using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Files;
using Vivu.Application.UseCases.Locations.Commands.ReportLocation;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.ReportLocation
{
    public class ReportLocationCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<ILocationReportRepository> _locationReportRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICloudinaryService> _cloudinaryService;
        private readonly Mock<ILogger<ReportLocationCommandHandler>> _loggerMock;
        private readonly ReportLocationCommandHandler _handler;

        public ReportLocationCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _locationReportRepositoryMock = new Mock<ILocationReportRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _cloudinaryService = new Mock<ICloudinaryService>();
            _loggerMock = new Mock<ILogger<ReportLocationCommandHandler>>();

            _handler = new ReportLocationCommandHandler(
                _locationRepositoryMock.Object,
                _locationReportRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _cloudinaryService.Object,
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

        private void SetupValidLocation(Guid locationId, string name = "Test Location")
        {
            var location = new Location { Id = locationId, Name = name, IsVerified = true };
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId)).ReturnsAsync(location);
        }

        private void SetupSuccessfulReport(Guid userId, Guid locationId, ReportLocationResponse response)
        {
            _locationReportRepositoryMock
                .Setup(x => x.GetUserReportCountTodayAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _locationReportRepositoryMock
                .Setup(x => x.HasUserPendingReportForLocationAsync(userId, locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mapperMock
                .Setup(x => x.Map<ReportLocationResponse>(It.IsAny<LocationReport>()))
                .Returns(response);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        #endregion

        #region Success Scenarios

        [Fact]
        public async Task Handle_ValidRequest_CreatesReportSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new ReportLocationCommand
            {
                LocationId = locationId,
                ReportType = ReportType.WRONG_INFO,
                Reason = "The address is incorrect",
                Description = "Should be 123 Main Street"
            };

            var expectedResponse = new ReportLocationResponse
            {
                Id = Guid.NewGuid(),
                LocationId = locationId,
                ReportType = "WRONG_INFO",
                Reason = command.Reason,
                Description = command.Description,
                Status = "PENDING"
            };

            SetupValidUser(userId);
            SetupValidLocation(locationId);
            SetupSuccessfulReport(userId, locationId, expectedResponse);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.LocationId.Should().Be(locationId);
            result.Value.ReportType.Should().Be("WRONG_INFO");
            result.Value.Status.Should().Be("PENDING");

            _locationReportRepositoryMock.Verify(x => x.AddAsync(It.IsAny<LocationReport>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(ReportType.WRONG_INFO)]
        [InlineData(ReportType.CLOSED)]
        public async Task Handle_DifferentReportTypes_CreatesReportSuccessfully(ReportType reportType)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new ReportLocationCommand
            {
                LocationId = locationId,
                ReportType = reportType,
                Reason = "Test reason"
            };

            var expectedResponse = new ReportLocationResponse
            {
                Id = Guid.NewGuid(),
                LocationId = locationId,
                ReportType = reportType.ToString()
            };

            SetupValidUser(userId);
            SetupValidLocation(locationId);
            SetupSuccessfulReport(userId, locationId, expectedResponse);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.ReportType.Should().Be(reportType.ToString());
        }

        #endregion

        #region Authentication Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid-guid")]
        public async Task Handle_InvalidCurrentUser_ReturnsInvalidTokenError(string? userId)
        {
            // Arrange
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId!);
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
            _locationRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Location Validation

        [Fact]
        public async Task Handle_LocationNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new ReportLocationCommand
            {
                LocationId = locationId,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            SetupValidUser(userId);
            _locationRepositoryMock.Setup(x => x.GetByIdAsync(locationId)).ReturnsAsync((Location)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Location.NotFound");
        }

        #endregion

        #region Rate Limiting

        [Fact]
        public async Task Handle_RateLimitExceeded_ReturnsRateLimitError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new ReportLocationCommand
            {
                LocationId = locationId,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            SetupValidUser(userId);
            SetupValidLocation(locationId);

            _locationReportRepositoryMock
                .Setup(x => x.GetUserReportCountTodayAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3); // Already 3 reports today

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.LocationReport.RateLimitExceeded);
            _locationReportRepositoryMock.Verify(x => x.AddAsync(It.IsAny<LocationReport>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AtRateLimit_AllowsReport()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new ReportLocationCommand
            {
                LocationId = locationId,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            var expectedResponse = new ReportLocationResponse { Id = Guid.NewGuid(), LocationId = locationId };

            SetupValidUser(userId);
            SetupValidLocation(locationId);

            _locationReportRepositoryMock
                .Setup(x => x.GetUserReportCountTodayAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(2); // 2 reports today, 3rd is allowed

            _locationReportRepositoryMock
                .Setup(x => x.HasUserPendingReportForLocationAsync(userId, locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mapperMock.Setup(x => x.Map<ReportLocationResponse>(It.IsAny<LocationReport>()))
                .Returns(expectedResponse);

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _locationReportRepositoryMock.Verify(x => x.AddAsync(It.IsAny<LocationReport>()), Times.Once);
        }

        #endregion

        #region Duplicate Report

        [Fact]
        public async Task Handle_AlreadyReportedLocation_ReturnsConflictError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new ReportLocationCommand
            {
                LocationId = locationId,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            SetupValidUser(userId);
            SetupValidLocation(locationId);

            _locationReportRepositoryMock
                .Setup(x => x.GetUserReportCountTodayAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _locationReportRepositoryMock
                .Setup(x => x.HasUserPendingReportForLocationAsync(userId, locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // Already reported this location

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Location.AlreadyReported);
            _locationReportRepositoryMock.Verify(x => x.AddAsync(It.IsAny<LocationReport>()), Times.Never);
        }

        #endregion

        #region Error Cases

        [Fact]
        public async Task Handle_SaveChangesFails_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new ReportLocationCommand
            {
                LocationId = locationId,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            SetupValidUser(userId);
            SetupValidLocation(locationId);

            _locationReportRepositoryMock
                .Setup(x => x.GetUserReportCountTodayAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _locationReportRepositoryMock
                .Setup(x => x.HasUserPendingReportForLocationAsync(userId, locationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0); // Save failed

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            _locationReportRepositoryMock.Verify(x => x.AddAsync(It.IsAny<LocationReport>()), Times.Once);
        }

        #endregion
    }
}

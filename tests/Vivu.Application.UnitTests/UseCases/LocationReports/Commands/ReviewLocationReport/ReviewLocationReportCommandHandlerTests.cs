using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.LocationReports.Commands.ReviewLocationReport;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.LocationReports.Commands.ReviewLocationReport
{
    public class ReviewLocationReportCommandHandlerTests
    {
        private readonly Mock<ILocationReportRepository> _locationReportRepositoryMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ReviewLocationReportCommandHandler>> _loggerMock;
        private readonly ReviewLocationReportCommandHandler _handler;

        public ReviewLocationReportCommandHandlerTests()
        {
            _locationReportRepositoryMock = new Mock<ILocationReportRepository>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _currentUserMock = new Mock<ICurrentUser>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ReviewLocationReportCommandHandler>>();

            _handler = new ReviewLocationReportCommandHandler(
                _locationReportRepositoryMock.Object,
                _locationRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private static LocationReport CreateLocationReport(
            Guid? reportId = null,
            Guid? locationId = null,
            string status = "PENDING",
            string reportType = "WRONG_INFO")
        {
            var location = new Location
            {
                Id = locationId ?? Guid.NewGuid(),
                Name = "Test Location",
                IsVerified = true
            };

            var report = LocationReport.Create(
                locationId: location.Id,
                userId: Guid.NewGuid(),
                reportType: reportType,
                reportReason: "Test reason",
                reportDescription: "Test description"
            );

            if (reportId.HasValue)
            {
                typeof(LocationReport).GetProperty("Id")!.SetValue(report, reportId.Value);
            }

            typeof(LocationReport).GetProperty("Status")!.SetValue(report, status);
            typeof(LocationReport).GetProperty("Location")!.SetValue(report, location);
            typeof(LocationReport).GetProperty("ReportType")!.SetValue(report, reportType);

            return report;
        }

        private void SetupSuccessfulReview(LocationReport report)
        {
            _locationReportRepositoryMock
                .Setup(x => x.GetReportByIdWithDetailsAsync(report.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        #endregion

        #region Success Scenarios

        [Theory]
        [InlineData(ReportStatus.APPROVED)]
        [InlineData(ReportStatus.REJECTED)]
        public async Task Handle_ValidStatuses_UpdatesCorrectly(ReportStatus status)
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var command = new ReviewLocationReportCommand
            {
                ReportId = reportId,
                Status = status,
                AdminNote = "Processed"
            };

            var report = CreateLocationReport(reportId);
            SetupSuccessfulReview(report);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Status.Should().Be(status.ToString());

            _locationReportRepositoryMock.Verify(x => x.Update(It.Is<LocationReport>(r =>
                r.Status == status.ToString())), Times.Once);
        }

        [Fact]
        public async Task Handle_WithAdminNote_SetsAdminNote()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var command = new ReviewLocationReportCommand
            {
                ReportId = reportId,
                Status = ReportStatus.APPROVED,
                AdminNote = "Report approved"
            };

            var report = CreateLocationReport(reportId);
            SetupSuccessfulReview(report);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.AdminNote.Should().Be("Report approved");
        }

        [Fact]
        public async Task Handle_WithoutAdminNote_ReturnsSuccess()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var command = new ReviewLocationReportCommand
            {
                ReportId = reportId,
                Status = ReportStatus.APPROVED,
                AdminNote = null
            };

            var report = CreateLocationReport(reportId);
            SetupSuccessfulReview(report);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.AdminNote.Should().BeNull();
        }

        #endregion

        #region Error Cases

        [Fact]
        public async Task Handle_ReportNotFound_ReturnsFailure()
        {
            // Arrange
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = ReportStatus.APPROVED
            };

            _locationReportRepositoryMock
                .Setup(x => x.GetReportByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocationReport?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("LocationReport.NotFound");
        }

        [Fact]
        public async Task Handle_AlreadyProcessedReport_ReturnsFailure()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var command = new ReviewLocationReportCommand
            {
                ReportId = reportId,
                Status = ReportStatus.APPROVED
            };

            var report = CreateLocationReport(reportId, status: "APPROVED");
            _locationReportRepositoryMock
                .Setup(x => x.GetReportByIdWithDetailsAsync(reportId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("LocationReport.AlreadyProcessed");
        }

        [Fact]
        public async Task Handle_SaveChangesFails_ReturnsFailure()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var command = new ReviewLocationReportCommand
            {
                ReportId = reportId,
                Status = ReportStatus.APPROVED
            };

            var report = CreateLocationReport(reportId);
            _locationReportRepositoryMock
                .Setup(x => x.GetReportByIdWithDetailsAsync(reportId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("LocationReport.UpdateFailed");
        }

        [Fact]
        public async Task Handle_ExceptionThrown_ReturnsFailure()
        {
            // Arrange
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = ReportStatus.APPROVED
            };

            _locationReportRepositoryMock
                .Setup(x => x.GetReportByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("LocationReport.ReviewError");
        }

        #endregion

        #region Business Logic

        [Fact]
        public async Task Handle_ApproveWithWrongInfo_UpdatesLocationAndMarksAsVerified()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var command = new ReviewLocationReportCommand
            {
                ReportId = reportId,
                Status = ReportStatus.APPROVED,
                Name = "Updated Name",
                Description = "Updated Description",
                Images = "new-image.jpg"
            };

            var report = CreateLocationReport(reportId, locationId, reportType: "WRONG_INFO");

            _locationReportRepositoryMock
                .Setup(x => x.GetReportByIdWithDetailsAsync(reportId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            report.Location!.IsVerified.Should().BeTrue();
            _locationRepositoryMock.Verify(x => x.Update(It.IsAny<Location>()), Times.Once);
        }



        #endregion
    }
}

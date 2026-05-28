using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.LocationReports.Queries.GetLocationReports;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.LocationReports.Queries.GetLocationReports
{
    public class GetLocationReportsQueryHandlerTests
    {
        private readonly Mock<ILocationReportRepository> _locationReportRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILogger<GetLocationReportsQueryHandler>> _loggerMock;
        private readonly GetLocationReportsQueryHandler _handler;

        public GetLocationReportsQueryHandlerTests()
        {
            _locationReportRepositoryMock = new Mock<ILocationReportRepository>();
            _mapperMock = new Mock<IMapper>();
            _currentUserMock = new Mock<ICurrentUser>();
            _loggerMock = new Mock<ILogger<GetLocationReportsQueryHandler>>();

            _handler = new GetLocationReportsQueryHandler(
                _locationReportRepositoryMock.Object,
                _mapperMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private static GetLocationReportsQuery CreateValidQuery(
            string? status = null,
            string? reportType = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            return new GetLocationReportsQuery
            {
                Status = status,
                ReportType = reportType,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private static LocationReport CreateLocationReport(
            Guid? id = null,
            ReportStatus status = ReportStatus.PENDING,
            ReportType reportType = ReportType.WRONG_INFO)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                Name = "Test Location",
                Description = "Test Description",
                Address = "Test Address",
                Latitude = 10.0,
                Longitude = 105.0,
                CategoryId = Guid.NewGuid()
            };

            var report = LocationReport.Create(
                locationId: location.Id,
                userId: Guid.NewGuid(),
                reportType: reportType.ToString(),
                reportReason: "Test reason",
                reportDescription: "Test report"
            );

            if (id.HasValue)
            {
                typeof(LocationReport).GetProperty("Id")!.SetValue(report, id.Value);
            }

            typeof(LocationReport).GetProperty("Status")!.SetValue(report, status.ToString());
            typeof(LocationReport).GetProperty("Location")!.SetValue(report, location);

            return report;
        }

        private static LocationReportDto CreateLocationReportDto(LocationReport report)
        {
            return new LocationReportDto
            {
                Id = report.Id,
                LocationId = report.LocationId,
                LocationName = report.Location?.Name ?? "Test Location",
                ReporterId = report.UserId,
                ReportType = report.ReportType,
                Status = report.Status,
                Reason = report.ReportReason,
                Description = report.ReportDescription,
                CreatedDate = report.CreatedDate
            };
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithValidRequest_ShouldReturnPaginatedReports()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery();

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var reports = new List<LocationReport>
            {
                CreateLocationReport(),
                CreateLocationReport(),
                CreateLocationReport()
            };

            var mockQueryable = reports.AsQueryable().BuildMock();
            _locationReportRepositoryMock
                .Setup(x => x.GetLocationReportsQuery(null, null))
                .Returns(mockQueryable);

            var reportDtos = reports.Select(CreateLocationReportDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationReportDto>>(It.IsAny<List<LocationReport>>()))
                .Returns(reportDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(3);
            result.Value.TotalCount.Should().Be(3);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ShouldReturnFilteredReports()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(status: "PENDING");

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var reports = new List<LocationReport>
            {
                CreateLocationReport(status: ReportStatus.PENDING),
                CreateLocationReport(status: ReportStatus.PENDING)
            };

            var mockQueryable = reports.AsQueryable().BuildMock();
            _locationReportRepositoryMock
                .Setup(x => x.GetLocationReportsQuery("PENDING", null))
                .Returns(mockQueryable);

            var reportDtos = reports.Select(CreateLocationReportDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationReportDto>>(It.IsAny<List<LocationReport>>()))
                .Returns(reportDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(2);
            result.Value.Items.Should().AllSatisfy(r => r.Status.Should().Be("PENDING"));
        }

        [Fact]
        public async Task Handle_WithReportTypeFilter_ShouldReturnFilteredReports()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(reportType: "WRONG_INFO");

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var reports = new List<LocationReport>
            {
                CreateLocationReport(reportType: ReportType.WRONG_INFO),
                CreateLocationReport(reportType: ReportType.WRONG_INFO)
            };

            var mockQueryable = reports.AsQueryable().BuildMock();
            _locationReportRepositoryMock
                .Setup(x => x.GetLocationReportsQuery(null, "WRONG_INFO"))
                .Returns(mockQueryable);

            var reportDtos = reports.Select(CreateLocationReportDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationReportDto>>(It.IsAny<List<LocationReport>>()))
                .Returns(reportDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(2);
            result.Value.Items.Should().AllSatisfy(r => r.ReportType.Should().Be("WRONG_INFO"));
        }

        [Fact]
        public async Task Handle_WithBothFilters_ShouldReturnFilteredReports()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(status: "PENDING", reportType: "WRONG_INFO");

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var reports = new List<LocationReport>
            {
                CreateLocationReport(status: ReportStatus.PENDING, reportType: ReportType.WRONG_INFO)
            };

            var mockQueryable = reports.AsQueryable().BuildMock();
            _locationReportRepositoryMock
                .Setup(x => x.GetLocationReportsQuery("PENDING", "WRONG_INFO"))
                .Returns(mockQueryable);

            var reportDtos = reports.Select(CreateLocationReportDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationReportDto>>(It.IsAny<List<LocationReport>>()))
                .Returns(reportDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(1);
            result.Value.Items[0].Status.Should().Be("PENDING");
            result.Value.Items[0].ReportType.Should().Be("WRONG_INFO");
        }

        [Fact]
        public async Task Handle_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(pageNumber: 2, pageSize: 2);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var reports = new List<LocationReport>
            {
                CreateLocationReport(),
                CreateLocationReport(),
                CreateLocationReport(),
                CreateLocationReport(),
                CreateLocationReport()
            };

            var mockQueryable = reports.AsQueryable().BuildMock();
            _locationReportRepositoryMock
                .Setup(x => x.GetLocationReportsQuery(null, null))
                .Returns(mockQueryable);

            var reportDtos = reports.Skip(2).Take(2).Select(CreateLocationReportDto).ToList();
            _mapperMock
                .Setup(x => x.Map<List<LocationReportDto>>(It.IsAny<List<LocationReport>>()))
                .Returns(reportDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.TotalCount.Should().Be(5);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(2);
            result.Value.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task Handle_WithNoResults_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery();

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var reports = new List<LocationReport>();
            var mockQueryable = reports.AsQueryable().BuildMock();
            _locationReportRepositoryMock
                .Setup(x => x.GetLocationReportsQuery(null, null))
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationReportDto>>(It.IsAny<List<LocationReport>>()))
                .Returns(new List<LocationReportDto>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        #endregion

        #region Error Cases

        [Fact]
        public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
        {
            // Arrange
            var query = CreateValidQuery();
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_WithNullUserId_ShouldReturnFailure()
        {
            // Arrange
            var query = CreateValidQuery();
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_WithInvalidGuidFormat_ShouldReturnFailure()
        {
            // Arrange
            var query = CreateValidQuery();
            _currentUserMock.Setup(x => x.Id).Returns("not-a-guid");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Different Status Values

        [Theory]
        [InlineData("PENDING")]
        [InlineData("APPROVED")]
        [InlineData("REJECTED")]
        public async Task Handle_WithDifferentStatuses_ShouldCallRepositoryWithCorrectStatus(string status)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(status: status);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var reports = new List<LocationReport>();
            var mockQueryable = reports.AsQueryable().BuildMock();
            _locationReportRepositoryMock
                .Setup(x => x.GetLocationReportsQuery(status, null))
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationReportDto>>(It.IsAny<List<LocationReport>>()))
                .Returns(new List<LocationReportDto>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationReportRepositoryMock.Verify(
                x => x.GetLocationReportsQuery(status, null),
                Times.Once);
        }

        #endregion

        #region Different Report Types

        [Theory]
        [InlineData("WRONG_INFO")]
        [InlineData("CLOSED")]
        public async Task Handle_WithDifferentReportTypes_ShouldCallRepositoryWithCorrectType(string reportType)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(reportType: reportType);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());

            var reports = new List<LocationReport>();
            var mockQueryable = reports.AsQueryable().BuildMock();
            _locationReportRepositoryMock
                .Setup(x => x.GetLocationReportsQuery(null, reportType))
                .Returns(mockQueryable);

            _mapperMock
                .Setup(x => x.Map<List<LocationReportDto>>(It.IsAny<List<LocationReport>>()))
                .Returns(new List<LocationReportDto>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationReportRepositoryMock.Verify(
                x => x.GetLocationReportsQuery(null, reportType),
                Times.Once);
        }

        #endregion
    }
}

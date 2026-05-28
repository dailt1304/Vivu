using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Statistics;
using Vivu.Application.UseCases.Statictis.Queries.GetLocationStats;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Statictis.Queries.GetLocationStats
{
    public class GetLocationStatsQueryHandlerTests
    {
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<ILocationReportRepository> _locationReportRepositoryMock;
        private readonly Mock<ILogger<GetLocationStatsQueryHandler>> _loggerMock;
        private readonly GetLocationStatsQueryHandler _handler;

        public GetLocationStatsQueryHandlerTests()
        {
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _locationReportRepositoryMock = new Mock<ILocationReportRepository>();
            _loggerMock = new Mock<ILogger<GetLocationStatsQueryHandler>>();

            _handler = new GetLocationStatsQueryHandler(
                _locationRepositoryMock.Object,
                _locationReportRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private void SetupDefaultRepositoryMocks(
            Dictionary<string, int>? locationsByCategory = null,
            int pendingCount = 5,
            Dictionary<string, int>? reportsByType = null,
            decimal verificationRate = 85.5m,
            int totalVerified = 100)
        {
            _locationRepositoryMock
                .Setup(x => x.GetLocationsByCategoryAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(locationsByCategory ?? new Dictionary<string, int>
                {
                    { "Food & Drink", 10 },
                    { "Nature", 20 },
                    { "Entertainment", 15 }
                });

            _locationRepositoryMock
                .Setup(x => x.GetPendingSubmissionsCountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingCount);

            _locationReportRepositoryMock
                .Setup(x => x.GetReportsByTypeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(reportsByType ?? new Dictionary<string, int>
                {
                    { "WRONG_INFO", 5 },
                    { "CLOSED", 3 }
                });

            _locationRepositoryMock
                .Setup(x => x.GetVerificationRateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(verificationRate);

            _locationRepositoryMock
                .Setup(x => x.GetTotalVerifiedLocationsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(totalVerified);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithValidData_ReturnsSuccessResult()
        {
            // Arrange
            SetupDefaultRepositoryMocks();
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithValidData_ReturnsCorrectLocationsByCategory()
        {
            // Arrange
            var expectedCategories = new Dictionary<string, int>
            {
                { "Food & Drink", 42 },
                { "Nature", 17 },
                { "Entertainment", 8 }
            };
            SetupDefaultRepositoryMocks(locationsByCategory: expectedCategories);
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.LocationsByCategory.Should().BeEquivalentTo(expectedCategories);
            result.Value.LocationsByCategory.Should().HaveCount(3);
        }

        [Fact]
        public async Task Handle_WithValidData_ReturnsCorrectPendingSubmissionsCount()
        {
            // Arrange
            SetupDefaultRepositoryMocks(pendingCount: 12);
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.PendingSubmissionsCount.Should().Be(12);
        }

        [Fact]
        public async Task Handle_WithValidData_ReturnsCorrectReportsByType()
        {
            // Arrange
            var expectedReports = new Dictionary<string, int>
            {
                { "WRONG_INFO", 10 },
                { "CLOSED", 5 }
            };
            SetupDefaultRepositoryMocks(reportsByType: expectedReports);
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.ReportsByType.Should().BeEquivalentTo(expectedReports);
        }

        [Fact]
        public async Task Handle_WithValidData_ReturnsCorrectVerificationRate()
        {
            // Arrange
            SetupDefaultRepositoryMocks(verificationRate: 75.25m);
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.VerificationRate.Should().Be(75.25m);
        }

        [Fact]
        public async Task Handle_WithValidData_ReturnsCorrectTotalVerifiedLocations()
        {
            // Arrange
            SetupDefaultRepositoryMocks(totalVerified: 250);
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.TotalVerifiedLocations.Should().Be(250);
        }

        [Fact]
        public async Task Handle_WithValidData_ReturnsCompleteStatsDto()
        {
            // Arrange
            var locationsByCategory = new Dictionary<string, int>
            {
                { "Food & Drink", 30 },
                { "Parks", 12 }
            };
            var reportsByType = new Dictionary<string, int>
            {
                { "WRONG_INFO", 7 }
            };

            SetupDefaultRepositoryMocks(
                locationsByCategory: locationsByCategory,
                pendingCount: 8,
                reportsByType: reportsByType,
                verificationRate: 90.0m,
                totalVerified: 180);

            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var dto = result.Value!;
            dto.LocationsByCategory.Should().BeEquivalentTo(locationsByCategory);
            dto.PendingSubmissionsCount.Should().Be(8);
            dto.ReportsByType.Should().BeEquivalentTo(reportsByType);
            dto.VerificationRate.Should().Be(90.0m);
            dto.TotalVerifiedLocations.Should().Be(180);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_WithNoCategoriesData_ReturnsEmptyDictionary()
        {
            // Arrange
            SetupDefaultRepositoryMocks(locationsByCategory: new Dictionary<string, int>());
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.LocationsByCategory.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithNoReports_ReturnsEmptyReportsDictionary()
        {
            // Arrange
            SetupDefaultRepositoryMocks(reportsByType: new Dictionary<string, int>());
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.ReportsByType.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithZeroPendingSubmissions_ReturnsZero()
        {
            // Arrange
            SetupDefaultRepositoryMocks(pendingCount: 0);
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.PendingSubmissionsCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithZeroVerificationRate_ReturnsZero()
        {
            // Arrange
            SetupDefaultRepositoryMocks(verificationRate: 0m);
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.VerificationRate.Should().Be(0m);
        }

        [Fact]
        public async Task Handle_WithFullVerificationRate_Returns100()
        {
            // Arrange
            SetupDefaultRepositoryMocks(verificationRate: 100m, totalVerified: 500);
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.VerificationRate.Should().Be(100m);
            result.Value.TotalVerifiedLocations.Should().Be(500);
        }

        [Fact]
        public async Task Handle_WithZeroTotalVerifiedLocations_ReturnsZero()
        {
            // Arrange
            SetupDefaultRepositoryMocks(totalVerified: 0, verificationRate: 0m);
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalVerifiedLocations.Should().Be(0);
        }

        #endregion

        #region Repository Call Verification Tests

        [Fact]
        public async Task Handle_Always_CallsGetLocationsByCategoryOnce()
        {
            // Arrange
            SetupDefaultRepositoryMocks();
            var query = new GetLocationStatsQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(
                x => x.GetLocationsByCategoryAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Always_CallsGetPendingSubmissionsCountOnce()
        {
            // Arrange
            SetupDefaultRepositoryMocks();
            var query = new GetLocationStatsQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(
                x => x.GetPendingSubmissionsCountAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Always_CallsGetReportsByTypeOnce()
        {
            // Arrange
            SetupDefaultRepositoryMocks();
            var query = new GetLocationStatsQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationReportRepositoryMock.Verify(
                x => x.GetReportsByTypeAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Always_CallsGetVerificationRateOnce()
        {
            // Arrange
            SetupDefaultRepositoryMocks();
            var query = new GetLocationStatsQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(
                x => x.GetVerificationRateAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Always_CallsGetTotalVerifiedLocationsOnce()
        {
            // Arrange
            SetupDefaultRepositoryMocks();
            var query = new GetLocationStatsQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(
                x => x.GetTotalVerifiedLocationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Always_CallsAllFiveRepositoryMethodsExactlyOnce()
        {
            // Arrange
            SetupDefaultRepositoryMocks();
            var query = new GetLocationStatsQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(x => x.GetLocationsByCategoryAsync(It.IsAny<CancellationToken>()), Times.Once);
            _locationRepositoryMock.Verify(x => x.GetPendingSubmissionsCountAsync(It.IsAny<CancellationToken>()), Times.Once);
            _locationRepositoryMock.Verify(x => x.GetVerificationRateAsync(It.IsAny<CancellationToken>()), Times.Once);
            _locationRepositoryMock.Verify(x => x.GetTotalVerifiedLocationsAsync(It.IsAny<CancellationToken>()), Times.Once);
            _locationReportRepositoryMock.Verify(x => x.GetReportsByTypeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Return Type Tests

        [Fact]
        public async Task Handle_ReturnsResultOfCorrectType()
        {
            // Arrange
            SetupDefaultRepositoryMocks();
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Should().BeOfType<LocationStatsDto>();
        }

        [Fact]
        public async Task Handle_ResultValueIsNeverNull()
        {
            // Arrange
            SetupDefaultRepositoryMocks();
            var query = new GetLocationStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.LocationsByCategory.Should().NotBeNull();
            result.Value.ReportsByType.Should().NotBeNull();
        }

        #endregion
    }
}

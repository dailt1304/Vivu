using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.LocationReports.Queries.GetLocationReports;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.LocationReports.Queries.GetLocationReports
{
    public class GetLocationReportsQueryValidatorTests
    {
        private readonly GetLocationReportsQueryValidator _validator;

        public GetLocationReportsQueryValidatorTests()
        {
            _validator = new GetLocationReportsQueryValidator();
        }

        #region Status Validation Tests - Valid Statuses

        [Theory]
        [InlineData("PENDING")]
        [InlineData("APPROVED")]
        [InlineData("REJECTED")]
        public void Validate_ValidStatus_PassesValidation(string status)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = status,
                ReportType = null,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Theory]
        [InlineData("pending")]
        [InlineData("approved")]
        [InlineData("rejected")]
        [InlineData("Pending")]
        [InlineData("Approved")]
        [InlineData("Rejected")]
        public void Validate_ValidStatusDifferentCase_PassesValidation(string status)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = status,
                ReportType = null,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Fact]
        public void Validate_NullStatus_PassesValidation()
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = null,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Fact]
        public void Validate_EmptyStatus_PassesValidation()
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = "",
                ReportType = null,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        #endregion

        #region Status Validation Tests - Invalid Statuses

        [Theory]
        [InlineData("INVALID")]
        [InlineData("CANCELLED")]
        [InlineData("123")]
        [InlineData("test")]
        public void Validate_InvalidStatus_FailsValidation(string status)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = status,
                ReportType = null,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage("Status must be one of: PENDING, APPROVED, REJECTED");
        }

        #endregion

        #region ReportType Validation Tests - Valid Types

        [Theory]
        [InlineData("WRONG_INFO")]
        [InlineData("CLOSED")]
        public void Validate_ValidReportType_PassesValidation(string reportType)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = reportType,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.ReportType);
        }

        [Theory]
        [InlineData("wrong_info")]
        [InlineData("closed")]
        [InlineData("Wrong_Info")]
        [InlineData("Closed")]
        public void Validate_ValidReportTypeDifferentCase_PassesValidation(string reportType)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = reportType,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.ReportType);
        }

        [Fact]
        public void Validate_NullReportType_PassesValidation()
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = null,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.ReportType);
        }

        [Fact]
        public void Validate_EmptyReportType_PassesValidation()
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = "",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.ReportType);
        }

        #endregion

        #region ReportType Validation Tests - Invalid Types

        [Theory]
        [InlineData("DUPLICATE")]
        [InlineData("INVALID")]
        [InlineData("SPAM")]
        [InlineData("123")]
        [InlineData("test")]
        public void Validate_InvalidReportType_FailsValidation(string reportType)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = reportType,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ReportType)
                .WithErrorMessage("Report type must be one of: WRONG_INFO, CLOSED");
        }

        #endregion

        #region Combined Filters Tests

        [Fact]
        public void Validate_ValidStatusAndReportType_PassesValidation()
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = "PENDING",
                ReportType = "WRONG_INFO",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_InvalidStatusAndValidReportType_FailsValidation()
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = "INVALID",
                ReportType = "WRONG_INFO",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status);
            result.ShouldNotHaveValidationErrorFor(x => x.ReportType);
        }

        [Fact]
        public void Validate_ValidStatusAndInvalidReportType_FailsValidation()
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = "PENDING",
                ReportType = "DUPLICATE",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
            result.ShouldHaveValidationErrorFor(x => x.ReportType);
        }

        [Fact]
        public void Validate_InvalidStatusAndInvalidReportType_FailsValidation()
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = "INVALID",
                ReportType = "DUPLICATE",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status);
            result.ShouldHaveValidationErrorFor(x => x.ReportType);
        }

        #endregion

        #region Pagination Tests

        [Theory]
        [InlineData(1, 10)]
        [InlineData(5, 25)]
        [InlineData(10, 50)]
        [InlineData(100, 100)]
        public void Validate_ValidPagination_PassesValidation(int pageNumber, int pageSize)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = null,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Validate_InvalidPageNumber_NormalizesToOne(int pageNumber)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = null,
                PageNumber = pageNumber,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert - PaginationRequest normalizes invalid values, so validation still passes
            result.ShouldNotHaveAnyValidationErrors();
            query.PageNumber.Should().Be(1); // Normalized to 1
        }

        [Theory]
        [InlineData(101)]
        [InlineData(200)]
        [InlineData(1000)]
        public void Validate_PageSizeAboveMaximum_NormalizesToMaximum(int pageSize)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = null,
                PageNumber = 1,
                PageSize = pageSize
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert - PaginationRequest normalizes to max 100
            result.ShouldNotHaveAnyValidationErrors();
            query.PageSize.Should().Be(100); // Normalized to max
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Validate_InvalidPageSize_NormalizesToDefault(int pageSize)
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = null,
                PageNumber = 1,
                PageSize = pageSize
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert - PaginationRequest normalizes to default 10
            result.ShouldNotHaveAnyValidationErrors();
            query.PageSize.Should().Be(10); // Normalized to default
        }

        [Fact]
        public void Validate_DefaultPaginationValues_PassesValidation()
        {
            // Arrange
            var query = new GetLocationReportsQuery
            {
                Status = null,
                ReportType = null
                // PageNumber and PageSize use defaults from PaginationRequest
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
            query.PageNumber.Should().Be(1);
            query.PageSize.Should().Be(10);
        }

        #endregion
    }
}

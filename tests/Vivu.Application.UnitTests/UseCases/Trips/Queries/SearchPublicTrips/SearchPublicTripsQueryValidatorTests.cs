using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Trips.Queries.SearchPublicTrips;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Queries.SearchPublicTrips
{
    public class SearchPublicTripsQueryValidatorTests
    {
        private readonly SearchPublicTripsQueryValidator _validator;

        public SearchPublicTripsQueryValidatorTests()
        {
            _validator = new SearchPublicTripsQueryValidator();
        }

        #region Helper Methods

        private static SearchPublicTripsQuery CreateValidQuery() => new SearchPublicTripsQuery
        {
            SearchTerm = "beach",
            PageNumber = 1,
            PageSize = 10
        };

        #endregion

        #region SearchTerm Validation Tests

        [Fact]
        public void Validate_ValidSearchTerm_PassesValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.SearchTerm = "beach vacation";

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
        }

        [Fact]
        public void Validate_MinimumLengthSearchTerm_PassesValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.SearchTerm = "ab";

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
        }

        [Fact]
        public void Validate_EmptySearchTerm_FailsValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.SearchTerm = "";

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SearchTerm)
                .WithErrorMessage("Search term is required");
        }

        [Fact]
        public void Validate_NullSearchTerm_FailsValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.SearchTerm = null!;

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SearchTerm)
                .WithErrorMessage("Search term is required");
        }

        [Fact]
        public void Validate_SearchTermTooShort_FailsValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.SearchTerm = "a";

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SearchTerm)
                .WithErrorMessage("Search term must be at least 2 characters");
        }

        [Theory]
        [InlineData("beach")]
        [InlineData("mountain hiking")]
        [InlineData("city tour")]
        [InlineData("adventure travel")]
        public void Validate_VariousValidSearchTerms_PassValidation(string searchTerm)
        {
            // Arrange
            var query = CreateValidQuery();
            query.SearchTerm = searchTerm;

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
        }

        #endregion

        #region PageNumber Validation Tests
        // Note: PageNumber validation tests are not included because PaginationRequest
        // clamps PageNumber values in the property setter (values < 1 are clamped to 1)
        // before validation runs, so validation errors for invalid PageNumber never occur.

        [Fact]
        public void Validate_ValidPageNumber_PassesValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.PageNumber = 1;

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        }

        #endregion

        #region PageSize Validation Tests
        // Note: PageSize validation tests for invalid values are not included because
        // PaginationRequest clamps PageSize values in the property setter:
        // - values < 1 are clamped to 10
        // - values > 100 are clamped to 100
        // This clamping happens before validation runs, so validation errors never occur.

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(25)]
        [InlineData(50)]
        [InlineData(100)]
        public void Validate_VariousValidPageSizes_PassValidation(int pageSize)
        {
            // Arrange
            var query = CreateValidQuery();
            query.PageSize = pageSize;

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
        }

        #endregion

        #region Optional Fields Validation Tests

        [Fact]
        public void Validate_WithCityId_PassesValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.CityId = Guid.NewGuid();

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.CityId);
        }

        [Fact]
        public void Validate_WithCountryId_PassesValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.CountryId = Guid.NewGuid();

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.CountryId);
        }

        [Fact]
        public void Validate_WithDuration_PassesValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.Duration = 7;

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Duration);
        }

        [Fact]
        public void Validate_WithNullOptionalFields_PassesValidation()
        {
            // Arrange
            var query = CreateValidQuery();
            query.CityId = null;
            query.CountryId = null;
            query.Duration = null;

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Combined Validation Tests

        [Fact]
        public void Validate_CompleteValidQuery_PassesValidation()
        {
            // Arrange
            var query = new SearchPublicTripsQuery
            {
                SearchTerm = "beach vacation",
                CityId = Guid.NewGuid(),
                CountryId = Guid.NewGuid(),
                Duration = 7,
                PageNumber = 1,
                PageSize = 20
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MinimalValidQuery_PassesValidation()
        {
            // Arrange
            var query = new SearchPublicTripsQuery
            {
                SearchTerm = "ab",
                PageNumber = 1,
                PageSize = 1
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MultipleErrors_ReportsAllErrors()
        {
            // Arrange
            var query = new SearchPublicTripsQuery
            {
                SearchTerm = "a", // Too short
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            // Only SearchTerm should have validation error
            // PageNumber and PageSize are clamped by PaginationRequest before validation
            result.ShouldHaveValidationErrorFor(x => x.SearchTerm);
            result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
            result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
        }

        #endregion
    }
}

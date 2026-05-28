using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Trips.Queries.GetPublicTrips;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Queries.GetPublicTrips
{
    public class GetPublicTripsQueryValidatorTests
    {
        private readonly GetPublicTripsQueryValidator _validator;

        public GetPublicTripsQueryValidatorTests()
        {
            _validator = new GetPublicTripsQueryValidator();
        }

        #region Helper Methods

        private static GetPublicTripsQuery CreateValidQuery() => new GetPublicTripsQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "newest"
        };

        #endregion

        #region Duration Validation

        [Fact]
        public void Validate_WithNullDuration_IsValid()
        {
            var query = CreateValidQuery();
            query.Duration = null;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.Duration);
        }

        [Fact]
        public void Validate_WithPositiveDuration_IsValid()
        {
            var query = CreateValidQuery();
            query.Duration = 7;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.Duration);
        }

        [Fact]
        public void Validate_WithZeroDuration_HasValidationError()
        {
            var query = CreateValidQuery();
            query.Duration = 0;
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.Duration)
                .WithErrorMessage("Duration must be a positive number");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-5)]
        [InlineData(-100)]
        public void Validate_WithNegativeDuration_HasValidationError(int duration)
        {
            var query = CreateValidQuery();
            query.Duration = duration;
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.Duration)
                .WithErrorMessage("Duration must be a positive number");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(30)]
        [InlineData(365)]
        public void Validate_WithValidDurationValues_IsValid(int duration)
        {
            var query = CreateValidQuery();
            query.Duration = duration;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.Duration);
        }

        #endregion

        #region SortBy Validation

        [Theory]
        [InlineData("newest")]
        [InlineData("popular")]
        [InlineData("trending")]
        public void Validate_WithValidSortBy_IsValid(string sortBy)
        {
            var query = CreateValidQuery();
            query.SortBy = sortBy;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WithNullSortBy_IsValid()
        {
            var query = CreateValidQuery();
            query.SortBy = null!;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("NEWEST")]
        [InlineData("recent")]
        [InlineData("date")]
        [InlineData("alphabetical")]
        public void Validate_WithInvalidSortBy_HasValidationError(string sortBy)
        {
            var query = CreateValidQuery();
            query.SortBy = sortBy;
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.SortBy)
                .WithErrorMessage("SortBy must be 'newest', 'popular', or 'trending'");
        }

        #endregion

        #region PageNumber & PageSize Notes
        // Note: PaginationRequest silently clamps PageNumber (<1 → 1) and PageSize (>100 → 100, <1 → 10)
        // at the property setter level, so FluentValidation rules for those constraints never fire in tests.
        // The validator rules exist for API-level model-binding scenarios only.
        #endregion

        #region Combined Validation

        [Fact]
        public void Validate_WithAllValidFields_HasNoValidationErrors()
        {
            var query = new GetPublicTripsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "popular",
                Duration = 7,
                CityId = Guid.NewGuid(),
                CountryId = Guid.NewGuid()
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithMultipleErrors_ReportsAllErrors()
        {
            // PageNumber/PageSize are clamped by PaginationRequest setter, so only
            // SortBy and Duration produce real validation errors here.
            var query = new GetPublicTripsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "invalid",   // error 1
                Duration = -1          // error 2
            };
            var result = _validator.TestValidate(query);
            result.Errors.Should().HaveCountGreaterThan(1);
        }

        #endregion
    }
}

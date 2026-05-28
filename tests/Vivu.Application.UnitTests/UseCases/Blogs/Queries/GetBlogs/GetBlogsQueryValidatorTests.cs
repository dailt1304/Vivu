using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Blogs.Queries.GetBlogs;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Queries.GetBlogs
{
    public class GetBlogsQueryValidatorTests
    {
        private readonly GetBlogsQueryValidator _validator;

        public GetBlogsQueryValidatorTests()
        {
            _validator = new GetBlogsQueryValidator();
        }

        #region Helpers

        private static GetBlogsQuery CreateValidQuery()
            => new GetBlogsQuery { PageNumber = 1, PageSize = 10, SortBy = "newest" };

        #endregion

        #region PageNumber & PageSize Validation
        // Note: PaginationRequest silently clamps PageNumber (<1 → 1) and PageSize
        // (>100 → 100, <1 → DefaultPageSize=10) at the property setter level, so
        // FluentValidation boundary rules for those constraints never fire in unit tests.
        // The rules exist for API-level model-binding scenarios. We only verify the
        // happy path here.

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(100)]
        public void Validate_WithValidPageNumber_HasNoError(int pageNumber)
        {
            var query = CreateValidQuery();
            query.PageNumber = pageNumber;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(25)]
        [InlineData(50)]
        public void Validate_WithValidPageSize_HasNoError(int pageSize)
        {
            var query = CreateValidQuery();
            query.PageSize = pageSize;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
        }

        #endregion

        #region SortBy Validation

        [Theory]
        [InlineData("newest")]
        [InlineData("popular")]
        [InlineData("most_viewed")]
        [InlineData("most_liked")]
        // Validator uses .ToLower() so mixed-case is also accepted
        [InlineData("NEWEST")]
        [InlineData("Popular")]
        [InlineData("MOST_VIEWED")]
        [InlineData("Most_Liked")]
        public void Validate_WithValidSortBy_HasNoError(string sortBy)
        {
            var query = CreateValidQuery();
            query.SortBy = sortBy;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WithNullSortBy_HasNoError()
        {
            var query = CreateValidQuery();
            query.SortBy = null!;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WithEmptySortBy_HasNoError()
        {
            var query = CreateValidQuery();
            query.SortBy = string.Empty;
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WithWhiteSpaceSortBy_HasNoError()
        {
            var query = CreateValidQuery();
            query.SortBy = "   ";
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("Latest")]
        [InlineData("trending")]
        [InlineData("date")]
        [InlineData("views")]
        [InlineData("random")]
        public void Validate_WithInvalidSortBy_HasValidationError(string sortBy)
        {
            var query = CreateValidQuery();
            query.SortBy = sortBy;
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.SortBy)
                .WithErrorMessage("SortBy must be one of: newest, popular, most_viewed, most_liked.");
        }

        #endregion

        #region Combined Validation

        [Fact]
        public void Validate_WithAllValidFields_HasNoErrors()
        {
            var query = new GetBlogsQuery
            {
                PageNumber = 1,
                PageSize = 20,
                SortBy = "most_viewed"
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithDefaultQuery_HasNoErrors()
        {
            var query = new GetBlogsQuery();
            // SortBy default is "newest", PageNumber default is 1, PageSize default is 10
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}

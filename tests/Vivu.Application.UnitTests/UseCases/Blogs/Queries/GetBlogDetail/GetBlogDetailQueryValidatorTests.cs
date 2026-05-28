using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Blogs.Queries.GetBlogDetail;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Queries.GetBlogDetail
{
    public class GetBlogDetailQueryValidatorTests
    {
        private readonly GetBlogDetailQueryValidator _validator;

        public GetBlogDetailQueryValidatorTests()
        {
            _validator = new GetBlogDetailQueryValidator();
        }

        #region Helpers

        private static GetBlogDetailQuery CreateQuery(string idOrSlug)
            => new GetBlogDetailQuery { IdOrSlug = idOrSlug };

        #endregion

        #region IdOrSlug – Invalid Values

        [Fact]
        public void Validate_WhenIdOrSlugIsEmpty_HasValidationError()
        {
            var result = _validator.TestValidate(CreateQuery(string.Empty));
            result.ShouldHaveValidationErrorFor(x => x.IdOrSlug)
                  .WithErrorMessage("Blog ID or Slug is required.");
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Validate_WhenIdOrSlugIsWhitespace_HasValidationError(string whitespace)
        {
            var result = _validator.TestValidate(CreateQuery(whitespace));
            result.ShouldHaveValidationErrorFor(x => x.IdOrSlug);
        }

        #endregion

        #region IdOrSlug – Valid Values

        [Theory]
        [InlineData("my-travel-blog")]
        [InlineData("hanoi-trip-2025")]
        [InlineData("a")]
        [InlineData("some-very-long-slug-name-for-a-blog-post")]
        public void Validate_WhenIdOrSlugIsValidSlug_HasNoValidationError(string slug)
        {
            var result = _validator.TestValidate(CreateQuery(slug));
            result.ShouldNotHaveValidationErrorFor(x => x.IdOrSlug);
        }

        [Fact]
        public void Validate_WhenIdOrSlugIsValidGuid_HasNoValidationError()
        {
            var guid   = Guid.NewGuid().ToString();
            var result = _validator.TestValidate(CreateQuery(guid));
            result.ShouldNotHaveValidationErrorFor(x => x.IdOrSlug);
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000001")]
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")]
        public void Validate_WhenIdOrSlugIsFormattedGuid_HasNoValidationError(string guid)
        {
            var result = _validator.TestValidate(CreateQuery(guid));
            result.ShouldNotHaveValidationErrorFor(x => x.IdOrSlug);
        }

        #endregion

        #region Overall Validity

        [Fact]
        public void Validate_WithValidSlug_IsValid()
        {
            var result = _validator.TestValidate(CreateQuery("valid-slug"));
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithEmptyIdOrSlug_IsNotValid()
        {
            var result = _validator.TestValidate(CreateQuery(string.Empty));
            result.IsValid.Should().BeFalse();
        }

        #endregion
    }
}

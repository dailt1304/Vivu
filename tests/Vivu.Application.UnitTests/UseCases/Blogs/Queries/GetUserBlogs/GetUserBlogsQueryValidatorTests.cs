using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Blogs.Queries.GetUserBlogs;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Queries.GetUserBlogs
{
    public class GetUserBlogsQueryValidatorTests
    {
        private readonly GetUserBlogsQueryValidator _validator;

        public GetUserBlogsQueryValidatorTests()
        {
            _validator = new GetUserBlogsQueryValidator();
        }

        #region Helpers

        private static GetUserBlogsQuery CreateQuery(Guid userId, int pageNumber = 1, int pageSize = 10)
            => new GetUserBlogsQuery { UserId = userId, PageNumber = pageNumber, PageSize = pageSize };

        #endregion

        #region UserId – Invalid

        [Fact]
        public void Validate_WhenUserIdIsEmpty_HasValidationError()
        {
            var result = _validator.TestValidate(new GetUserBlogsQuery { UserId = Guid.Empty });
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                  .WithErrorMessage("User ID is required.");
        }

        [Fact]
        public void Validate_WhenUserIdIsEmpty_IsNotValid()
        {
            var result = _validator.Validate(new GetUserBlogsQuery { UserId = Guid.Empty });
            result.IsValid.Should().BeFalse();
        }

        #endregion

        #region UserId – Valid

        [Fact]
        public void Validate_WhenUserIdIsValidGuid_HasNoValidationError()
        {
            var result = _validator.TestValidate(CreateQuery(Guid.NewGuid()));
            result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WhenUserIdIsValidGuid_IsValid()
        {
            var result = _validator.Validate(CreateQuery(Guid.NewGuid()));
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("11111111-1111-1111-1111-111111111111")]
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")]
        [InlineData("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
        public void Validate_WithVariousValidGuids_HasNoValidationError(string guidStr)
        {
            var result = _validator.TestValidate(CreateQuery(Guid.Parse(guidStr)));
            result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        }

        #endregion

        #region Overall Validity

        [Fact]
        public void Validate_WithValidQuery_IsValid()
        {
            var query  = CreateQuery(Guid.NewGuid(), pageNumber: 1, pageSize: 10);
            var result = _validator.Validate(query);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithEmptyUserIdOnly_HasExactlyOneError()
        {
            var query  = new GetUserBlogsQuery { UserId = Guid.Empty, PageNumber = 1, PageSize = 10 };
            var result = _validator.Validate(query);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].PropertyName.Should().Be(nameof(GetUserBlogsQuery.UserId));
        }

        #endregion
    }
}

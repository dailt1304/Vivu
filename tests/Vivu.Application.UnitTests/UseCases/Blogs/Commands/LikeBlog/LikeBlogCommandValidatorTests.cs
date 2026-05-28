using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Blogs.Commands.LikeBlog;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Commands.LikeBlog
{
    public class LikeBlogCommandValidatorTests
    {
        private readonly LikeBlogCommandValidator _validator;

        public LikeBlogCommandValidatorTests()
        {
            _validator = new LikeBlogCommandValidator();
        }

        #region BlogId – Invalid

        [Fact]
        public void Validate_WhenBlogIdIsEmpty_HasValidationError()
        {
            var command = new LikeBlogCommand { BlogId = Guid.Empty };
            var result  = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.BlogId)
                  .WithErrorMessage("Blog ID is required.");
        }

        [Fact]
        public void Validate_WhenBlogIdIsEmpty_IsNotValid()
        {
            var command = new LikeBlogCommand { BlogId = Guid.Empty };
            var result  = _validator.Validate(command);
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_WhenBlogIdIsEmpty_HasExactlyOneError()
        {
            var command = new LikeBlogCommand { BlogId = Guid.Empty };
            var result  = _validator.Validate(command);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].PropertyName.Should().Be(nameof(LikeBlogCommand.BlogId));
        }

        #endregion

        #region BlogId – Valid

        [Fact]
        public void Validate_WhenBlogIdIsValidGuid_HasNoValidationError()
        {
            var command = new LikeBlogCommand { BlogId = Guid.NewGuid() };
            var result  = _validator.TestValidate(command);
            result.ShouldNotHaveValidationErrorFor(x => x.BlogId);
        }

        [Fact]
        public void Validate_WhenBlogIdIsValidGuid_IsValid()
        {
            var command = new LikeBlogCommand { BlogId = Guid.NewGuid() };
            var result  = _validator.Validate(command);
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("11111111-1111-1111-1111-111111111111")]
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")]
        [InlineData("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
        public void Validate_WithVariousValidGuids_HasNoValidationError(string guidStr)
        {
            var command = new LikeBlogCommand { BlogId = Guid.Parse(guidStr) };
            var result  = _validator.TestValidate(command);
            result.ShouldNotHaveValidationErrorFor(x => x.BlogId);
        }

        #endregion
    }
}

using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Blogs.Commands.CreateComment;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Commands.CreateComment
{
    public class CreateCommentCommandValidatorTests
    {
        private readonly CreateCommentCommandValidator _validator = new();

        private static CreateCommentCommand Make(Guid? blogId = null, string content = "Valid content")
            => new CreateCommentCommand
            {
                BlogId  = blogId ?? Guid.NewGuid(),
                Content = content
            };

        #region BlogId

        [Fact]
        public void Validate_WhenBlogIdIsEmpty_HasError()
        {
            var result = _validator.TestValidate(Make(blogId: Guid.Empty));
            result.ShouldHaveValidationErrorFor(x => x.BlogId)
                  .WithErrorMessage("Blog ID is required.");
        }

        [Fact]
        public void Validate_WhenBlogIdIsValid_HasNoError()
        {
            var result = _validator.TestValidate(Make());
            result.ShouldNotHaveValidationErrorFor(x => x.BlogId);
        }

        #endregion

        #region Content

        [Fact]
        public void Validate_WhenContentIsEmpty_HasError()
        {
            var result = _validator.TestValidate(Make(content: string.Empty));
            result.ShouldHaveValidationErrorFor(x => x.Content)
                  .WithErrorMessage("Comment content is required.");
        }

        [Fact]
        public void Validate_WhenContentExceeds2000Chars_HasError()
        {
            var result = _validator.TestValidate(Make(content: new string('a', 2001)));
            result.ShouldHaveValidationErrorFor(x => x.Content)
                  .WithErrorMessage("Comment must not exceed 2000 characters.");
        }

        [Fact]
        public void Validate_WhenContentIsExactly2000Chars_HasNoError()
        {
            var result = _validator.TestValidate(Make(content: new string('a', 2000)));
            result.ShouldNotHaveValidationErrorFor(x => x.Content);
        }

        [Theory]
        [InlineData("Hi")]
        [InlineData("Normal comment text")]
        public void Validate_WhenContentIsValid_HasNoError(string content)
        {
            var result = _validator.TestValidate(Make(content: content));
            result.ShouldNotHaveValidationErrorFor(x => x.Content);
        }

        #endregion

        #region Overall

        [Fact]
        public void Validate_WithValidCommand_IsValid()
        {
            _validator.Validate(Make()).IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithBothFieldsInvalid_HasTwoErrors()
        {
            var result = _validator.Validate(new CreateCommentCommand
            {
                BlogId  = Guid.Empty,
                Content = string.Empty
            });
            result.Errors.Should().HaveCount(2);
        }

        #endregion
    }
}

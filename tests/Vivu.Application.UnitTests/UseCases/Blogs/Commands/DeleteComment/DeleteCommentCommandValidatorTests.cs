using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Blogs.Commands.DeleteComment;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Commands.DeleteComment
{
    public class DeleteCommentCommandValidatorTests
    {
        private readonly DeleteCommentCommandValidator _validator = new();

        #region CommentId – Invalid

        [Fact]
        public void Validate_WhenCommentIdIsEmpty_HasError()
        {
            var result = _validator.TestValidate(new DeleteCommentCommand { CommentId = Guid.Empty });
            result.ShouldHaveValidationErrorFor(x => x.CommentId)
                  .WithErrorMessage("Comment ID is required.");
        }

        [Fact]
        public void Validate_WhenCommentIdIsEmpty_IsNotValid()
        {
            _validator.Validate(new DeleteCommentCommand { CommentId = Guid.Empty })
                      .IsValid.Should().BeFalse();
        }

        #endregion

        #region CommentId – Valid

        [Fact]
        public void Validate_WhenCommentIdIsValidGuid_HasNoError()
        {
            var result = _validator.TestValidate(new DeleteCommentCommand { CommentId = Guid.NewGuid() });
            result.ShouldNotHaveValidationErrorFor(x => x.CommentId);
        }

        [Fact]
        public void Validate_WhenCommentIdIsValidGuid_IsValid()
        {
            _validator.Validate(new DeleteCommentCommand { CommentId = Guid.NewGuid() })
                      .IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("11111111-1111-1111-1111-111111111111")]
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")]
        public void Validate_WithVariousValidGuids_HasNoError(string guidStr)
        {
            var result = _validator.TestValidate(new DeleteCommentCommand { CommentId = Guid.Parse(guidStr) });
            result.ShouldNotHaveValidationErrorFor(x => x.CommentId);
        }

        #endregion
    }
}

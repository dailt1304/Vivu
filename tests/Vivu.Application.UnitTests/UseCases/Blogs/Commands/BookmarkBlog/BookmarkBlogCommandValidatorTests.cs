using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Blogs.Commands.BookmarkBlog;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Commands.BookmarkBlog
{
    public class BookmarkBlogCommandValidatorTests
    {
        private readonly BookmarkBlogCommandValidator _validator = new();

        private static BookmarkBlogCommand Make(Guid? blogId = null)
            => new BookmarkBlogCommand { BlogId = blogId ?? Guid.NewGuid() };

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
    }
}

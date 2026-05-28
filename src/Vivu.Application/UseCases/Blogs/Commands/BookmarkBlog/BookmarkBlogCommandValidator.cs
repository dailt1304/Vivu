using FluentValidation;

namespace Vivu.Application.UseCases.Blogs.Commands.BookmarkBlog
{
    public class BookmarkBlogCommandValidator : AbstractValidator<BookmarkBlogCommand>
    {
        public BookmarkBlogCommandValidator()
        {
            RuleFor(x => x.BlogId)
                .NotEmpty().WithMessage("Blog ID is required.");
        }
    }
}

using FluentValidation;

namespace Vivu.Application.UseCases.Blogs.Commands.LikeBlog
{
    public class LikeBlogCommandValidator : AbstractValidator<LikeBlogCommand>
    {
        public LikeBlogCommandValidator()
        {
            RuleFor(x => x.BlogId)
                .NotEmpty()
                .WithMessage("Blog ID is required.");
        }
    }
}

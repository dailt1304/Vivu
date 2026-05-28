using FluentValidation;

namespace Vivu.Application.UseCases.BlogReports.Commands.UnbanBlog;

public class UnbanBlogCommandValidator : AbstractValidator<UnbanBlogCommand>
{
    public UnbanBlogCommandValidator()
    {
        RuleFor(x => x.BlogId)
            .NotEmpty()
            .WithMessage("Blog ID is required");
    }
}

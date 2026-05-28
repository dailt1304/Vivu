using FluentValidation;

namespace Vivu.Application.UseCases.BlogReports.Commands.BanBlog;

public class BanBlogCommandValidator : AbstractValidator<BanBlogCommand>
{
    public BanBlogCommandValidator()
    {
        RuleFor(x => x.BlogId)
            .NotEmpty()
            .WithMessage("Blog ID is required");
    }
}

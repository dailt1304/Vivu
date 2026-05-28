using FluentValidation;
using Vivu.Domain.Enums;

namespace Vivu.Application.UseCases.Blogs.Commands.FlagBlog;

public class FlagBlogCommandValidator : AbstractValidator<FlagBlogCommand>
{
    public FlagBlogCommandValidator()
    {
        RuleFor(x => x.BlogId)
            .NotEmpty().WithMessage("Blog ID is required");

        RuleFor(x => x.ReportType)
            .IsInEnum().WithMessage("Invalid report type. Valid values are: SPAM, INAPPROPRIATE, MISLEADING, COPYRIGHT, OTHER");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

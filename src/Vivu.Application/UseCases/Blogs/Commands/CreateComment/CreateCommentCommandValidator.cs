using FluentValidation;

namespace Vivu.Application.UseCases.Blogs.Commands.CreateComment
{
    public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
    {
        public CreateCommentCommandValidator()
        {
            RuleFor(x => x.BlogId)
                .NotEmpty().WithMessage("Blog ID is required.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required.")
                .MaximumLength(2000).WithMessage("Comment must not exceed 2000 characters.");
        }
    }
}

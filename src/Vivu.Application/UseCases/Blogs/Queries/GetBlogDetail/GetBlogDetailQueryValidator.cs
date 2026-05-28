using FluentValidation;

namespace Vivu.Application.UseCases.Blogs.Queries.GetBlogDetail
{
    public class GetBlogDetailQueryValidator : AbstractValidator<GetBlogDetailQuery>
    {
        public GetBlogDetailQueryValidator()
        {
            RuleFor(x => x.IdOrSlug)
                .NotEmpty()
                .WithMessage("Blog ID or Slug is required.");
        }
    }
}

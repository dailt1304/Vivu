using FluentValidation;

namespace Vivu.Application.UseCases.Blogs.Queries.SearchPublicBlogs
{
    public class SearchPublicBlogsQueryValidator : AbstractValidator<SearchPublicBlogsQuery>
    {
        public SearchPublicBlogsQueryValidator()
        {
            RuleFor(x => x.SearchTerm)
                .NotEmpty()
                .WithMessage("Search term is required")
                .MinimumLength(2)
                .WithMessage("Search term must be at least 2 characters");
        }
    }
}

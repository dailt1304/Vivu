using FluentValidation;

namespace Vivu.Application.UseCases.Blogs.Queries.GetBlogs
{
    public class GetBlogsQueryValidator : AbstractValidator<GetBlogsQuery>
    {
        private static readonly string[] AllowedSortValues = { "newest", "popular", "most_viewed", "most_liked" };

        public GetBlogsQueryValidator()
        {
            RuleFor(x => x.SortBy)
                .Must(v => string.IsNullOrWhiteSpace(v) || AllowedSortValues.Contains(v.ToLower()))
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortValues)}.");
        }
    }
}

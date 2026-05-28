using FluentValidation;

namespace Vivu.Application.UseCases.Blogs.Queries.GetUserBlogs
{
    public class GetUserBlogsQueryValidator : AbstractValidator<GetUserBlogsQuery>
    {
        public GetUserBlogsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");
        }
    }
}

using FluentValidation;

namespace Vivu.Application.UseCases.Users.Queries.SearchUsers
{
    public class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
    {
        public SearchUsersQueryValidator()
        {
            RuleFor(x => x.SearchTerm)
                .MaximumLength(100).WithMessage("Search term must not exceed 100 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));

            RuleFor(x => x.Status)
                .Must(status => status == "active" || status == "banned")
                .WithMessage("Status must be either 'active' or 'banned'")
                .When(x => !string.IsNullOrWhiteSpace(x.Status));

            RuleFor(x => x.Role)
                .MaximumLength(50).WithMessage("Role must not exceed 50 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Role));

            RuleFor(x => x.CreatedFrom)
                .LessThanOrEqualTo(x => x.CreatedTo)
                .WithMessage("CreatedFrom must be before or equal to CreatedTo")
                .When(x => x.CreatedFrom.HasValue && x.CreatedTo.HasValue);

            RuleFor(x => x.CreatedTo)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("CreatedTo cannot be in the future")
                .When(x => x.CreatedTo.HasValue);
        }
    }
}

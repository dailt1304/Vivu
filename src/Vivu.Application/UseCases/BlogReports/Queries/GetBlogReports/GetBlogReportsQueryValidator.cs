using FluentValidation;

namespace Vivu.Application.UseCases.BlogReports.Queries.GetBlogReports;

public class GetBlogReportsQueryValidator : AbstractValidator<GetBlogReportsQuery>
{
    public GetBlogReportsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrEmpty(status) ||
                           new[] { "PENDING", "APPROVED", "REJECTED" }.Contains(status.ToUpper()))
            .WithMessage("Status must be one of: PENDING, APPROVED, REJECTED")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.ReportType)
            .Must(type => string.IsNullOrEmpty(type) ||
                         new[] { "SPAM", "INAPPROPRIATE", "MISLEADING", "COPYRIGHT", "OTHER" }.Contains(type.ToUpper()))
            .WithMessage("Report type must be one of: SPAM, INAPPROPRIATE, MISLEADING, COPYRIGHT, OTHER")
            .When(x => !string.IsNullOrEmpty(x.ReportType));
    }
}

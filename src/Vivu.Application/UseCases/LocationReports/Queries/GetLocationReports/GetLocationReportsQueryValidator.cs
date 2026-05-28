using FluentValidation;

namespace Vivu.Application.UseCases.LocationReports.Queries.GetLocationReports;

public class GetLocationReportsQueryValidator : AbstractValidator<GetLocationReportsQuery>
{
    public GetLocationReportsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrEmpty(status) || 
                           new[] { "PENDING", "APPROVED", "REJECTED"}.Contains(status.ToUpper()))
            .WithMessage("Status must be one of: PENDING, APPROVED, REJECTED")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.ReportType)
            .Must(type => string.IsNullOrEmpty(type) || 
                         new[] { "WRONG_INFO", "CLOSED" }.Contains(type.ToUpper()))
            .WithMessage("Report type must be one of: WRONG_INFO, CLOSED")
            .When(x => !string.IsNullOrEmpty(x.ReportType));
    }
}

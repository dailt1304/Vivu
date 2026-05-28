using FluentValidation;
using Vivu.Domain.Enums;

namespace Vivu.Application.UseCases.BlogReports.Commands.ReviewBlogReport;

public class ReviewBlogReportCommandValidator : AbstractValidator<ReviewBlogReportCommand>
{
    public ReviewBlogReportCommandValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty()
            .WithMessage("Report ID is required");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid report status")
            .Must(status => status != ReportStatus.PENDING)
            .WithMessage("Cannot set status back to PENDING");

        RuleFor(x => x.AdminNote)
            .MaximumLength(1000)
            .WithMessage("Admin note cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.AdminNote));
    }
}

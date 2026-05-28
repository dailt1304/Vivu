using FluentValidation;

namespace Vivu.Application.UseCases.LocationReports.Commands.UpdateLocationReport
{
    public class UpdateLocationReportCommandValidator : AbstractValidator<UpdateLocationReportCommand>
    {
        public UpdateLocationReportCommandValidator()
        {
            RuleFor(x => x.ReportId)
                .NotEmpty()
                .WithMessage("ReportId is required.");

            RuleFor(x => x.ReportType)
                .NotEmpty()
                .WithMessage("ReportType is required.");

            RuleFor(x => x.ReportReason)
                .NotEmpty()
                .WithMessage("ReportReason is required.");
        }
    }
}

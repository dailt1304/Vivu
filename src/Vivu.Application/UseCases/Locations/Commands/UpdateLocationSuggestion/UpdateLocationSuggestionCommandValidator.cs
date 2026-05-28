using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.Locations.Commands.UpdateLocationSuggestion
{
    public class UpdateLocationSuggestionCommandValidator : AbstractValidator<UpdateLocationSuggestionCommand>
    {
        public UpdateLocationSuggestionCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .NotEmpty().WithMessage("LocationId is required");

            RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Address).MaximumLength(300);

            RuleFor(x => x)
                .Must(x =>
                    !string.IsNullOrWhiteSpace(x.Address) ||
                    (x.Latitude.HasValue && x.Longitude.HasValue))
                .WithMessage("Provide either Address, or both Latitude and Longitude.");

            RuleFor(x => x)
                .Must(x =>
                    (x.Latitude.HasValue && x.Longitude.HasValue) ||
                    (!x.Latitude.HasValue && !x.Longitude.HasValue))
                .WithMessage("Latitude and Longitude must be provided together.");

            When(x => x.Latitude.HasValue, () =>
            {
                RuleFor(x => x.Latitude!.Value).InclusiveBetween(-90, 90);
            });

            When(x => x.Longitude.HasValue, () =>
            {
                RuleFor(x => x.Longitude!.Value).InclusiveBetween(-180, 180);
            });

            RuleFor(x => x.Phone).MaximumLength(30);
            RuleFor(x => x.Website).MaximumLength(300);
            RuleFor(x => x.Tags).MaximumLength(500);

            RuleFor(x => x.Images)
                .NotNull().WithMessage("Images is required")
                .NotEmpty().WithMessage("At least one image is required");

            When(x => x.Images != null, () =>
            {
                RuleForEach(x => x.Images)
                    .Must(file => file != null && file.Length <= 5 * 1024 * 1024)
                    .WithMessage("Each image must be smaller than 5MB");
            });
        }
    }
}

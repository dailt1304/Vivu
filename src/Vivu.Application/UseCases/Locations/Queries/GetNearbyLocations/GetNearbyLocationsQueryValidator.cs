using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Vivu.Application.UseCases.Locations.Queries.GetNearbyLocations
{
    public class GetNearbyLocationsQueryValidator : AbstractValidator<GetNearbyLocationsQuery>
    {
        public GetNearbyLocationsQueryValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180.");

            RuleFor(x => x.RadiusInMeters)
                .GreaterThanOrEqualTo(100)
                .WithMessage("Radius must be at least 100 meters.")
                .LessThanOrEqualTo(50000)
                .WithMessage("Radius cannot exceed 50,000 meters (50 km).");

            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty)
                .When(x => x.CategoryId.HasValue)
                .WithMessage("Category ID must be a valid GUID.");

            RuleFor(x => x.MinRating)
                .InclusiveBetween(0, 5)
                .When(x => x.MinRating.HasValue)
                .WithMessage("Minimum rating must be between 0 and 5.");

            RuleFor(x => x.Limit)
                .GreaterThan(0)
                .WithMessage("Limit must be greater than 0.")
                .LessThanOrEqualTo(100)
                .WithMessage("Limit cannot exceed 100.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Vivu.Application.UseCases.Locations.Queries.GetLocationsWithFilters
{
    public class GetLocationsByFilterQueryValidator : AbstractValidator<GetLocationsByFilterQuery>
    {
        public GetLocationsByFilterQueryValidator()
        {

            RuleFor(x => x.CityId)
                .NotEqual(Guid.Empty)
                .When(x => x.CityId.HasValue)
                .WithMessage("City ID must be a valid GUID.");

            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty)
                .When(x => x.CategoryId.HasValue)
                .WithMessage("Category ID must be a valid GUID.");

            RuleFor(x => x.MinRating)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum rating must be at least 0.")
                .LessThanOrEqualTo(5)
                .WithMessage("Minimum rating cannot exceed 5.")
                .When(x => x.MinRating.HasValue);

            RuleFor(x => x.UserLatitude)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90.")
                .When(x => x.UserLatitude.HasValue);

            RuleFor(x => x.UserLongitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180.")
                .When(x => x.UserLongitude.HasValue);

            RuleFor(x => x)
                .Must(x => (x.UserLatitude.HasValue && x.UserLongitude.HasValue) ||
                           (!x.UserLatitude.HasValue && !x.UserLongitude.HasValue))
                .WithMessage("Both latitude and longitude must be provided together.")
                .When(x => x.UserLatitude.HasValue || x.UserLongitude.HasValue);

            RuleFor(x => x.RadiusInMeters)
                .GreaterThan(0)
                .WithMessage("Radius must be greater than 0.")
                .LessThanOrEqualTo(50000)
                .WithMessage("Radius cannot exceed 50,000 meters (50 km).")
                .When(x => x.RadiusInMeters.HasValue);

            RuleFor(x => x)
                .Must(x => x.UserLatitude.HasValue && x.UserLongitude.HasValue)
                .When(x => x.RadiusInMeters.HasValue)
                .WithMessage("User coordinates (latitude and longitude) are required when using radius filter.");

            RuleFor(x => x.SortBy)
                .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) ||
                               new[] { "rating", "distance", "recent", "name" }
                                   .Contains(sortBy.ToLower()))
                .WithMessage("Sort by must be one of: 'rating', 'distance', 'recent', 'name'.")
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

            RuleFor(x => x)
                .Must(x => x.UserLatitude.HasValue && x.UserLongitude.HasValue)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy) &&
                           x.SortBy.Equals("distance", StringComparison.OrdinalIgnoreCase))
                .WithMessage("User coordinates (latitude and longitude) are required when sorting by distance.");

            RuleFor(x => x.SortColumn)
                .Must(column => string.IsNullOrWhiteSpace(column) ||
                               new[] { "Name", "RatingAverage", "CreatedAt", "RatingCount" }
                                   .Contains(column, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Sort column must be one of: 'Name', 'RatingAverage', 'CreatedAt', 'RatingCount'.")
                .When(x => !string.IsNullOrWhiteSpace(x.SortColumn));
        }
    }
}

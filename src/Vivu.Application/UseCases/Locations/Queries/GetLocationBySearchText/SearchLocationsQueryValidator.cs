using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Vivu.Application.UseCases.Locations.Queries.GetLocationBySearchText
{
    public class SearchLocationsQueryValidator : AbstractValidator<SearchLocationsQuery>
    {
        public SearchLocationsQueryValidator()
        {
            RuleFor(x => x.SearchTerm)
                .NotEmpty()
                .WithMessage("Search term is required.")
                .MinimumLength(2)
                .WithMessage("Search term must be at least 2 characters.")
                .MaximumLength(255)
                .WithMessage("Search term cannot exceed 255 characters.");

            RuleFor(x => x.CityId)
                .NotEqual(Guid.Empty)
                .When(x => x.CityId.HasValue)
                .WithMessage("City ID must be a valid GUID.");

            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty)
                .When(x => x.CategoryId.HasValue)
                .WithMessage("Category ID must be a valid GUID.");

            RuleFor(x => x.MinRating)
                .InclusiveBetween(0, 5)
                .When(x => x.MinRating.HasValue)
                .WithMessage("Minimum rating must be between 0 and 5.");
        }
    }
}

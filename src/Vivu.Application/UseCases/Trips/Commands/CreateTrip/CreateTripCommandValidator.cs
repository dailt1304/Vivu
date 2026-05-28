using FluentValidation;
using Vivu.Domain.Interfaces;

namespace Vivu.Application.UseCases.Trips.Commands.CreateTrip
{
    public class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
    {
        private readonly ICityRepository _cityRepository;

        public CreateTripCommandValidator(ICityRepository cityRepository)
        {
            _cityRepository = cityRepository;

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.CityId)
                .MustAsync(async (cityId, cancellationToken) =>
                {
                    if (!cityId.HasValue) return true; // CityId is optional
                    var city = await _cityRepository.GetByIdAsync(cityId.Value);
                    return city != null;
                })
                .WithMessage("City does not exist")
                .When(x => x.CityId.HasValue);

            RuleFor(x => x.StartDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future")
                .When(x => x.StartDate.HasValue);

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

            RuleFor(x => x.TripSize)
                .GreaterThan(0).WithMessage("Trip size must be greater than 0")
                .When(x => x.TripSize.HasValue);
        }
    }
}

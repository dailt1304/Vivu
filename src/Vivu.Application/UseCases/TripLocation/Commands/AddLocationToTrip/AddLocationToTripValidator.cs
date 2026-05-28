using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.TripLocation.Commands.AddLocationToTrip
{
    public class AddLocationToTripValidator : AbstractValidator<AddLocationToTripCommand>
    {
        public AddLocationToTripValidator()
        {
            RuleFor(x => x.TripDayId)
                .NotEmpty().WithMessage("TripDayId is required");

            RuleFor(x => x.LocationId)
                .NotEmpty().WithMessage("LocationId is required");

            RuleFor(x => x.OrderIndex)
                .GreaterThanOrEqualTo(0).WithMessage("OrderIndex must be greater than or equal to 0");

            RuleFor(x => x.StartTime)
                .LessThan(x => x.EndTime)
                .When(x => x.StartTime.HasValue && x.EndTime.HasValue)
                .WithMessage("StartTime must be less than EndTime");
        }
    }
}

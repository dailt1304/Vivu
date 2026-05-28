using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Vivu.Application.DTOs.Responses.AI;

namespace Vivu.Application.UseCases.AI.CreateTrip
{
    public class AICreateTripCommandValidator : AbstractValidator<AICreateTripCommand>
    {
        public AICreateTripCommandValidator()
        {
            RuleFor(x => x.TripPlan)
                .NotNull().WithMessage("Trip plan is required");

            RuleFor(x => x.TripPlan.Title)
                .NotEmpty().WithMessage("Trip title is required")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.TripPlan.Days)
                .NotEmpty().WithMessage("Trip must have at least one day")
                .Must(days => days.Count <= 30).WithMessage("Trip cannot exceed 30 days");

            RuleFor(x => x.TripPlan)
                .Must(plan => plan.Start <= plan.End)
                .WithMessage("Start date must be before end date")
                .When(x => x.TripPlan.Start != DateOnly.MinValue && x.TripPlan.End != DateOnly.MinValue);

            RuleFor(x => x.TripPlan.Days)
                .Must(days => days.All(d => d.Locations.Any()))
                .WithMessage("Each day must have at least one location");

            RuleFor(x => x.TripPlan.Days)
                .Must(HaveValidLocationIds)
                .WithMessage("All locations must have valid location IDs");
        }

        private bool HaveValidLocationIds(List<DayPlanDto> days)
        {
            return days.All(day =>
                day.Locations.All(loc => loc.LocationId != Guid.Empty)
            );
        }
    }
}

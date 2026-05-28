using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripDay.Commands.AddTripDay
{
    public class AddTripDayValidator : AbstractValidator<AddTripDayCommand>
    {
        public AddTripDayValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty()
                .WithMessage("TripId is required.");

            RuleFor(x => x.Title)
                .MaximumLength(200)
                .WithMessage("Title must be at most 200 characters.");
        }

    }
}

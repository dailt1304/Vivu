using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Vivu.Application.UseCases.TripDay.Commands.AddTripDay;

namespace Vivu.Application.UseCases.TripMember.Command.RemoveMemberFromTrip
{
    public class RemoveMemberFromTripValidator : AbstractValidator<RemoveMemberFromTripCommand>
    {
        public RemoveMemberFromTripValidator()
        {
            RuleFor(x => x.TripId)
               .NotEmpty()
               .WithMessage("TripId is required.");

            RuleFor(x => x.UserId)
               .NotEmpty()
               .WithMessage("MemberId is required.");
        }
    }
}

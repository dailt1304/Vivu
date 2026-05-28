using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateMemberRole
{
    public class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
    {
        public UpdateMemberRoleCommandValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty().WithMessage("TripId is required.");

            RuleFor(x => x.MemberUserId)
                .NotEmpty().WithMessage("MemberUserId is required.");

            RuleFor(x => x.NewRole).IsInEnum().WithMessage("NewRole must be a valid TripRole enum value.");
        }
    }
}

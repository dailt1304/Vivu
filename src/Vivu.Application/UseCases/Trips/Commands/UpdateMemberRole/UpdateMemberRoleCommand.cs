using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateMemberRole
{
    public class UpdateMemberRoleCommand : IRequest<Result<TripMemberDto>>
    {
        public Guid TripId { get; set; }
        public Guid MemberUserId { get; set; }
        public TripRole NewRole { get; set; } 
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripMember.Command.RemoveMemberFromTrip
{
    public class RemoveMemberFromTripCommand : IRequest<Result<string>>
    {
        public Guid TripId { get; set; }
        public Guid UserId { get; set; }
    }
}

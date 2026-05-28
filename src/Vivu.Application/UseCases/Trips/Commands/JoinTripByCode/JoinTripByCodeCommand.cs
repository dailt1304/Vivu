using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.JoinTripByCode
{
    public record JoinTripByCodeCommand(string InviteCode) : IRequest<Result<TripMemberDto>>;

}

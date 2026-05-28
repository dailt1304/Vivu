using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetUserTrips
{
    public class GetUserTripsQuery : PaginationRequest, IRequest<Result<UserTripsResponseDto>>
    {
        public Guid UserId { get; set; }
        public string? Status { get; set; }
    }
}

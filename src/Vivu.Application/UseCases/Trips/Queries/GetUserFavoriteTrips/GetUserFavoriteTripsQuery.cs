using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetUserFavoriteTrips
{
    public class GetUserFavoriteTripsQuery : PaginationRequest, IRequest<Result<PaginatedList<TripDto>>>
    {
    }
}

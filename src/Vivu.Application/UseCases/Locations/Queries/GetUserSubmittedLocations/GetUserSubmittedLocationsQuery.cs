using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetUserSubmittedLocations
{
    public class GetUserSubmittedLocationsQuery : PaginationRequest, IRequest<Result<PaginatedList<LocationDto>>>
    {
        public ReportStatus? Status { get; set; }
    }
}

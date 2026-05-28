using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetLocationBySearchText
{
    public class SearchLocationsQuery : PaginationRequest, IRequest<Result<PaginatedList<LocationSearchResultDto>>>
    {
        public string SearchTerm { get; set; } = string.Empty;

        public Guid? CityId { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal? MinRating { get; set; }
        public bool IsVerifiedOnly { get; set; } = true; 

        public bool IncludeHighlights { get; set; } = true;
    }
}

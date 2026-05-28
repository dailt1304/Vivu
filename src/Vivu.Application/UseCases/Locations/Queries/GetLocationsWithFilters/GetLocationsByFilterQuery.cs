using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetLocationsWithFilters
{
    public class GetLocationsByFilterQuery : PaginationRequest, IRequest<Result<PaginatedList<LocationDto>>>
    {
        public Guid? CityId { get; set; }
        public Guid? CategoryId { get; set; }

        public decimal? MinRating { get; set; }

        public bool IsVerifiedOnly { get; set; } = false;

        public double? UserLatitude { get; set; }
        public double? UserLongitude { get; set; }

        public double? RadiusInMeters { get; set; }

        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = true;

        public string? SearchText { get; set; }
    }
}

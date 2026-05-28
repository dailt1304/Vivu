using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetPopularLocations
{
    public record GetPopularLocationsQuery : IRequest<Result<List<PopularLocationDto>>>
    {
        public Guid? CityId { get; set; }
        public Guid? CategoryId { get; set; }
        public int Limit { get; set; } = 10;
    }
}

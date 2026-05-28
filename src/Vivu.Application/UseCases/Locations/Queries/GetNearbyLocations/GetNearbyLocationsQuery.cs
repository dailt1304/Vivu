using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetNearbyLocations
{
    public class GetNearbyLocationsQuery : IRequest<Result<List<NearbyLocationDto>>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public double RadiusInMeters { get; set; } = 10000;

        public Guid? CategoryId { get; set; }
        public decimal? MinRating { get; set; }
        public bool IsVerifiedOnly { get; set; } = true;

        public int Limit { get; set; } = 20;
    }
}

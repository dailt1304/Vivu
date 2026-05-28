using MediatR;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripImages.Queries.GetTripImages;

public class GetTripImagesQuery : IRequest<Result<List<TripImageDto>>>
{
    public Guid TripId { get; set; }
}

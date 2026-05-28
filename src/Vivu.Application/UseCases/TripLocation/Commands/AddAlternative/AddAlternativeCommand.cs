using MediatR;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.AddAlternative
{
    public class AddAlternativeCommand : IRequest<Result<TripLocationAlternativeResponse>>
    {
        public Guid TripLocationId { get; set; }
        public Guid LocationId { get; set; }
        public string? Reason { get; set; }
    }
}

using MediatR;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.RejectLocationSuggestion
{
    public class RejectLocationSuggestionCommand : IRequest<Result<LocationDto>>
    {
        public Guid LocationId { get; set; }
        public string AdminNote { get; set; } = string.Empty;
    }
}

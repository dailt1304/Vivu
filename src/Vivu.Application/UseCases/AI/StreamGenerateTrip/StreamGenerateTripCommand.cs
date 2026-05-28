using MediatR;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.StreamGenerateTrip
{
    public class StreamGenerateTripCommand : IStreamRequest<Result<StreamEvent>>
    {
        public string? GenerationId { get; init; }
        public required Guid CityId { get; init; }
        public required DateOnly StartDate { get; init; }
        public required DateOnly EndDate { get; init; }
        public int GroupSize { get; init; } = 1;
        public GroupCompositionType GroupComposition { get; init; } = GroupCompositionType.General;
        public List<string> Preferences { get; init; } = [];
        public TripBudget? Budget { get; init; }
        public string? Notes { get; init; }
        public string? Title { get; init; }
        public string? CoverUrl { get; init; }
        public bool AutoSave { get; init; } = true;
        public bool GenerateInviteCode { get; init; } = true;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Entities;
using Vivu.Domain.Shared;

namespace Vivu.Application.Interfaces.AI
{
    public interface IStreamingAIService
    {
        IAsyncEnumerable<Result<AIStreamChunk>> GenerateTripPlanStreamAsync(
          TripPlanDto request,
          List<Location> availableLocations,
          CancellationToken cancellationToken = default
        );
        IAsyncEnumerable<Result<AIStreamChunk>> ModifyTripPlanStreamAsync(
            TripPlanResponse currentTrip,
            List<ChatMessage> recentMessages,
            List<Location> availableLocations,
            string userRequest,
            UserPersonalizationContext? personalization = null,
            TripConstraints? constraints = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        );
        IAsyncEnumerable<Result<AIStreamChunk>> ChatMessageStreamAsync(
            Trip trip, 
            List<ChatMessage> recentMessages, 
            string userMessage,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        );
    }

}

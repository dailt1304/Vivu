using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Infrastructure.Services.AI
{
    public class StreamingAIService : IStreamingAIService
    {
        private readonly IAIClientFactory _clientFactory;
        private readonly IPromptBuilder _promptBuilder;
        private readonly ILogger<StreamingAIService> _logger;
        public StreamingAIService(IAIClientFactory clientFactory, IPromptBuilder promptBuilder, ILogger<StreamingAIService> logger)
        {
            _clientFactory = clientFactory;
            _promptBuilder = promptBuilder;
            _logger = logger;
        }

        public async IAsyncEnumerable<Result<AIStreamChunk>> ChatMessageStreamAsync(
            Trip trip, 
            List<ChatMessage> recentMessages, 
            string userMessage,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var prompt = _promptBuilder.BuildChatMessagePrompt(trip, recentMessages,userMessage);
            Console.WriteLine("CHAT MESSAGE PROMPT:" + prompt);
            var client = _clientFactory.GetClient();
            await foreach (var chunk in client.SendStreamRequestAsync(prompt, cancellationToken))
            {
                if (chunk.IsFailure)
                {
                    yield return chunk;
                    yield break;
                }
                yield return chunk;
            }
        }

        public async IAsyncEnumerable<Result<AIStreamChunk>> GenerateTripPlanStreamAsync(
          TripPlanDto request,
          List<Domain.Entities.Location> availableLocations,
          [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var prompt = _promptBuilder.BuildTripPlanPrompt(request, availableLocations);
            var client = _clientFactory.GetClient();

            await foreach (var chunk in client.SendStreamRequestAsync(prompt, cancellationToken))
            {
                if (chunk.IsFailure)
                {
                    yield return chunk;
                    yield break;
                }

                yield return chunk;
            }
        }
        public async IAsyncEnumerable<Result<AIStreamChunk>> ModifyTripPlanStreamAsync(
            TripPlanResponse currentTrip,
            List<ChatMessage> recentMessages,
            List<Domain.Entities.Location> availableLocations,
            string userRequest,
            UserPersonalizationContext? personalization = null,
            TripConstraints? constraints = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var prompt = _promptBuilder.BuildModifyTripPrompt(
                currentTrip,
                recentMessages,
                availableLocations,
                userRequest,
                personalization,
                constraints);

            var client = _clientFactory.GetClient();

            await foreach (var chunk in client.SendStreamRequestAsync(prompt, cancellationToken))
            {
                if (chunk.IsFailure)
                {
                    yield return chunk;
                    yield break;
                }
                yield return chunk;
            }
        }


    }

}

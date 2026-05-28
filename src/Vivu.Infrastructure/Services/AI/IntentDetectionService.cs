using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.AI;
using Vivu.Domain.Enums;

namespace Vivu.Infrastructure.Services.AI
{
    public class IntentDetectionService : IIntentDetectionService
    {
        private readonly IAIClientFactory _clientFactory;
        private readonly IPromptBuilder _promptBuilder;
        private readonly ILogger<IntentDetectionService> _logger;

        public IntentDetectionService(
            IAIClientFactory clientFactory,
            IPromptBuilder promptBuilder,
            ILogger<IntentDetectionService> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _promptBuilder = promptBuilder;
        }

        public async Task<AIChatIntent> DetectIntentAsync(
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            var prompt = _promptBuilder.BuildDetectIntentPrompt(userMessage);

            try
            {
                var client = _clientFactory.GetClient();
                var result = await client.SendRequestAsync(
                    prompt,
                    new AIRequestOptions { Temperature = 0.1 },
                    cancellationToken
                );

                if (result.IsFailure)
                {
                    _logger.LogWarning("Intent detection failed, defaulting to conversation");
                    return AIChatIntent.Conversation;
                }

                var response = result.Value!.Content.Trim().ToLower()
                    .Replace("\"", "").Replace("'", "");

                _logger.LogDebug("Intent detected: {Intent} for message: {Message}", response, userMessage);

                return response.Contains("modify_trip")
                    ? AIChatIntent.ModifyTrip
                    : AIChatIntent.Conversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting intent, defaulting to conversation");
                return AIChatIntent.Conversation;
            }
        }
    }
}

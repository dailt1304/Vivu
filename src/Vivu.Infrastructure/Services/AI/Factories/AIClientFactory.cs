using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.AI;

namespace Vivu.Infrastructure.Services.AI.Factories
{
    public class AIClientFactory : IAIClientFactory
    {
        private readonly IEnumerable<IAIClient> _clients;
        private readonly IConfiguration _config;
        private readonly ILogger<AIClientFactory> _logger;

        public AIClientFactory(
            IEnumerable<IAIClient> clients,
            IConfiguration config,
            ILogger<AIClientFactory> logger)
        {
            _clients = clients;
            _config = config;
            _logger = logger;
        }

        public IAIClient GetClient(string? providerName = null)
        {
            var targetProvider = providerName
                ?? _config["AI:DefaultProvider"]
                ?? AIProvider.Gemini;

            var client = _clients.FirstOrDefault(c =>
                c.ProviderName.Equals(targetProvider, StringComparison.OrdinalIgnoreCase));

            if (client == null)
            {
                _logger.LogWarning(
                    "AI provider '{Provider}' not found. Available: [{Available}]",
                    targetProvider,
                    string.Join(", ", _clients.Select(c => c.ProviderName)));

                throw new InvalidOperationException(
                    $"AI provider '{targetProvider}' is not registered. " +
                    $"Available providers: {string.Join(", ", _clients.Select(c => c.ProviderName))}");
            }

            _logger.LogDebug("Selected AI provider: {Provider}", client.ProviderName);
            return client;
        }
    }
}

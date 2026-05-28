using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;
using Vivu.Infrastructure.Repository;

namespace Vivu.Infrastructure.Services.AI
{
    public class UsageTrackingService :  IUsageTrackingService
    {
        private readonly IConfiguration _config;
        private readonly IApiUsageLogRepository _apiUsageLogRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UsageTrackingService> _logger;

        public UsageTrackingService(
            IConfiguration config,
            IApiUsageLogRepository apiUsageLogRepository,
            IUnitOfWork unitOfWork,
            ILogger<UsageTrackingService> logger) 
        {
            _config = config;
            _apiUsageLogRepository = apiUsageLogRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task LogUsageAsync(
            Guid userId,
            Guid? subscriptionId,
            string provider,
            int tokensInput,
            int tokensOutput,
            int responseTimeMs,
            decimal totalCost,
            int statusCode = 200,
            string? errorMessage = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var log = ApiUsageLog.Create(
                    userId: userId,
                    userSubscriptionId: subscriptionId, 
                    apiProvider: provider,             
                    tokensInput: tokensInput,
                    tokensOutput: tokensOutput,
                    totalCost: totalCost,
                    responseTimeMs: responseTimeMs,
                    statusCode: statusCode,
                    errorMessage: errorMessage
                );
                await _apiUsageLogRepository.AddAsync(log);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Logged AI usage: User={UserId}, Provider={Provider}, Tokens={Tokens}, Cost={Cost:C}",
                    userId, provider, tokensInput + tokensOutput, totalCost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log AI usage for user {UserId}", userId);
            }
        }
        public async Task LogFailedUsageAsync(
            Guid userId,
            Guid? subscriptionId,
            string errorMessage,
            AIRawResponse? response = null)
        {
            await LogUsageAsync(
                userId,
                subscriptionId,
                response?.Provider ?? "unknown",
                response?.TokensInput ?? 0,
                response?.TokensOutput ?? 0,
                response?.ResponseTimeMs ?? 0,
                0m,
                500,
                errorMessage);
        }
        public decimal CalculateCost(int tokeninput, int tokenoutput, string provider)
        {
            var unitCost = provider.ToLower() switch
            {
                "claude" => _config.GetValue<decimal>("AI:Claude:UnitCost", 0.00002m),
                "openai" => _config.GetValue<decimal>("AI:OpenAI:UnitCost", 0.00001m),
                _ => 0m
            };

            return (tokeninput + tokenoutput) * unitCost;
        }
    }
}

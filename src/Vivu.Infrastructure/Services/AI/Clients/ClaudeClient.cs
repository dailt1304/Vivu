using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Vivu.Domain.Errors.DomainErrors;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.AI;
using Vivu.Domain.Shared;
using System.Runtime.CompilerServices;

namespace Vivu.Infrastructure.Services.AI.Clients
{
    public class ClaudeClient : IAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<ClaudeClient> _logger;

        public string ProviderName => AIProvider.Claude;

        public ClaudeClient(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<ClaudeClient> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;

            var apiKey = _config["AI:Claude:ApiKey"]
                ?? throw new InvalidOperationException("Claude API key not configured");

            _httpClient.BaseAddress = new Uri("https://api.anthropic.com/");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<Result<AIRawResponse>> SendRequestAsync(
            string prompt,
            AIRequestOptions options,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var model = options.Model ?? _config["AI:Claude:DefaultModel"] ?? "claude-sonnet-4-5-20250929";

                var requestBody = new
                {
                    model = model,
                    max_tokens = options.MaxTokens,
                    temperature = options.Temperature,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                _logger.LogInformation(
                    "Sending request to Claude API. Model: {Model}, MaxTokens: {MaxTokens}",
                    model, options.MaxTokens);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

                var response = await _httpClient.PostAsJsonAsync(
                    "v1/messages",
                    requestBody,
                    cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Claude API error. Status: {Status}, Response: {Response}",
                        response.StatusCode, errorContent);

                    return Result<AIRawResponse>.Failure(AIErrors.ServiceUnavailable(ProviderName));
                }

                var result = await response.Content.ReadFromJsonAsync<ClaudeApiResponse>(cancellationToken);

                if (result?.Content == null || !result.Content.Any())
                {
                    return Result<AIRawResponse>.Failure((AIErrors.InvalidResponse("Empty response content")));
                }

                stopwatch.Stop();

                var rawResponse = new AIRawResponse
                {
                    Content = result.Content[0].Text,
                    TokensInput = result.Usage?.InputTokens ?? 0,
                    TokensOutput = result.Usage?.OutputTokens ?? 0,
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    Provider = ProviderName
                };

                _logger.LogInformation(
                    "Claude API success. Tokens: {Input}in/{Output}out, Time: {Time}ms",
                    rawResponse.TokensInput, rawResponse.TokensOutput, rawResponse.ResponseTimeMs);

                return rawResponse;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Claude API request timeout after {Timeout}s", options.TimeoutSeconds);
                return Result<AIRawResponse>.Failure(AIErrors.Timeout(options.TimeoutSeconds));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Claude API");
                return Result<AIRawResponse>.Failure(AIErrors.ServiceUnavailable(ProviderName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Claude API");
                return Result<AIRawResponse>.Failure(AIErrors.ServiceUnavailable(ProviderName));
            }
        }

        public IAsyncEnumerable<Result<AIStreamChunk>> SendStreamRequestAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        // Internal DTOs for Claude API
        private class ClaudeApiResponse
        {
            [JsonPropertyName("content")]
            public List<ContentBlock> Content { get; set; } = new();

            [JsonPropertyName("usage")]
            public UsageInfo? Usage { get; set; }
        }

        private class ContentBlock
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        private class UsageInfo
        {
            [JsonPropertyName("input_tokens")]
            public int InputTokens { get; set; }

            [JsonPropertyName("output_tokens")]
            public int OutputTokens { get; set; }
        }
    }
}

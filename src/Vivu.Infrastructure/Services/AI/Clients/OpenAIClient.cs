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
    public class OpenAIClient : IAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<OpenAIClient> _logger;

        public string ProviderName => AIProvider.OpenAI;

        public OpenAIClient(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<OpenAIClient> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;

            var apiKey = _config["AI:OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI API key not configured");

            _httpClient.BaseAddress = new Uri("https://api.openai.com/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
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
                var model = options.Model ?? _config["AI:OpenAI:DefaultModel"] ?? "gpt-4-turbo-preview";

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = options.MaxTokens,
                    temperature = options.Temperature
                };

                _logger.LogInformation(
                    "Sending request to OpenAI API. Model: {Model}, MaxTokens: {MaxTokens}",
                    model, options.MaxTokens);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

                var response = await _httpClient.PostAsJsonAsync(
                    "v1/chat/completions",
                    requestBody,
                    cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "OpenAI API error. Status: {Status}, Response: {Response}",
                        response.StatusCode, errorContent);

                    return AIErrors.ServiceUnavailable(ProviderName);
                }

                var result = await response.Content.ReadFromJsonAsync<OpenAIApiResponse>(cancellationToken);

                if (result?.Choices == null || !result.Choices.Any())
                {
                    return AIErrors.InvalidResponse("Empty response choices");
                }

                stopwatch.Stop();

                var rawResponse = new AIRawResponse
                {
                    Content = result.Choices[0].Message.Content,
                    TokensInput = result.Usage?.PromptTokens ?? 0,
                    TokensOutput = result.Usage?.CompletionTokens ?? 0,
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    Provider = ProviderName
                };

                _logger.LogInformation(
                    "OpenAI API success. Tokens: {Input}in/{Output}out, Time: {Time}ms",
                    rawResponse.TokensInput, rawResponse.TokensOutput, rawResponse.ResponseTimeMs);

                return rawResponse;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("OpenAI API request timeout after {Timeout}s", options.TimeoutSeconds);
                return AIErrors.Timeout(options.TimeoutSeconds);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling OpenAI API");
                return AIErrors.ServiceUnavailable(ProviderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling OpenAI API");
                return AIErrors.ServiceUnavailable(ProviderName);
            }
        }

        public IAsyncEnumerable<Result<AIStreamChunk>> SendStreamRequestAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        // Internal DTOs for OpenAI API
        private class OpenAIApiResponse
        {
            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; } = new();

            [JsonPropertyName("usage")]
            public UsageInfo? Usage { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("message")]
            public Message Message { get; set; } = new();
        }

        private class Message
        {
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private class UsageInfo
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }
        }
    }
}

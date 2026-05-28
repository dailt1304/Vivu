using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using static Vivu.Domain.Errors.DomainErrors;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.AI;
using Vivu.Domain.Shared;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Vivu.Infrastructure.Services.AI.Clients
{
    public class GeminiClient : IAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<GeminiClient> _logger;

        public string ProviderName => "Gemini";

        public GeminiClient(HttpClient httpClient, IConfiguration config, ILogger<GeminiClient> logger )
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;

            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<Result<AIRawResponse>> SendRequestAsync(
            string prompt,
            AIRequestOptions options,
            CancellationToken cancellationToken)
        {
            var apiKey = _config["AI:Gemini:ApiKey"];
            var model = options.Model ?? "gemini-3-flash-preview";

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                    generationConfig = new
                    {
                        temperature = options.Temperature
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"v1beta/models/{model}:generateContent?key={apiKey}",
                    requestBody,
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error: {Error}", error);
                    return AIErrors.ServiceUnavailable(ProviderName);
                }

                var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken);

                stopwatch.Stop();

                var content = result.Candidates[0].Content.Parts[0].Text;
                var tokensInput = result.UsageMetadata.PromptTokenCount;
                var tokensOutput = result.UsageMetadata.CandidatesTokenCount;
                return new AIRawResponse
                {
                    Content = content,
                    TokensInput = tokensInput,
                    TokensOutput = tokensOutput,
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    Provider = ProviderName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API request failed");
                return AIErrors.ServiceUnavailable(ProviderName);
            }
        }
        public async IAsyncEnumerable<Result<AIStreamChunk>> SendStreamRequestAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var apiKey = _config["AI:Gemini:ApiKey"];
            var model = "gemini-3-flash-preview";
            var stopwatch = Stopwatch.StartNew();
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                      parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7
                }
            };

            HttpResponseMessage? response = null;
            Result<AIStreamChunk>? errorResult = null; 

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"v1beta/models/{model}:streamGenerateContent?key={apiKey}&alt=sse")
                {
                    Content = JsonContent.Create(requestBody)
                };

                response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Gemini streaming API");
                errorResult = Result<AIStreamChunk>.Failure(AIErrors.ServiceUnavailable("Gemini"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Gemini streaming API");
                errorResult = Result<AIStreamChunk>.Failure(AIErrors.ServiceUnavailable("Gemini"));
            }

            if (errorResult != null)
            {
                yield return errorResult;
                yield break;
            }

            await using var stream = await response!.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);


            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("data: "))
                {
                    var json = line.Substring(6);

                    if (json == "[DONE]")
                    {
                        yield break;
                    }

                    GeminiResponse? chunk = null;

                    try
                    {
                        chunk = JsonSerializer.Deserialize<GeminiResponse>(json);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse stream chunk: {Json}", json);
                        continue; 
                    }
                    stopwatch.Stop();
                    var content = chunk?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";
                    int? promptTokens = chunk.UsageMetadata?.PromptTokenCount;
                    int? completionTokens = chunk.UsageMetadata?.CandidatesTokenCount;
                    if (!string.IsNullOrEmpty(content) || promptTokens.HasValue)
                    {

                        yield return Result<AIStreamChunk>.Success(new AIStreamChunk
                        {
                            Content = content,
                            IsComplete = false,
                            PromptTokens = promptTokens,
                            CompletionTokens = completionTokens,
                            ProviderName = ProviderName,
                            ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                        });
                    }
                }
            }
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate> Candidates { get; set; }

            [JsonPropertyName("usageMetadata")]
            public UsageMetadata UsageMetadata { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content Content { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public List<Part> Parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        private class UsageMetadata
        {
            [JsonPropertyName("promptTokenCount")]
            public int PromptTokenCount { get; set; }

            [JsonPropertyName("candidatesTokenCount")]
            public int CandidatesTokenCount { get; set; }
        }
    }
}

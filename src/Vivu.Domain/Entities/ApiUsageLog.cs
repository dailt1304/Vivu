using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class ApiUsageLog : Entity<Guid>
{
    public Guid? UserId { get; set; }
    public Guid? UserSubscriptionId { get; set; }
    public string ApiProvider { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int? TokensInput { get; set; }
    public int? TokensOutput { get; set; }
    public int? TokensTotal { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public int? ResponseTimeMs { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation Properties
    public virtual UserSubscription? UserSubscription { get; set; }

    public static ApiUsageLog Create(
            Guid? userId,
            Guid? userSubscriptionId,
            string apiProvider,
            int tokensInput,
            int tokensOutput,
            decimal totalCost,
            int responseTimeMs,
            int statusCode,
            string errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(apiProvider))
            throw new ArgumentException("Api Provider is required", nameof(apiProvider));

        if (tokensInput < 0 || tokensOutput < 0)
            throw new ArgumentException("Tokens cannot be negative");

        var tokensTotal = tokensInput + tokensOutput;

        var unitCost = totalCost / (decimal)Math.Max(1, tokensTotal);

        return new ApiUsageLog
        {
            Id = Guid.NewGuid(),               
            UserId = userId,
            UserSubscriptionId = userSubscriptionId,
            ApiProvider = apiProvider,
            RequestCount = 1,                  
            TokensInput = tokensInput,
            TokensOutput = tokensOutput,
            TokensTotal = tokensTotal,         
            UnitCost = unitCost,               
            TotalCost = totalCost,
            ResponseTimeMs = responseTimeMs,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            CreatedDate = DateTime.UtcNow      
        };
    }
}

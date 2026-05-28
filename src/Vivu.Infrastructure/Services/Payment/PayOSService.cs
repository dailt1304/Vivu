using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Application.Interfaces.Payment;

namespace Vivu.Infrastructure.Services.Payment;

public class PayOSService : IPayOSService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayOSService> _logger;
    private readonly string _clientId;
    private readonly string _apiKey;
    private readonly string _checksumKey;
    private readonly string _returnUrl;
    private readonly string _cancelUrl;

    private const string PayOSBaseUrl = "https://api-merchant.payos.vn";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public PayOSService(HttpClient httpClient, IConfiguration configuration, ILogger<PayOSService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _clientId = Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID")
            ?? configuration["PayOS:ClientId"]
            ?? throw new InvalidOperationException("PayOS ClientId is not configured.");

        _apiKey = Environment.GetEnvironmentVariable("PAYOS_API_KEY")
            ?? configuration["PayOS:ApiKey"]
            ?? throw new InvalidOperationException("PayOS ApiKey is not configured.");

        _checksumKey = Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY")
            ?? configuration["PayOS:ChecksumKey"]
            ?? throw new InvalidOperationException("PayOS ChecksumKey is not configured.");

        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
            ?? configuration["URLs:FrontendURL"]
            ?? "https://vivu.com";

        _returnUrl = configuration["PayOS:ReturnUrl"] ?? $"{frontendUrl}/payment/success";
        _cancelUrl = configuration["PayOS:CancelUrl"] ?? $"{frontendUrl}/payment/cancel";

        _httpClient.BaseAddress = new Uri(PayOSBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
    }

    public async Task<CreatePaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        string buyerName,
        string buyerEmail,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            orderCode,
            amount,
            description,
            buyerName,
            buyerEmail,
            returnUrl = _returnUrl,
            cancelUrl = _cancelUrl,
            signature = GenerateSignature(orderCode, amount, description, _cancelUrl, _returnUrl)
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation(
            "Creating PayOS payment link. OrderCode: {OrderCode}, Amount: {Amount}",
            orderCode, amount);

        var response = await _httpClient.PostAsync("/v2/payment-requests", content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayOS create payment link failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"PayOS API error: {responseBody}");
        }

        var result = JsonSerializer.Deserialize<PayOSApiResponse<PayOSPaymentLinkData>>(responseBody, JsonOptions);

        if (result?.Code != "00" || result.Data == null)
        {
            _logger.LogError("PayOS create payment link returned error code: {Code}, desc: {Desc}", result?.Code, result?.Desc);
            throw new InvalidOperationException($"PayOS error: {result?.Desc}");
        }

        return new CreatePaymentLinkResult
        {
            PaymentLinkId = result.Data.PaymentLinkId,
            CheckoutUrl = result.Data.CheckoutUrl,
            OrderCode = result.Data.OrderCode,
            Status = result.Data.Status
        };
    }

    public async Task<PayOSPaymentInfoResult> GetPaymentInfoAsync(long orderCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching PayOS payment info for OrderCode: {OrderCode}", orderCode);

        var response = await _httpClient.GetAsync($"/v2/payment-requests/{orderCode}", cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayOS get payment info failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"PayOS API error: {responseBody}");
        }

        var result = JsonSerializer.Deserialize<PayOSApiResponse<PayOSPaymentInfoData>>(responseBody, JsonOptions);

        if (result?.Code != "00" || result.Data == null)
        {
            throw new InvalidOperationException($"PayOS error: {result?.Desc}");
        }

        return new PayOSPaymentInfoResult
        {
            PaymentLinkId = result.Data.Id,
            OrderCode = result.Data.OrderCode,
            Status = result.Data.Status,
            Amount = result.Data.Amount,
            AmountPaid = result.Data.AmountPaid,
            AmountRemaining = result.Data.AmountRemaining,
            Transactions = result.Data.Transactions?.Select(t => new PayOSTransaction
            {
                Reference = t.Reference,
                Amount = t.Amount,
                AccountNumber = t.AccountNumber,
                Description = t.Description,
                TransactionDateTime = t.TransactionDateTime,
                CounterAccountBankId = t.CounterAccountBankId,
                CounterAccountBankName = t.CounterAccountBankName,
                CounterAccountName = t.CounterAccountName,
                CounterAccountNumber = t.CounterAccountNumber,
                VirtualAccountName = t.VirtualAccountName,
                VirtualAccountNumber = t.VirtualAccountNumber
            }).ToList() ?? new List<PayOSTransaction>()
        };
    }

    public async Task<CancelPaymentResult> CancelPaymentLinkAsync(
        long orderCode,
        string? cancellationReason = null,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new { cancellationReason };
        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Cancelling PayOS payment link for OrderCode: {OrderCode}", orderCode);

        var response = await _httpClient.PostAsync($"/v2/payment-requests/{orderCode}/cancel", content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayOS cancel payment failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"PayOS API error: {responseBody}");
        }

        var result = JsonSerializer.Deserialize<PayOSApiResponse<PayOSCancelData>>(responseBody, JsonOptions);

        if (result?.Code != "00" || result.Data == null)
        {
            throw new InvalidOperationException($"PayOS error: {result?.Desc}");
        }

        return new CancelPaymentResult
        {
            PaymentLinkId = result.Data.Id,
            OrderCode = result.Data.OrderCode,
            Status = result.Data.Status,
            Amount = result.Data.Amount
        };
    }

    public bool VerifyWebhookSignature(string webhookBody, string signature)
    {
        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(webhookBody);
            if (!data.TryGetProperty("data", out var dataElement))
            {
                _logger.LogWarning("Webhook body does not contain 'data' property");
                return false;
            }

            var sortedData = new SortedDictionary<string, string>();
            foreach (var property in dataElement.EnumerateObject())
            {
                sortedData[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }

            var dataString = string.Join("&", sortedData.Select(kv => $"{kv.Key}={kv.Value}"));
            var computedSignature = ComputeHmacSha256(dataString, _checksumKey);
            var isValid = string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayOS webhook signature");
            return false;
        }
    }

    private string GenerateSignature(long orderCode, int amount, string description, string cancelUrl, string returnUrl)
    {
        var sortedData = new SortedDictionary<string, string>
        {
            ["amount"] = amount.ToString(),
            ["cancelUrl"] = cancelUrl,
            ["description"] = description,
            ["orderCode"] = orderCode.ToString(),
            ["returnUrl"] = returnUrl
        };

        var dataString = string.Join("&", sortedData.Select(kv => $"{kv.Key}={kv.Value}"));
        return ComputeHmacSha256(dataString, _checksumKey);
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    // Internal API response models
    private class PayOSApiResponse<T>
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = string.Empty;
        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private class PayOSPaymentLinkData
    {
        [JsonPropertyName("paymentLinkId")]
        public string PaymentLinkId { get; set; } = string.Empty;
        [JsonPropertyName("checkoutUrl")]
        public string CheckoutUrl { get; set; } = string.Empty;
        [JsonPropertyName("orderCode")]
        public long OrderCode { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("qrCode")]
        public string? QrCode { get; set; }
    }

    private class PayOSPaymentInfoData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("orderCode")]
        public long OrderCode { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        [JsonPropertyName("amountPaid")]
        public int AmountPaid { get; set; }
        [JsonPropertyName("amountRemaining")]
        public int AmountRemaining { get; set; }
        [JsonPropertyName("transactions")]
        public List<PayOSTransactionData>? Transactions { get; set; }
    }

    private class PayOSTransactionData
    {
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("transactionDateTime")]
        public string TransactionDateTime { get; set; } = string.Empty;
        [JsonPropertyName("counterAccountBankId")]
        public string? CounterAccountBankId { get; set; }
        [JsonPropertyName("counterAccountBankName")]
        public string? CounterAccountBankName { get; set; }
        [JsonPropertyName("counterAccountName")]
        public string? CounterAccountName { get; set; }
        [JsonPropertyName("counterAccountNumber")]
        public string? CounterAccountNumber { get; set; }
        [JsonPropertyName("virtualAccountName")]
        public string? VirtualAccountName { get; set; }
        [JsonPropertyName("virtualAccountNumber")]
        public string? VirtualAccountNumber { get; set; }
    }

    private class PayOSCancelData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("orderCode")]
        public long OrderCode { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }
}

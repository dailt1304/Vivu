using Vivu.Application.DTOs.Responses.Payments;

namespace Vivu.Application.Interfaces.Payment;

public interface IPayOSService
{
    Task<CreatePaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        string buyerName,
        string buyerEmail,
        Guid packageId,
        CancellationToken cancellationToken = default);

    Task<PayOSPaymentInfoResult> GetPaymentInfoAsync(long orderCode, CancellationToken cancellationToken = default);

    Task<CancelPaymentResult> CancelPaymentLinkAsync(long orderCode, string? cancellationReason = null, CancellationToken cancellationToken = default);

    bool VerifyWebhookSignature(string webhookBody, string signature);
}

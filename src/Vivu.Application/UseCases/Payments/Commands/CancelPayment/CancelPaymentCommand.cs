using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Commands.CancelPayment;

public class CancelPaymentCommand : IRequest<Result<bool>>
{
    public Guid TransactionId { get; set; }
    public string? CancellationReason { get; set; }
}

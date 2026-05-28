using MediatR;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Queries.GetTransactionById;

public class GetTransactionByIdQuery : IRequest<Result<TransactionDto>>
{
    public Guid TransactionId { get; set; }
}

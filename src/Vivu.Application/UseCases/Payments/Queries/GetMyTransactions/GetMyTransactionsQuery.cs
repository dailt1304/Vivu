using MediatR;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Queries.GetMyTransactions;

public class GetMyTransactionsQuery : IRequest<Result<List<TransactionDto>>>
{
}

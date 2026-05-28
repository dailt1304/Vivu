using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Queries.GetMyTransactions;

public class GetMyTransactionsQueryHandler : IRequestHandler<GetMyTransactionsQuery, Result<List<TransactionDto>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetMyTransactionsQueryHandler> _logger;

    public GetMyTransactionsQueryHandler(
        ICurrentUser currentUser,
        ITransactionRepository transactionRepository,
        IMapper mapper,
        ILogger<GetMyTransactionsQueryHandler> logger)
    {
        _currentUser = currentUser;
        _transactionRepository = transactionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<TransactionDto>>> Handle(GetMyTransactionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result<List<TransactionDto>>.Failure(DomainErrors.Auth.InvalidToken);
        }

        var transactions = await _transactionRepository.GetByUserIdAsync(userId, cancellationToken);
        var dtos = _mapper.Map<List<TransactionDto>>(transactions);

        return Result<List<TransactionDto>>.Success(dtos);
    }
}

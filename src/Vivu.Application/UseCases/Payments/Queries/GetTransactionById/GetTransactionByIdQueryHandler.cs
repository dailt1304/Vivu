using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Queries.GetTransactionById;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, Result<TransactionDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTransactionByIdQueryHandler> _logger;

    public GetTransactionByIdQueryHandler(
        ICurrentUser currentUser,
        ITransactionRepository transactionRepository,
        IMapper mapper,
        ILogger<GetTransactionByIdQueryHandler> logger)
    {
        _currentUser = currentUser;
        _transactionRepository = transactionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<TransactionDto>> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result<TransactionDto>.Failure(DomainErrors.Auth.InvalidToken);
        }

        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);

        if (transaction == null)
        {
            return Result<TransactionDto>.Failure(DomainErrors.Payment.TransactionNotFound);
        }

        if (transaction.UserId != userId)
        {
            return Result<TransactionDto>.Failure(DomainErrors.Payment.AccessDenied);
        }

        var dto = _mapper.Map<TransactionDto>(transaction);
        return Result<TransactionDto>.Success(dto);
    }
}

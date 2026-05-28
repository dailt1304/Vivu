using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Payment;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Commands.CancelPayment;

public class CancelPaymentCommandHandler : IRequestHandler<CancelPaymentCommand, Result<bool>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPayOSService _payOSService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelPaymentCommandHandler> _logger;

    public CancelPaymentCommandHandler(
        ICurrentUser currentUser,
        ITransactionRepository transactionRepository,
        IPayOSService payOSService,
        IUnitOfWork unitOfWork,
        ILogger<CancelPaymentCommandHandler> logger)
    {
        _currentUser = currentUser;
        _transactionRepository = transactionRepository;
        _payOSService = payOSService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            _logger.LogWarning("Cancel payment failed: Invalid or missing user ID");
            return Result<bool>.Failure(DomainErrors.Auth.InvalidToken);
        }

        _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);

        if (transaction == null)
        {
            _logger.LogWarning(
                "Cancel payment failed: Transaction not found. TransactionId: {TransactionId}",
                request.TransactionId);
            return Result<bool>.Failure(DomainErrors.Payment.TransactionNotFound);
        }

        if (transaction.UserId != userId)
        {
            _logger.LogWarning(
                "Cancel payment failed: Access denied. UserId: {UserId}, TransactionId: {TransactionId}",
                userId,
                request.TransactionId);
            return Result<bool>.Failure(DomainErrors.Payment.AccessDenied);
        }

        if (transaction.Status != PaymentStatus.Pending.ToString())
        {
            _logger.LogWarning(
                "Cancel payment failed: Cannot cancel non-pending transaction. TransactionId: {TransactionId}, Status: {Status}",
                request.TransactionId,
                transaction.Status);
            return Result<bool>.Failure(DomainErrors.Payment.CannotCancelNonPendingTransaction);
        }

        try
        {
            await _payOSService.CancelPaymentLinkAsync(transaction.OrderCode, request.CancellationReason ?? "Cancelled by user", cancellationToken);
            transaction.Status = PaymentStatus.Cancelled.ToString();
            transaction.ModifiedDate = DateTime.UtcNow;
            _transactionRepository.Update(transaction);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Transaction cancelled successfully. TransactionId: {TransactionId}, UserId: {UserId}",
                transaction.Id, userId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel transaction {TransactionId}", request.TransactionId);
            return Result<bool>.Failure(DomainErrors.Payment.CancelFailed);
        }
    }
}

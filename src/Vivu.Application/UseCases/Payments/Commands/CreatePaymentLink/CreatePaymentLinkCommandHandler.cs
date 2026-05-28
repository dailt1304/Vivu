using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Payment;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Commands.CreatePaymentLink;

public class CreatePaymentLinkCommandHandler : IRequestHandler<CreatePaymentLinkCommand, Result<CreatePaymentResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionPackageRepository _packageRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPayOSService _payOSService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreatePaymentLinkCommandHandler> _logger;

    public CreatePaymentLinkCommandHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        ISubscriptionPackageRepository packageRepository,
        IUserSubscriptionRepository userSubscriptionRepository,
        ITransactionRepository transactionRepository,
        IPayOSService payOSService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreatePaymentLinkCommandHandler> logger)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _packageRepository = packageRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
        _transactionRepository = transactionRepository;
        _payOSService = payOSService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<CreatePaymentResponse>> Handle(CreatePaymentLinkCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Create payment link attempt. PackageId: {PackageId}",
            request.PackageId);

        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            _logger.LogWarning("Create payment link failed: Invalid or missing user ID");
            return Result<CreatePaymentResponse>.Failure(DomainErrors.Auth.InvalidToken);
        }

        _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

        var package = await _packageRepository.GetByIdAsync(request.PackageId);
        if (package == null || !package.IsActive)
        {
            _logger.LogWarning(
                "Create payment link failed: Package not found or inactive. PackageId: {PackageId}",
                request.PackageId);
            return Result<CreatePaymentResponse>.Failure(DomainErrors.Subscription.PackageNotFound);
        }

        _logger.LogDebug("Package validated. PackageId: {PackageId}, Name: {PackageName}", package.Id, package.Name);

        var activeSubscription = await _userSubscriptionRepository.GetUserActiveSubscription(userId, cancellationToken);
        if (activeSubscription != null)
        {
            _logger.LogWarning(
                "Create payment link failed: User already has active subscription. UserId: {UserId}",
                userId);
            return Result<CreatePaymentResponse>.Failure(DomainErrors.Subscription.AlreadySubscribed);
        }

        // Check for existing pending transaction
        var existingPending = await _transactionRepository.GetPendingByUserAndPackageAsync(userId, package.Id, cancellationToken);
        if (existingPending != null && !string.IsNullOrEmpty(existingPending.CheckoutUrl))
        {
            // Verify the actual status on PayOS before reusing
            try
            {
                var payosInfo = await _payOSService.GetPaymentInfoAsync(existingPending.OrderCode, cancellationToken);
                
                if (payosInfo.Status == "PENDING")
                {
                    // Link is still valid on PayOS, safe to reuse
                    _logger.LogInformation(
                        "Reusing existing valid pending transaction {TransactionId} for User {UserId}, Package {PackageId}",
                        existingPending.Id, userId, package.Id);

                    var response = _mapper.Map<CreatePaymentResponse>(existingPending);
                    return Result<CreatePaymentResponse>.Success(response);
                }
                else
                {
                    // Link is cancelled/expired on PayOS, mark our transaction accordingly
                    _logger.LogInformation(
                        "Existing pending transaction {TransactionId} has PayOS status '{Status}'. Marking as cancelled and creating new one.",
                        existingPending.Id, payosInfo.Status);

                    existingPending.Status = PaymentStatus.Cancelled.ToString();
                    existingPending.ModifiedDate = DateTime.UtcNow;
                    _transactionRepository.Update(existingPending);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // If PayOS lookup fails, cancel the stale transaction and create a new one
                _logger.LogWarning(ex,
                    "Failed to verify PayOS status for pending transaction {TransactionId}. Marking as cancelled.",
                    existingPending.Id);

                existingPending.Status = PaymentStatus.Cancelled.ToString();
                existingPending.ModifiedDate = DateTime.UtcNow;
                _transactionRepository.Update(existingPending);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var user = await _userRepository.GetByIdAsync(userId);
        var buyerName = user?.UserProfile?.FullName ?? string.Empty;
        var buyerEmail = user?.Email ?? string.Empty;
        _logger.LogDebug("User details retrieved. UserId: {UserId}, Email: {Email}", userId, buyerEmail);

        // Generate a unique orderCode using timestamp + random to avoid collisions
        var orderCode = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 9_000_000L) * 1000L 
                        + Random.Shared.Next(100, 999);
        var amount = (int)Math.Round(package.Price);
        var rawDescription = $"{package.Name}";
        var description = rawDescription.Length > 25 ? rawDescription[..25] : rawDescription;

        try
        {
            var paymentLink = await _payOSService.CreatePaymentLinkAsync(
                orderCode, amount, description, buyerName, buyerEmail, package.Id, cancellationToken);

            var transaction = Transaction.Create(
                userId: userId,
                packageId: package.Id,
                amount: package.Price,
                paymentMethod: "PayOS",
                orderCode: paymentLink.OrderCode,
                paymentLinkId: paymentLink.PaymentLinkId,
                checkoutUrl: paymentLink.CheckoutUrl,
                description: description);

            _logger.LogDebug("Transaction entity created. TransactionId: {TransactionId}", transaction.Id);

            await _transactionRepository.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Transaction saved to database. TransactionId: {TransactionId}", transaction.Id);

            _logger.LogInformation(
                "Payment link created successfully. TransactionId: {TransactionId}, UserId: {UserId}, PackageId: {PackageId}, OrderCode: {OrderCode}, Amount: {Amount}",
                transaction.Id,
                userId,
                package.Id,
                paymentLink.OrderCode,
                package.Price);

            var response = _mapper.Map<CreatePaymentResponse>(transaction);
            return Result<CreatePaymentResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PayOS payment link for User {UserId}, Package {PackageId}", userId, package.Id);
            return Result<CreatePaymentResponse>.Failure(DomainErrors.Payment.CreateFailed);
        }
    }
}

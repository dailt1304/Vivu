using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Application.Interfaces.Email;
using Vivu.Application.Interfaces.Invoices;
using Vivu.Application.Interfaces.Payment;
using Vivu.Application.UseCases.Notifications.Events;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Commands.HandlePayOSWebhook;

public class HandlePayOSWebhookCommandHandler : IRequestHandler<HandlePayOSWebhookCommand, Result>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly ISubscriptionPackageRepository _packageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPayOSService _payOSService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInvoiceService _invoiceService;
    private readonly IEmailService _emailService;
    private readonly IPublisher _publisher;
    private readonly ILogger<HandlePayOSWebhookCommandHandler> _logger;

    public HandlePayOSWebhookCommandHandler(
        ITransactionRepository transactionRepository,
        IUserSubscriptionRepository userSubscriptionRepository,
        ISubscriptionPackageRepository packageRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPayOSService payOSService,
        IUnitOfWork unitOfWork,
        IInvoiceService invoiceService,
        IEmailService emailService,
        IPublisher publisher,
        ILogger<HandlePayOSWebhookCommandHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
        _packageRepository = packageRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _payOSService = payOSService;
        _unitOfWork = unitOfWork;
        _invoiceService = invoiceService;
        _emailService = emailService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(HandlePayOSWebhookCommand request, CancellationToken cancellationToken)
    {
        if (!_payOSService.VerifyWebhookSignature(request.WebhookBody, request.Signature))
        {
            _logger.LogWarning("PayOS webhook signature verification failed");
            return Result.Failure(DomainErrors.Payment.InvalidWebhookSignature);
        }

        PayOSWebhookPayload? payload;
        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            payload = JsonSerializer.Deserialize<PayOSWebhookPayload>(request.WebhookBody, jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize PayOS webhook payload");
            return Result.Failure(DomainErrors.Payment.WebhookProcessingFailed);
        }

        if (payload?.Data == null)
        {
            _logger.LogWarning("PayOS webhook payload has no data");
            return Result.Failure(DomainErrors.Payment.WebhookProcessingFailed);
        }

        var orderCode = payload.Data.OrderCode;
        var transaction = await _transactionRepository.GetByOrderCodeAsync(orderCode, cancellationToken);

        if (transaction == null)
        {
            _logger.LogWarning("PayOS webhook received for unknown OrderCode: {OrderCode}", orderCode);
            return Result.Success();
        }

        if (transaction.Status != PaymentStatus.Pending.ToString())
        {
            _logger.LogInformation(
                "PayOS webhook skipped: Transaction {TransactionId} already in status {Status}",
                transaction.Id, transaction.Status);
            return Result.Success();
        }

        var isPaid = payload.Code == "00" && payload.Success;

        if (isPaid)
        {
            transaction.Status = PaymentStatus.Paid.ToString();
            transaction.ModifiedDate = DateTime.UtcNow;
            _transactionRepository.Update(transaction);

            var user = await _userRepository.GetByIdWithRolesTrackedAsync(transaction.UserId, cancellationToken);
            if (user != null)
            {
                var premiumRole = await _roleRepository.GetByNameAsync(Role.Names.Premium);
                if (premiumRole != null)
                {
                    user.AssignRole(premiumRole.Id);
                    _userRepository.Update(user);
                }
                else
                {
                    _logger.LogWarning("Premium role not found while processing payment success. UserId: {UserId}", transaction.UserId);
                }
            }
            else
            {
                _logger.LogWarning("User not found while processing payment success. UserId: {UserId}", transaction.UserId);
            }

            if (transaction.PackageId.HasValue)
            {
                var package = await _packageRepository.GetByIdAsync(transaction.PackageId.Value);
                if (package != null)
                {
                    var existingSubscription = await _userSubscriptionRepository
                        .GetUserActiveSubscription(transaction.UserId, cancellationToken);

                    if (existingSubscription == null)
                    {
                        var subscription = UserSubscription.Create(transaction.UserId, package);
                        await _userSubscriptionRepository.AddAsync(subscription);

                        _logger.LogInformation(
                            "Activated subscription {SubscriptionId} for User {UserId}. OrderCode: {OrderCode}",
                            subscription.Id, transaction.UserId, transaction.OrderCode);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "User {UserId} already has an active subscription. OrderCode: {OrderCode}",
                            transaction.UserId, transaction.OrderCode);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                var invoice = await _invoiceService.BuildInvoiceFromTransactionAsync(
                    transaction.Id, 
                    cancellationToken);
                
                await _emailService.SendTransactionDetailsEmailAsync(
                    invoice.Customer.Email, 
                    invoice);

                _logger.LogInformation(
                    "Transaction details email sent for OrderCode: {OrderCode} to {Email}",
                    orderCode, invoice.Customer.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to send transaction email for OrderCode: {OrderCode}. Transaction was still processed successfully.",
                    orderCode);
            }
        }
        else
        {
            transaction.Status = PaymentStatus.Cancelled.ToString();
            transaction.ModifiedDate = DateTime.UtcNow;
            _transactionRepository.Update(transaction);

            _logger.LogInformation(
                "PayOS payment cancelled for OrderCode: {OrderCode}, Transaction: {TransactionId}",
                orderCode, transaction.Id);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Publish payment notification events
        if (isPaid)
        {
            string? packageName = null;
            if (transaction.PackageId.HasValue)
            {
                var pkg = await _packageRepository.GetByIdAsync(transaction.PackageId.Value);
                packageName = pkg?.Name;
            }
            await _publisher.Publish(new PaymentSuccessEvent(
                userId: transaction.UserId,
                transactionId: transaction.Id,
                packageName: packageName), cancellationToken);
        }
        else
        {
            await _publisher.Publish(new PaymentFailedEvent(
                userId: transaction.UserId,
                transactionId: transaction.Id), cancellationToken);
        }
        return Result.Success();
    }
}

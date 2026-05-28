using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Application.Interfaces.Invoices;
using Vivu.Domain.Interfaces;

namespace Vivu.Infrastructure.Services.Invoices;

public class InvoiceService : IInvoiceService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ITransactionRepository transactionRepository,
        ILogger<InvoiceService> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<InvoiceDto> BuildInvoiceFromTransactionAsync(
        Guid transactionId, 
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdWithDetailsAsync(transactionId, cancellationToken);

        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction with ID '{transactionId}' not found.");
        }

        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var invoiceDate = TimeZoneInfo.ConvertTimeFromUtc(transaction.CreatedDate, vietnamTimeZone);
        var transactionDate = TimeZoneInfo.ConvertTimeFromUtc(transaction.CreatedDate, vietnamTimeZone);
        
        var total = transaction.Amount;

        var invoice = new InvoiceDto
        {
            TransactionId = transaction.Id,
            InvoiceNumber = GenerateInvoiceNumber(transaction.OrderCode, invoiceDate),
            InvoiceDate = invoiceDate,
            
            Customer = new InvoiceCustomerDto
            {
                UserId = transaction.UserId,
                Email = transaction.User.Email,
                FullName = transaction.User.UserProfile?.FullName,
                Phone = transaction.User.Phone
            },
            
            Package = transaction.Package != null ? new InvoicePackageDto
            {
                PackageId = transaction.Package.Id,
                Name = transaction.Package.Name,
                Code = transaction.Package.Code,
                Description = transaction.Package.Description,
                DurationDays = transaction.Package.DurationDays,
                MaxAiRequestPerDay = transaction.Package.MaxAiRequestPerDay,
                Price = transaction.Package.Price
            } : new InvoicePackageDto
            {
                PackageId = Guid.Empty,
                Name = "Unknown Package",
                Price = transaction.Amount
            },
            
            Payment = new InvoicePaymentDto
            {
                OrderCode = transaction.OrderCode,
                PaymentMethod = transaction.PaymentMethod,
                PayOSPaymentLinkId = transaction.PayOSPaymentLinkId,
                TransactionDate = transactionDate
            },
            
            Subtotal = total,
            Tax = 0,
            Total = total,
            Status = transaction.Status,
            Notes = transaction.Description
        };

        return invoice;
    }

    private string GenerateInvoiceNumber(long orderCode, DateTime createdDate)
    {
        return $"INV-{createdDate:yyyyMMdd}-{orderCode}";
    }
}

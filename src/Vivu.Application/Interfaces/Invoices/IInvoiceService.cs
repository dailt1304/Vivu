using Vivu.Application.DTOs.Responses.Payments;

namespace Vivu.Application.Interfaces.Invoices;

public interface IInvoiceService
{
    Task<InvoiceDto> BuildInvoiceFromTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
}

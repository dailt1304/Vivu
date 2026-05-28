using Vivu.Application.DTOs.Responses.Payments;

namespace Vivu.Application.Interfaces.Email;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string code);
    Task SendPasswordResetEmailAsync(string email, string code, string username);
    Task SendTransactionDetailsEmailAsync(string email, InvoiceDto invoice);
}
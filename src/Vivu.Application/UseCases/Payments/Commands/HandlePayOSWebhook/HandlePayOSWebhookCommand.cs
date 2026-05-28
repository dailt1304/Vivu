using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Commands.HandlePayOSWebhook;

public class HandlePayOSWebhookCommand : IRequest<Result>
{
    public string WebhookBody { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

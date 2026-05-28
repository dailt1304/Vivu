using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.Payments.Commands.CancelPayment;
using Vivu.Application.UseCases.Payments.Commands.CreatePaymentLink;
using Vivu.Application.UseCases.Payments.Commands.HandlePayOSWebhook;
using Vivu.Application.UseCases.Payments.Queries.GetMyTransactions;
using Vivu.Application.UseCases.Payments.Queries.GetTransactionById;

namespace Vivu.WebApi.Controllers;

[Route("api/payments")]
[ApiController]
[Authorize]
public class PaymentController : ApiControllerBase
{
    [HttpPost("create-payment-link")]
    public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("transactions/{id:guid}/cancel")]
    public async Task<IActionResult> CancelPayment(
        [FromRoute] Guid id,
        [FromBody] CancelPaymentCommand command)
    {
        command.TransactionId = id;
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetMyTransactions()
    {
        var query = new GetMyTransactionsQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("transactions/{id:guid}")]
    public async Task<IActionResult> GetTransactionById([FromRoute] Guid id)
    {
        var query = new GetTransactionByIdQuery { TransactionId = id };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpPost("webhook/payos")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePayOSWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        string signature = string.Empty;
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("signature", out var signatureElement))
            {
                signature = signatureElement.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // If parsing fails, signature will be empty and verification will fail
        }

        var command = new HandlePayOSWebhookCommand
        {
            WebhookBody = body,
            Signature = signature
        };

        var result = await Mediator.Send(command);
        if (result.IsFailure)
        {
            return Ok(new { success = false, message = "Webhook processed with errors" });
        }

        return Ok(new { success = true });
    }
}

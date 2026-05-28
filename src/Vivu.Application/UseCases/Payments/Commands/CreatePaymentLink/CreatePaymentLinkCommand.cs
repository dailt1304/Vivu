using MediatR;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Payments.Commands.CreatePaymentLink;

public class CreatePaymentLinkCommand : IRequest<Result<CreatePaymentResponse>>
{
    public Guid PackageId { get; set; }
}

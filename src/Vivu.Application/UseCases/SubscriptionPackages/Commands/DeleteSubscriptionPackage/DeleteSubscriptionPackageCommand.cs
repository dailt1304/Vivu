using MediatR;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.DeleteSubscriptionPackage;

public class DeleteSubscriptionPackageCommand : IRequest<Result<SubscriptionPackageDto>>
{
    public Guid Id { get; set; }
}

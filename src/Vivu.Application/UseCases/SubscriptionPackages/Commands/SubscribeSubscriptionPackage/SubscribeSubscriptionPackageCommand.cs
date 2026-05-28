using MediatR;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.SubscribeSubscriptionPackage;

public class SubscribeSubscriptionPackageCommand : IRequest<Result<UserSubscriptionDto>>
{
    public Guid PackageId { get; set; }
}
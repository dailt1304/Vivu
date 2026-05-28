using MediatR;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Queries.GetSubscriptionPackageById;

public class GetSubscriptionPackageByIdQuery : IRequest<Result<SubscriptionPackageDto>>
{
    public Guid Id { get; set; }
}

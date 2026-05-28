using MediatR;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Queries.GetActiveSubscriptionPackages;

public class GetActiveSubscriptionPackagesQuery : IRequest<Result<List<SubscriptionPackageDto>>>
{
}

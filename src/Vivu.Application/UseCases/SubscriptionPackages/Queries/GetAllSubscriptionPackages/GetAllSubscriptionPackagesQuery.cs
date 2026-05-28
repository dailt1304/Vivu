using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Queries.GetAllSubscriptionPackages;

public class GetAllSubscriptionPackagesQuery : PaginationRequest, IRequest<Result<PaginatedList<SubscriptionPackageDto>>>
{
}

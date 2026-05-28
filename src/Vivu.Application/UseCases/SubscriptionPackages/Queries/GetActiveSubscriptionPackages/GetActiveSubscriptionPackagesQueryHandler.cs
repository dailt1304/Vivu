using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Queries.GetActiveSubscriptionPackages;

public class GetActiveSubscriptionPackagesQueryHandler 
    : IRequestHandler<GetActiveSubscriptionPackagesQuery, Result<List<SubscriptionPackageDto>>>
{
    private readonly ISubscriptionPackageRepository _subscriptionPackageRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetActiveSubscriptionPackagesQueryHandler> _logger;

    public GetActiveSubscriptionPackagesQueryHandler(
        ISubscriptionPackageRepository subscriptionPackageRepository,
        IMapper mapper,
        ILogger<GetActiveSubscriptionPackagesQueryHandler> logger)
    {
        _subscriptionPackageRepository = subscriptionPackageRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<SubscriptionPackageDto>>> Handle(
        GetActiveSubscriptionPackagesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching active subscription packages");

        var packages = await _subscriptionPackageRepository.GetActivePackagesAsync(cancellationToken);

        _logger.LogDebug(
            "Retrieved {Count} active subscription packages",
            packages.Count);

        var packageDtos = packages
            .Select(package => _mapper.Map<SubscriptionPackageDto>(package))
            .ToList();

        _logger.LogInformation(
            "Successfully fetched {Count} active subscription packages",
            packageDtos.Count);

        return Result<List<SubscriptionPackageDto>>.Success(packageDtos);
    }
}

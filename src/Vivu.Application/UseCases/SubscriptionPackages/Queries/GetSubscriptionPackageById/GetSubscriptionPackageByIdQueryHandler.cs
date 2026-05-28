using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Queries.GetSubscriptionPackageById;

public class GetSubscriptionPackageByIdQueryHandler 
    : IRequestHandler<GetSubscriptionPackageByIdQuery, Result<SubscriptionPackageDto>>
{
    private readonly ISubscriptionPackageRepository _subscriptionPackageRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSubscriptionPackageByIdQueryHandler> _logger;

    public GetSubscriptionPackageByIdQueryHandler(
        ISubscriptionPackageRepository subscriptionPackageRepository,
        IMapper mapper,
        ILogger<GetSubscriptionPackageByIdQueryHandler> logger)
    {
        _subscriptionPackageRepository = subscriptionPackageRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<SubscriptionPackageDto>> Handle(
        GetSubscriptionPackageByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching subscription package detail for Id: {PackageId}",
            request.Id);

        var package = await _subscriptionPackageRepository.GetByIdAsync(request.Id);

        if (package == null)
        {
            _logger.LogWarning("Subscription package with ID {PackageId} not found", request.Id);
            return Result<SubscriptionPackageDto>.Failure(
                DomainErrors.Subscription.PackageNotFound);
        }

        _logger.LogDebug(
            "Successfully retrieved subscription package {PackageId} with name {PackageName}",
            package.Id,
            package.Name);

        var packageDto = _mapper.Map<SubscriptionPackageDto>(package);

        _logger.LogInformation(
            "Successfully fetched subscription package {PackageId}",
            package.Id);

        return Result<SubscriptionPackageDto>.Success(packageDto);
    }
}

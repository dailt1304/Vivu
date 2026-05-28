using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Queries.GetAllSubscriptionPackages;

public class GetAllSubscriptionPackagesQueryHandler
    : IRequestHandler<GetAllSubscriptionPackagesQuery, Result<PaginatedList<SubscriptionPackageDto>>>
{
    private readonly ISubscriptionPackageRepository _subscriptionPackageRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllSubscriptionPackagesQueryHandler> _logger;

    public GetAllSubscriptionPackagesQueryHandler(
        ISubscriptionPackageRepository subscriptionPackageRepository,
        IMapper mapper,
        ILogger<GetAllSubscriptionPackagesQueryHandler> logger)
    {
        _subscriptionPackageRepository = subscriptionPackageRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<SubscriptionPackageDto>>> Handle(
        GetAllSubscriptionPackagesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching subscription packages, Page: {PageNumber}, PageSize: {PageSize}",
            request.PageNumber,
            request.PageSize);

        var query = _subscriptionPackageRepository.GetAllQuery();

        if (!await query.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("No subscription packages found");

            var emptyResult = new PaginatedList<SubscriptionPackageDto>(
                new List<SubscriptionPackageDto>(),
                count: 0,
                request.PageNumber,
                request.PageSize);

            return Result<PaginatedList<SubscriptionPackageDto>>.Success(emptyResult);
        }

        // Order by DisplayOrder, then by CreatedDate
        query = query.OrderBy(sp => sp.DisplayOrder)
                     .ThenByDescending(sp => sp.CreatedDate);

        var paginatedPackages = await query.ToPaginatedListAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        _logger.LogDebug(
            "Retrieved {Count} subscription packages out of {TotalCount}",
            paginatedPackages.Items.Count,
            paginatedPackages.TotalCount);

        // Mapping
        var packageDtos = paginatedPackages.Items
            .Select(package => _mapper.Map<SubscriptionPackageDto>(package))
            .ToList();

        var result = new PaginatedList<SubscriptionPackageDto>(
            packageDtos,
            paginatedPackages.TotalCount,
            paginatedPackages.PageNumber,
            paginatedPackages.PageSize);

        _logger.LogInformation(
            "Successfully fetched {Count} subscription packages",
            result.Items.Count);

        return Result<PaginatedList<SubscriptionPackageDto>>.Success(result);
    }
}
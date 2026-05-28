using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.LocationCategories;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetLocationCategories
{
    public class GetLocationCategoriesQueryHandler : IRequestHandler<GetLocationCategoriesQuery, Result<PaginatedList<LocationCategoryDto>>>
    {
        private readonly ILocationCategoryRepository _locationCategoryRepository;
        private readonly ILogger<GetLocationCategoriesQueryHandler> _logger;
        private readonly IMapper _mapper;

        public GetLocationCategoriesQueryHandler(
            ILocationCategoryRepository locationCategoryRepository,
            ILogger<GetLocationCategoriesQueryHandler> logger,
            IMapper mapper)
        {
            _locationCategoryRepository = locationCategoryRepository;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<PaginatedList<LocationCategoryDto>>> Handle(
            GetLocationCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching location categories. Page: {PageNumber}, PageSize: {PageSize}",
                request.PageNumber,
                request.PageSize);

            var query = _locationCategoryRepository.GetAllCategoriesWithLocationCountQuery();

            var paginatedCategories = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            _logger.LogDebug("Retrieved {Count} location categories", paginatedCategories.Items.Count);

            // Maping
            var categoryDtos = _mapper.Map<List<LocationCategoryDto>>(paginatedCategories.Items);

            var mappedResult = new PaginatedList<LocationCategoryDto>(
                categoryDtos,
                paginatedCategories.TotalCount,
                paginatedCategories.PageNumber,
                paginatedCategories.PageSize);

            _logger.LogInformation(
                "Successfully fetched location categories. TotalCount: {TotalCount}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                mappedResult.TotalCount,
                mappedResult.PageNumber,
                mappedResult.PageSize);

            return Result<PaginatedList<LocationCategoryDto>>.Success(mappedResult);
        }
    }
}

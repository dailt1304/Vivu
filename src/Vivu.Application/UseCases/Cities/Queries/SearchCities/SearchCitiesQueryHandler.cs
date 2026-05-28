using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Queries.SearchCities
{
    public class SearchCitiesQueryHandler : IRequestHandler<SearchCitiesQuery, Result<PaginatedList<CityDto>>>
    {
        private readonly ICityRepository _cityRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SearchCitiesQueryHandler> _logger;

        public SearchCitiesQueryHandler(
            ICityRepository cityRepository,
            IMapper mapper,
            ILogger<SearchCitiesQueryHandler> logger)
        {
            _cityRepository = cityRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<CityDto>>> Handle(
            SearchCitiesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Searching cities - SearchText: {SearchText}, Page: {PageNumber}, PageSize: {PageSize}",
                request.SearchText, request.PageNumber, request.PageSize);

            var query = _cityRepository.GetSearchQuery(request.SearchText);

            var paginatedCities = await query.ToPaginatedListAsync(
                request.PageNumber, request.PageSize, cancellationToken);

            var result = paginatedCities.Map(c => _mapper.Map<CityDto>(c));

            return Result<PaginatedList<CityDto>>.Success(result);
        }
    }
}

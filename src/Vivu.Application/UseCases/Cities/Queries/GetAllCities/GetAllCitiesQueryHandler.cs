using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Queries.GetAllCities
{
    public class GetAllCitiesQueryHandler : IRequestHandler<GetAllCitiesQuery, Result<PaginatedList<CityDto>>>
    {
        private readonly ICityRepository _cityRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllCitiesQueryHandler> _logger;

        public GetAllCitiesQueryHandler(
            ICityRepository cityRepository,
            IMapper mapper,
            ILogger<GetAllCitiesQueryHandler> logger)
        {
            _cityRepository = cityRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<CityDto>>> Handle(
            GetAllCitiesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching cities - CountryId: {CountryId}, Page: {PageNumber}, PageSize: {PageSize}",
                request.CountryId, request.PageNumber, request.PageSize);

            var query = request.CountryId.HasValue
                ? _cityRepository.GetByCountryIdQuery(request.CountryId.Value)
                : _cityRepository.GetAllQuery();

            // Use ProjectTo so EF Core generates a SQL COUNT subquery for LocationCount
            // instead of loading all Location entities into memory.
            var projected = query.ProjectTo<CityDto>(_mapper.ConfigurationProvider);

            var paginatedResult = await projected.ToPaginatedListAsync(
                request.PageNumber, request.PageSize, cancellationToken);

            return Result<PaginatedList<CityDto>>.Success(paginatedResult);
        }
    }
}


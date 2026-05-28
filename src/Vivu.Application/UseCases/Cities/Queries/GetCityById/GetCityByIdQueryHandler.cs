using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Queries.GetCityById
{
    public class GetCityByIdQueryHandler : IRequestHandler<GetCityByIdQuery, Result<CityDto>>
    {
        private readonly ICityRepository _cityRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetCityByIdQueryHandler> _logger;

        public GetCityByIdQueryHandler(
            ICityRepository cityRepository,
            IMapper mapper,
            ILogger<GetCityByIdQueryHandler> logger)
        {
            _cityRepository = cityRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CityDto>> Handle(
            GetCityByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching city with ID: {CityId}", request.CityId);

            var city = await _cityRepository.GetByIdAsync(request.CityId);

            if (city == null)
            {
                _logger.LogWarning("City not found with ID: {CityId}", request.CityId);
                return Result<CityDto>.Failure(DomainErrors.Cities.NotFoundById(request.CityId));
            }

            var result = _mapper.Map<CityDto>(city);
            return Result<CityDto>.Success(result);
        }
    }
}

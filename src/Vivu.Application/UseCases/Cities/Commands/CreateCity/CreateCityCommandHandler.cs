using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Commands.CreateCity
{
    public class CreateCityCommandHandler : IRequestHandler<CreateCityCommand, Result<CityDto>>
    {
        private readonly ICityRepository _cityRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<CreateCityCommandHandler> _logger;

        public CreateCityCommandHandler(
            ICityRepository cityRepository,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateCityCommandHandler> logger)
        {
            _cityRepository = cityRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CityDto>> Handle(
            CreateCityCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Create city failed: Invalid or missing user ID");
                return Result<CityDto>.Failure(DomainErrors.Auth.InvalidToken);
            }
            _logger.LogInformation("Creating new city: {CityName}", request.Name);

            var existCity = _cityRepository.GetSearchQuery(request.Name).FirstOrDefault();
            if (existCity != null)
            {
                _logger.LogWarning("Create city failed: city name {name} already exist in database", existCity.Name);
                return Result<CityDto>.Failure(DomainErrors.Cities.AlreadyExistByName(existCity.Name));
            }

            var city = City.Create
            (
                name: request.Name,
                countryId: request.CountryId,
                latitude: request.Latitude,
                longitude: request.Longitude,
                image: request.Image,
                creater: userId
                
            );

            await _cityRepository.AddAsync(city);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var createdCity = await _cityRepository.GetByIdAsync(city.Id);
            
            var result = _mapper.Map<CityDto>(createdCity);
            _logger.LogInformation("City created successfully with ID: {CityId}", city.Id);

            return Result<CityDto>.Success(result);
        }
    }
}

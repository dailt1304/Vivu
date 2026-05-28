using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Domain.Errors;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Commands.UpdateCity
{
    public class UpdateCityCommandHandler : IRequestHandler<UpdateCityCommand, Result<CityDto>>
    {
        private readonly ICityRepository _cityRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateCityCommandHandler> _logger;

        public UpdateCityCommandHandler(
            ICityRepository cityRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateCityCommandHandler> logger)
        {
            _cityRepository = cityRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CityDto>> Handle(
            UpdateCityCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating city with ID: {CityId}", request.CityId);

            var city = await _cityRepository.GetByIdAsync(request.CityId);

            if (city == null)
            {
                _logger.LogWarning("City not found with ID: {CityId}", request.CityId);
                return Result<CityDto>.Failure(DomainErrors.Cities.NotFoundById(request.CityId));
            }

            if (request.CountryId.HasValue)
            {
                city.CountryId = request.CountryId.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                city.Name = request.Name;
                city.NameAscii = Domain.Entities.City.RemoveDiacritics(request.Name);
            }

            if (request.Latitude.HasValue)
            {
                city.Latitude = request.Latitude.Value;
            }

            if (request.Longitude.HasValue)
            {
                city.Longitude = request.Longitude.Value;
            }

            if (request.Image != null)
            {
                city.Image = request.Image;
            }

            city.ModifiedDate = DateTime.UtcNow;

            _cityRepository.Update(city);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updatedCity = await _cityRepository.GetByIdAsync(city.Id);

            var result = _mapper.Map<CityDto>(updatedCity);
            _logger.LogInformation("City updated successfully with ID: {CityId}", city.Id);

            return Result<CityDto>.Success(result);
        }
    }
}

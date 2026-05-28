using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Commands.DeleteCity
{
    public class DeleteCityCommandHandler : IRequestHandler<DeleteCityCommand, Result<bool>>
    {
        private readonly ICityRepository _cityRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteCityCommandHandler> _logger;

        public DeleteCityCommandHandler(
            ICityRepository cityRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteCityCommandHandler> logger)
        {
            _cityRepository = cityRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            DeleteCityCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting city with ID: {CityId}", request.CityId);

            var city = await _cityRepository.GetByIdAsync(request.CityId);

            if (city == null)
            {
                _logger.LogWarning("City not found with ID: {CityId}", request.CityId);
                return Result<bool>.Failure(DomainErrors.Cities.NotFoundById(request.CityId));
            }

            _cityRepository.Remove(city);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("City deleted successfully with ID: {CityId}", request.CityId);

            return Result<bool>.Success(true);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Collections;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Commands.AddLocationToCollection
{
    public class AddLocationToCollectionCommandHandler : IRequestHandler<AddLocationToCollectionCommand, Result<CollectionLocationDto>>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly ICollectionLocationRepository _collectionLocationRepository;
        private readonly IGenericRepository<Location> _locationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AddLocationToCollectionCommandHandler> _logger;

        public AddLocationToCollectionCommandHandler(
            ICollectionRepository collectionRepository,
            ICollectionLocationRepository collectionLocationRepository,
            IGenericRepository<Location> locationRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AddLocationToCollectionCommandHandler> logger)
        {
            _collectionRepository = collectionRepository;
            _collectionLocationRepository = collectionLocationRepository;
            _locationRepository = locationRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CollectionLocationDto>> Handle(AddLocationToCollectionCommand request, CancellationToken cancellationToken)
        {
            var collection = await _collectionRepository.GetByIdAsync(request.CollectionId);
            if (collection == null || collection.IsDeleted)
            {
                return Result<CollectionLocationDto>.Failure(DomainErrors.Collection.NotFound);
            }

            if (collection.UserId != request.UserId)
            {
                return Result<CollectionLocationDto>.Failure(DomainErrors.Collection.AccessDenied);
            }

            var location = await _locationRepository.GetByIdAsync(request.LocationId);
            if (location == null || location.IsDeleted)
            {
                return Result<CollectionLocationDto>.Failure(DomainErrors.Location.NotFound);
            }

            var existingLink = await _collectionLocationRepository.GetAsync(request.CollectionId, request.LocationId, cancellationToken);
            if (existingLink != null)
            {
                return Result<CollectionLocationDto>.Failure(DomainErrors.Collection.LocationAlreadyInCollection);
            }

            var collectionLocation = CollectionLocation.Create(request.CollectionId, request.LocationId, request.Note);

            await _collectionLocationRepository.AddAsync(collectionLocation);
            
            // Touch collection modified date
            collection.ModifiedDate = System.DateTime.UtcNow;
            _collectionRepository.Update(collection);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Location {LocationId} added to Collection {CollectionId}", request.LocationId, request.CollectionId);

            var dto = _mapper.Map<CollectionLocationDto>(collectionLocation);
            
            // Map additional location fields since the freshly created entity won't have the navigation properties fully loaded
            dto.Name = location.Name;
            dto.Address = location.Address;
            dto.RatingAverage = location.RatingAverage;
            dto.Latitude = location.Latitude;
            dto.Longitude = location.Longitude;

            return Result<CollectionLocationDto>.Success(dto);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Commands.RemoveLocationFromCollection
{
    public class RemoveLocationFromCollectionCommandHandler : IRequestHandler<RemoveLocationFromCollectionCommand, Result<bool>>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly ICollectionLocationRepository _collectionLocationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveLocationFromCollectionCommandHandler> _logger;

        public RemoveLocationFromCollectionCommandHandler(
            ICollectionRepository collectionRepository,
            ICollectionLocationRepository collectionLocationRepository,
            IUnitOfWork unitOfWork,
            ILogger<RemoveLocationFromCollectionCommandHandler> logger)
        {
            _collectionRepository = collectionRepository;
            _collectionLocationRepository = collectionLocationRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(RemoveLocationFromCollectionCommand request, CancellationToken cancellationToken)
        {
            var collection = await _collectionRepository.GetByIdAsync(request.CollectionId);
            if (collection == null || collection.IsDeleted)
            {
                return Result<bool>.Failure(DomainErrors.Collection.NotFound);
            }

            if (collection.UserId != request.UserId)
            {
                return Result<bool>.Failure(DomainErrors.Collection.AccessDenied);
            }

            var collectionLocation = await _collectionLocationRepository.GetAsync(request.CollectionId, request.LocationId, cancellationToken);
            if (collectionLocation == null)
            {
                return Result<bool>.Failure(DomainErrors.Collection.LocationNotInCollection);
            }

            _collectionLocationRepository.Remove(collectionLocation);

            // Touch collection modified date
            collection.ModifiedDate = System.DateTime.UtcNow;
            _collectionRepository.Update(collection);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Location {LocationId} removed from Collection {CollectionId}", request.LocationId, request.CollectionId);

            return Result<bool>.Success(true);
        }
    }
}

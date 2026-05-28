using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Commands.DeleteCollection
{
    public class DeleteCollectionCommandHandler : IRequestHandler<DeleteCollectionCommand, Result<bool>>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteCollectionCommandHandler> _logger;

        public DeleteCollectionCommandHandler(
            ICollectionRepository collectionRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteCollectionCommandHandler> logger)
        {
            _collectionRepository = collectionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(DeleteCollectionCommand request, CancellationToken cancellationToken)
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

            // Perform soft delete
            collection.Delete();
            
            _collectionRepository.Update(collection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Collection soft deleted successfully: {CollectionId}", collection.Id);

            return Result<bool>.Success(true);
        }
    }
}

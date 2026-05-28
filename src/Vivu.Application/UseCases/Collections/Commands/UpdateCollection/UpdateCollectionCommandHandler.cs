using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Collections;
using Vivu.Application.Interfaces.Files;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Commands.UpdateCollection
{
    public class UpdateCollectionCommandHandler : IRequestHandler<UpdateCollectionCommand, Result<CollectionDto>>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateCollectionCommandHandler> _logger;

        public UpdateCollectionCommandHandler(
            ICollectionRepository collectionRepository,
            IUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            ILogger<UpdateCollectionCommandHandler> logger)
        {
            _collectionRepository = collectionRepository;
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CollectionDto>> Handle(UpdateCollectionCommand request, CancellationToken cancellationToken)
        {
            var collection = await _collectionRepository.GetByIdAsync(request.CollectionId);
            if (collection == null || collection.IsDeleted)
            {
                return Result<CollectionDto>.Failure(DomainErrors.Collection.NotFound);
            }

            if (collection.UserId != request.UserId)
            {
                return Result<CollectionDto>.Failure(DomainErrors.Collection.AccessDenied);
            }

            var isDuplicate = await _collectionRepository.IsNameDuplicate(request.UserId, request.Name, collection.Id, cancellationToken);
            if (isDuplicate)
            {
                return Result<CollectionDto>.Failure(DomainErrors.Collection.NameAlreadyExists);
            }

            collection.Update(request.Name, request.Description);

            if (request.CoverImage != null)
            {
                using var stream = request.CoverImage.OpenReadStream();
                var coverImageUrl = await _cloudinaryService.UploadImageAsync(stream, request.CoverImage.FileName, "collections");
                collection.UpdateCoverImage(coverImageUrl);
            }

            _collectionRepository.Update(collection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Collection updated successfully: {CollectionId}", collection.Id);

            var collectionDto = _mapper.Map<CollectionDto>(collection);
            return Result<CollectionDto>.Success(collectionDto);
        }
    }
}

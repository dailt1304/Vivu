using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Collections;
using Vivu.Application.Interfaces.Files;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Commands.CreateCollection
{
    public class CreateCollectionCommandHandler : IRequestHandler<CreateCollectionCommand, Result<CollectionDto>>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateCollectionCommandHandler> _logger;

        public CreateCollectionCommandHandler(
            ICollectionRepository collectionRepository,
            IUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            ILogger<CreateCollectionCommandHandler> logger)
        {
            _collectionRepository = collectionRepository;
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CollectionDto>> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
        {
            var isDuplicate = await _collectionRepository.IsNameDuplicate(request.UserId, request.Name, null, cancellationToken);
            if (isDuplicate)
            {
                return Result<CollectionDto>.Failure(DomainErrors.Collection.NameAlreadyExists);
            }

            string? coverImageUrl = null;
            if (request.CoverImage != null)
            {
                using var stream = request.CoverImage.OpenReadStream();
                coverImageUrl = await _cloudinaryService.UploadImageAsync(stream, request.CoverImage.FileName, "collections");
            }

            var collection = Collection.Create(request.UserId, request.Name, request.Description, coverImageUrl);

            await _collectionRepository.AddAsync(collection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Collection created successfully: {CollectionId}", collection.Id);

            var collectionDto = _mapper.Map<CollectionDto>(collection);
            return Result<CollectionDto>.Success(collectionDto);
        }
    }
}

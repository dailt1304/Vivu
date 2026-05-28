using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Vivu.Application.DTOs.Responses.Collections;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Queries.GetCollectionDetail
{
    public class GetCollectionDetailQueryHandler : IRequestHandler<GetCollectionDetailQuery, Result<CollectionDetailDto>>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IMapper _mapper;

        public GetCollectionDetailQueryHandler(ICollectionRepository collectionRepository, IMapper mapper)
        {
            _collectionRepository = collectionRepository;
            _mapper = mapper;
        }

        public async Task<Result<CollectionDetailDto>> Handle(GetCollectionDetailQuery request, CancellationToken cancellationToken)
        {
            var collection = await _collectionRepository.GetCollectionWithLocations(request.CollectionId, cancellationToken);

            if (collection == null || collection.IsDeleted)
            {
                return Result<CollectionDetailDto>.Failure(DomainErrors.Collection.NotFound);
            }

            if (collection.UserId != request.UserId)
            {
                return Result<CollectionDetailDto>.Failure(DomainErrors.Collection.AccessDenied);
            }

            var collectionDto = _mapper.Map<CollectionDetailDto>(collection);
            return Result<CollectionDetailDto>.Success(collectionDto);
        }
    }
}

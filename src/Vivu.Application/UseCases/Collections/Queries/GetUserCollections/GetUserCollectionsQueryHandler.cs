using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Collections;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Queries.GetUserCollections
{
    public class GetUserCollectionsQueryHandler : IRequestHandler<GetUserCollectionsQuery, Result<PaginatedList<CollectionDto>>>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IMapper _mapper;

        public GetUserCollectionsQueryHandler(ICollectionRepository collectionRepository, IMapper mapper)
        {
            _collectionRepository = collectionRepository;
            _mapper = mapper;
        }

        public async Task<Result<PaginatedList<CollectionDto>>> Handle(GetUserCollectionsQuery request, CancellationToken cancellationToken)
        {
            var query = string.IsNullOrWhiteSpace(request.SearchText)
                ? _collectionRepository.GetUserCollections(request.UserId)
                : _collectionRepository.SearchUserCollections(request.UserId, request.SearchText);

            // Count total before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Fetch entities with pagination (client-side)
            var entities = await query
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);

            // Map on client-side to avoid EF Core translation issues with GetFirstImage()
            var dtos = _mapper.Map<List<CollectionDto>>(entities);

            var paginatedList = new PaginatedList<CollectionDto>(dtos, totalCount, request.PageNumber, request.PageSize);

            return Result<PaginatedList<CollectionDto>>.Success(paginatedList);
        }
    }
}

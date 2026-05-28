using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.DTOs.Responses.Collections;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Queries.GetUserCollectionsSummary
{
    public class GetUserCollectionsSummaryQueryHandler : IRequestHandler<GetUserCollectionsSummaryQuery, Result<List<CollectionSummaryDto>>>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IMapper _mapper;

        public GetUserCollectionsSummaryQueryHandler(ICollectionRepository collectionRepository, IMapper mapper)
        {
            _collectionRepository = collectionRepository;
            _mapper = mapper;
        }

        public async Task<Result<List<CollectionSummaryDto>>> Handle(GetUserCollectionsSummaryQuery request, CancellationToken cancellationToken)
        {
            var query = _collectionRepository.GetUserCollectionsSummary(request.UserId);

            var list = await query.ProjectTo<CollectionSummaryDto>(_mapper.ConfigurationProvider)
                                  .ToListAsync(cancellationToken);

            return Result<List<CollectionSummaryDto>>.Success(list);
        }
    }
}

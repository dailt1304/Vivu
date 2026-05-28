using System;
using MediatR;
using Vivu.Application.DTOs.Responses.Collections;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Queries.GetCollectionDetail
{
    public class GetCollectionDetailQuery : IRequest<Result<CollectionDetailDto>>
    {
        public Guid CollectionId { get; set; }
        public Guid UserId { get; set; }
    }
}

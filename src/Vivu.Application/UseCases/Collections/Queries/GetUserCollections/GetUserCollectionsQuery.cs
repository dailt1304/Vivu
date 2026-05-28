using System;
using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Collections;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Queries.GetUserCollections
{
    public class GetUserCollectionsQuery : PaginationRequest, IRequest<Result<PaginatedList<CollectionDto>>>
    {
        public Guid UserId { get; set; }
        public string? SearchText { get; set; }
    }
}

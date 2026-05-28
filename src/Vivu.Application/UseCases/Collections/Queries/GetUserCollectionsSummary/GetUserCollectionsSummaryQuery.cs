using System;
using System.Collections.Generic;
using MediatR;
using Vivu.Application.DTOs.Responses.Collections;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Queries.GetUserCollectionsSummary
{
    public class GetUserCollectionsSummaryQuery : IRequest<Result<List<CollectionSummaryDto>>>
    {
        public Guid UserId { get; set; }
    }
}

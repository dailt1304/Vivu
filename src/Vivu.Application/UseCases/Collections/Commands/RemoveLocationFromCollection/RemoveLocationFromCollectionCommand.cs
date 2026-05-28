using System;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Commands.RemoveLocationFromCollection
{
    public class RemoveLocationFromCollectionCommand : IRequest<Result<bool>>
    {
        public Guid CollectionId { get; set; }
        public Guid LocationId { get; set; }
        public Guid UserId { get; set; }
    }
}

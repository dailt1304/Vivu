using System;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Collections.Commands.DeleteCollection
{
    public class DeleteCollectionCommand : IRequest<Result<bool>>
    {
        public Guid CollectionId { get; set; }
        public Guid UserId { get; set; }
    }
}

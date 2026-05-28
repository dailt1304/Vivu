using System;
using MediatR;
using Vivu.Domain.Shared;
using Vivu.Application.DTOs.Responses.Collections;

namespace Vivu.Application.UseCases.Collections.Commands.AddLocationToCollection
{
    public class AddLocationToCollectionCommand : IRequest<Result<CollectionLocationDto>>
    {
        public Guid CollectionId { get; set; }
        public Guid UserId { get; set; }
        public Guid LocationId { get; set; }
        public string? Note { get; set; }
    }
}

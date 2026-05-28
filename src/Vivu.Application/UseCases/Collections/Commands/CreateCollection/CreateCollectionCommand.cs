using System;
using Microsoft.AspNetCore.Http;
using MediatR;
using Vivu.Domain.Shared;
using Vivu.Application.DTOs.Responses.Collections;

namespace Vivu.Application.UseCases.Collections.Commands.CreateCollection
{
    public class CreateCollectionCommand : IRequest<Result<CollectionDto>>
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IFormFile? CoverImage { get; set; }
    }
}

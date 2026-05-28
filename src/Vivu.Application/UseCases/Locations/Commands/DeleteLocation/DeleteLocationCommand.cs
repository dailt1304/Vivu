using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.DeleteLocation
{
    public class DeleteLocationCommand : IRequest<Result<bool>>
    {
        public Guid LocationId { get; set; }
    }
}

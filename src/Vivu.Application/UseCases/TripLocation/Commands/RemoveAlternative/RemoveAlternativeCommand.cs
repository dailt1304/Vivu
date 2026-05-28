using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.RemoveAlternative
{
    public class RemoveAlternativeCommand : IRequest<Result>
    {
        public Guid AlternativeId { get; set; }
    }
}

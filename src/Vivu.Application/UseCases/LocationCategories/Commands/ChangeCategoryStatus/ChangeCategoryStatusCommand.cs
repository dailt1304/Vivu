using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationCategories.Commands.ChangeCategoryStatus
{
    public class ChangeCategoryStatusCommand : IRequest<Result<bool>>
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
    }
}

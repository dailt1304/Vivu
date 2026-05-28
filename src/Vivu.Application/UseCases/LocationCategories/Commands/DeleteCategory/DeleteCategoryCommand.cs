using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationCategories.Commands.DeleteCategory
{
    public class DeleteCategoryCommand : IRequest<Result<bool>>
    {
        public Guid Id { get; set; }
    }
}

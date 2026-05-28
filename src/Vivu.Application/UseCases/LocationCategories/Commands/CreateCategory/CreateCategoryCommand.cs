using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationCategories.Commands.CreateCategory
{
    public class CreateCategoryCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int CategoryType { get; set; }
    }
}

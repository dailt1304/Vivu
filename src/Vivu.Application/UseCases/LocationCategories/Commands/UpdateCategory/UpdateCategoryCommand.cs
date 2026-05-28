using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationCategories.Commands.UpdateCategory
{
    public class UpdateCategoryCommand : IRequest<Result<bool>>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int CategoryType { get; set; }
    }
}

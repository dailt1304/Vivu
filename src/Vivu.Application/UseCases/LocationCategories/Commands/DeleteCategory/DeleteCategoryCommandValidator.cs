using FluentValidation;

namespace Vivu.Application.UseCases.LocationCategories.Commands.DeleteCategory
{
    public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
    {
        public DeleteCategoryCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("ID danh mục không được để trống.");
        }
    }
}

using FluentValidation;

namespace Vivu.Application.UseCases.LocationCategories.Commands.CreateCategory
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tên danh mục không được để trống.")
                .MaximumLength(200)
                .WithMessage("Tên danh mục không được vượt quá 200 ký tự.");

            RuleFor(x => x.CategoryType)
                .InclusiveBetween(0, 5)
                .WithMessage("Loại danh mục phải nằm trong khoảng 0-5.");
        }
    }
}

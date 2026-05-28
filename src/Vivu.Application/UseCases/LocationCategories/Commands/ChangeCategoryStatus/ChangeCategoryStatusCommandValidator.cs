using FluentValidation;

namespace Vivu.Application.UseCases.LocationCategories.Commands.ChangeCategoryStatus
{
    public class ChangeCategoryStatusCommandValidator : AbstractValidator<ChangeCategoryStatusCommand>
    {
        public ChangeCategoryStatusCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("ID danh mục không được để trống.");
        }
    }
}
